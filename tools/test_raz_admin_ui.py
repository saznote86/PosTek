"""Test écran UIA du scénario complet : RAZ puis reconnexion admin/admin.

Scénario fidèle au prompt « Correction RAZ (admin par défaut) » :
  Phase 1 : première installation — créer l'admin admin/admin, changer
            le mot de passe obligatoire, ouvrir la caisse, accéder à l'Accueil ;
  Phase 2 : Outils -> RAZ (double confirmation + code « RAZ ») puis
            vérification SQLite (tables métier vides, admin réinitialisé) ;
  Phase 3 : redémarrage de l'application, reconnexion admin/admin — DOIT
            aboutir — puis changement obligatoire et ouverture de caisse.

Usage :
  python postek/tools/test_raz_admin_ui.py [chemin-exe]

Variables utiles :
  POSTEK_ATTENTE_APP        délai d'attente des fenêtres (défaut : 30 s)
  POSTEK_MOT_DE_PASSE       mot de passe final après changement obligatoire
                            (défaut : postek1234 — 6 caractères minimum)

Le script ne passe jamais par la souris : tout passe par UIA (Invoke/Value).
"""

import base64
import ctypes
import hashlib
import os
import shutil
import sqlite3
import subprocess
import sys
import time
from pathlib import Path

from test_helpers import (
    _uia, _uic, attendre_fermeture, attendre_fenetre, cliquer, descendants,
    element_name, fenetres_visibles, find_by_name, find_by_automation_id,
    find_contains, hwnd_fenetre_titre, naviguer_vers, raw_descendants,
    verifier_contenu_texte,
)

RACINE = Path(__file__).resolve().parents[2]
EXE_DEFAUT = (RACINE / "postek/desktop/Postek.Caisse/bin/Debug/net8.0-windows/Postek.Caisse.exe")
BASE = (RACINE / "postek/desktop/Postek.Caisse/bin/Debug/net8.0-windows/postek_demo.db")

TIMEOUT = float(os.environ.get("POSTEK_ATTENTE_APP", "30"))
MOT_DE_PASSE_FINAL = os.environ.get("POSTEK_MOT_DE_PASSE", "postek1234")

TITRE_LOGIN = "POSTEK - Connexion"
TITRE_ACCUEIL = "POSTEK - Accueil"
TITRE_CHANGEMENT = "POSTEK - Changement de mot de passe"
TITRE_OUVERTURE = "POSTEK - Ouverture de caisse"

_processus = None


def log(message):
    print(f"[*] {message}", flush=True)


def _set_value(element, valeur):
    brut = element.GetCurrentPattern(_uic.UIA_ValuePatternId)
    if brut is None:
        raise RuntimeError(f"Champ sans ValuePattern : {element_name(element)!r}")
    brut.QueryInterface(_uic.IUIAutomationValuePattern).SetValue(valeur)


def tuer_application():
    subprocess.run(["taskkill", "/F", "/IM", "Postek.Caisse.exe"],
                   capture_output=True, check=False)
    for _ in range(20):
        etat = subprocess.run(["tasklist", "/FI", "IMAGENAME eq Postek.Caisse.exe"],
                              capture_output=True, text=True, check=False)
        if "Postek.Caisse.exe" not in etat.stdout:
            return
        time.sleep(0.3)


def lancer_application():
    global _processus
    drapeaux = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
    _processus = subprocess.Popen([str(EXE_DEFAUT)], cwd=str(EXE_DEFAUT.parent),
                                  creationflags=drapeaux, close_fds=True)
    log(f"Application lancée (PID {_processus.pid})")


def fermer_application():
    hwnd = hwnd_fenetre_titre(TITRE_ACCUEIL)
    if hwnd is None:
        hwnd = hwnd_fenetre_titre(TITRE_LOGIN)
    if hwnd is not None:
        ctypes.windll.user32.PostMessageW(hwnd, 0x0010, 0, 0)  # WM_CLOSE
        attendre_fermeture(hwnd, timeout=15.0)
    for _ in range(20):
        if _processus is None or _processus.poll() is not None:
            return
        time.sleep(0.3)
    tuer_application()


def preparer_base():
    """Sauvegarde puis repart d'une base vierge (première installation)."""
    tuer_application()
    for suffixe in ("", "-wal", "-shm"):
        chemin = Path(str(BASE) + suffixe)
        if chemin.exists():
            if suffixe == "":
                dossier = RACINE / "postek"
                horodatage = time.strftime("%Y%m%d_%H%M%S")
                destination = dossier / f"postek_demo_backup_testraz_{horodatage}.db"
                shutil.copy2(chemin, destination)
                log(f"Base sauvegardée : {destination.name}")
            chemin.unlink()
    log("Base de démonstration réinitialisée (première installation au lancement)")


def verifier_mdp(empreinte, mot):
    """Rejoue le PBKDF2-SHA256 du service C# sur l'empreinte stockée."""
    morceaux = empreinte.split("$")
    assert len(morceaux) == 4 and morceaux[0] == "PBKDF2-SHA256", empreinte
    iterations = int(morceaux[1])
    sel = base64.b64decode(morceaux[2])
    attendu = base64.b64decode(morceaux[3])
    obtenu = hashlib.pbkdf2_hmac("sha256", mot.encode(), sel, iterations, len(attendu))
    return obtenu == attendu


def lire_base():
    for _ in range(6):
        try:
            cnx = sqlite3.connect(f"file:{BASE}?mode=ro", uri=True)
            cnx.row_factory = sqlite3.Row
            return cnx
        except sqlite3.OperationalError:
            time.sleep(0.4)
    raise RuntimeError("Base SQLite illisible")


def compteur(cnx, table):
    return cnx.execute(f"SELECT COUNT(*) FROM [{table}]").fetchone()[0]


def admin_db():
    cnx = lire_base()
    try:
        ligne = cnx.execute(
            "SELECT identifiant, role, actif, mot_de_passe, DoitChangerMotDePasse "
            "FROM Utilisateurs WHERE identifiant = 'admin'").fetchone()
        assert ligne is not None, "admin absent de la base"
        return ligne
    finally:
        cnx.close()


def choisir_messagebox(fenetre, candidats):
    for nom in candidats:
        bouton = find_by_name(fenetre, nom, "Button")
        if bouton is not None:
            return bouton
    for nom in candidats:
        bouton = find_contains(fenetre, nom, "Button")
        if bouton is not None:
            return bouton
    raise AssertionError(f"Bouton {candidats} introuvable dans la boîte de dialogue")


def saisir_mot_de_passe(fenetre, mot):
    """Saisit le mot de passe : ValuePattern (les lettres ne sont pas au pavé)."""
    champ = (find_by_name(fenetre, "Mot de passe", "PasswordBox")
             or _premier_passwordbox(fenetre))
    assert champ is not None, "PasswordBox « Mot de passe » introuvable"
    _set_value(champ, mot)


def _premier_passwordbox(racine):
    for element in list(descendants(racine)) + list(raw_descendants(racine)):
        try:
            if element.CurrentClassName == "PasswordBox":
                return element
        except Exception:
            continue
    return None


def premiere_installation():
    log("PHASE 1 : première installation (création admin/admin)")
    login = attendre_fenetre(TITRE_LOGIN, timeout=TIMEOUT)
    assert login is not None, "LoginWindow absente au lancement"
    assert verifier_contenu_texte(login, "Première installation"), \
        "La fenêtre n'est pas en mode première installation"

    identifiant = (find_by_name(login, "Identifiant", "ComboBox")
                   or find_by_name(login, "Identifiant", "TextBox")
                   or find_by_name(login, "Identifiant", "Edit"))
    assert identifiant is not None, "Champ Identifiant introuvable"
    _set_value(identifiant, "admin")

    saisir_mot_de_passe(login, "admin")
    confirmation = find_by_name(login, "Confirmation du mot de passe", "PasswordBox")
    assert confirmation is not None, "Champ de confirmation introuvable"
    _set_value(confirmation, "admin")

    bouton = find_contains(login, "Créer l'administrateur", "Button")
    assert bouton is not None, "Bouton « Créer l'administrateur » introuvable"
    cliquer(bouton)

    complet = attendre_changement_obligatoire()
    cliquer(find_contains(complet, "Valider", "Button"))
    ouvrir_caisse_et_accueil()
    log("PHASE 1 OK : administrateur créé, session de caisse ouverte")


def attendre_changement_obligatoire():
    """Modale de changement obligatoire attendue après toute connexion avec flag."""
    fin = time.time() + TIMEOUT
    while time.time() < fin:
        modal = attendre_fenetre(TITRE_CHANGEMENT, timeout=0.5)
        if modal is not None:
            nouveau = find_by_automation_id(modal, "NouveauMotDePasse") \
                or _premier_passwordbox(modal)
            confirm = find_by_automation_id(modal, "Confirmation") \
                or _second_passwordbox(modal)
            assert nouveau is not None and confirm is not None, \
                "Champs de la modale de changement introuvables"
            _set_value(nouveau, MOT_DE_PASSE_FINAL)
            _set_value(confirm, MOT_DE_PASSE_FINAL)
            return modal
    raise AssertionError("Modale de changement obligatoire absente")


def _second_passwordbox(racine):
    vus = []
    for element in list(descendants(racine)) + list(raw_descendants(racine)):
        try:
            if element.CurrentClassName == "PasswordBox":
                vus.append(element)
        except Exception:
            continue
    return vus[1] if len(vus) > 1 else None


def ouvrir_caisse_et_accueil():
    ouverture = attendre_fenetre(TITRE_OUVERTURE, timeout=TIMEOUT)
    assert ouverture is not None, "Fenêtre d'ouverture de caisse absente"
    bouton = find_contains(ouverture, "Ouvrir la caisse", "Button")
    assert bouton is not None, "Bouton « Ouvrir la caisse » introuvable"
    cliquer(bouton)
    accueil = attendre_fenetre(TITRE_ACCUEIL, timeout=TIMEOUT)
    assert accueil is not None, "AccueilWindow absente après ouverture de caisse"
    return accueil


def effectuer_raz():
    log("PHASE 2 : RAZ depuis Outils (double confirmation)")
    accueil = attendre_fenetre(TITRE_ACCUEIL, timeout=TIMEOUT)
    assert accueil is not None, "AccueilWindow introuvable"
    naviguer_vers(accueil, "Outils")
    outils = _attendre_vue(accueil, "OUTILS POSTEK")

    bouton_raz = find_contains(outils, "RAZ", "Button")
    assert bouton_raz is not None, "Bouton RAZ introuvable"
    cliquer(bouton_raz)

    confirmation1 = attendre_fenetre("Confirmation 1/2", timeout=TIMEOUT)
    assert confirmation1 is not None, "Boîte « Confirmation 1/2 » absente"
    cliquer(choisir_messagebox(confirmation1, ("Oui", "Yes")))

    confirmation2 = attendre_fenetre("Confirmation RAZ", timeout=TIMEOUT)
    assert confirmation2 is not None, "Fenêtre « Confirmation RAZ » absente"
    champ = _premiere_zone_texte(confirmation2)
    assert champ is not None, "Zone de saisie du code RAZ introuvable"
    _set_value(champ, "RAZ")
    cliquer(find_contains(confirmation2, "Confirmer", "Button"))

    notification = _attendre_notification_raz()
    cliquer(choisir_messagebox(notification, ("OK",)))

    cnx = lire_base()
    try:
        assert compteur(cnx, "article") == 0, "articles non vidés"
        assert compteur(cnx, "ticket") == 0, "tickets non vidés"
        assert compteur(cnx, "HistoriqueConnexions") == 0, "historique non vidé"
        assert compteur(cnx, "SessionsCaisse") == 0, "sessions non vidées"
        admin = admin_db()
        assert admin["role"] == "Administrateur" and admin["actif"] == 1
        assert admin["DoitChangerMotDePasse"] == 1, "flag changement obligatoire absent"
        assert verifier_mdp(admin["mot_de_passe"], "admin"), \
            "l'empreinte admin ne correspond plus à « admin »"
    finally:
        cnx.close()
    log("PHASE 2 OK : données métier vidées, admin réinitialisé en admin/admin")


def _attendre_vue(accueil, texte, timeout=None):
    fin = time.time() + (timeout or TIMEOUT)
    while time.time() < fin:
        fenetre = attendre_fenetre(TITRE_ACCUEIL, timeout=0.5)
        if fenetre is not None and verifier_contenu_texte(fenetre, texte):
            return fenetre
        time.sleep(0.2)
    raise AssertionError(f"Vue « {texte} » non détectée dans l'Accueil")


def _premiere_zone_texte(racine):
    for element in list(descendants(racine)) + list(raw_descendants(racine)):
        try:
            if element.CurrentClassName in ("TextBox", "Edit"):
                return element
        except Exception:
            continue
    return None


def _attendre_notification_raz(timeout=TIMEOUT):
    fin = time.time() + timeout
    while time.time() < fin:
        for seulement in (True, False):
            for hwnd, titre in fenetres_visibles(seulement):
                if titre == "POSTEK":
                    element = _uia.ElementFromHandle(hwnd)
                    if verifier_contenu_texte(element, "Remise à zéro effectuée"):
                        return element
        time.sleep(0.3)
    raise AssertionError("Notification de fin de RAZ absente")


def reconnexion_admin():
    log("PHASE 3 : redémarrage puis reconnexion admin/admin")
    fermer_application()
    preparer_base_sans_reset()
    lancer_application()

    login = attendre_fenetre(TITRE_LOGIN, timeout=TIMEOUT)
    assert login is not None, "LoginWindow absente après redémarrage"
    assert not verifier_contenu_texte(login, "Première installation"), \
        "Première installation redétectée à tort après RAZ"

    identifiant = (find_by_name(login, "Identifiant", "ComboBox")
                   or find_by_name(login, "Identifiant", "TextBox")
                   or find_by_name(login, "Identifiant", "Edit"))
    assert identifiant is not None, "Champ Identifiant introuvable"
    _set_value(identifiant, "admin")
    saisir_mot_de_passe(login, "admin")

    valider = find_contains(login, "Valider", "Button")
    assert valider is not None, "Bouton Valider introuvable"
    cliquer(valider)

    modal = _attendre_changement_apres_login(login)
    nouveau = find_by_automation_id(modal, "NouveauMotDePasse") or _premier_passwordbox(modal)
    confirm = find_by_automation_id(modal, "Confirmation") or _second_passwordbox(modal)
    _set_value(nouveau, MOT_DE_PASSE_FINAL)
    _set_value(confirm, MOT_DE_PASSE_FINAL)
    cliquer(find_contains(modal, "Valider", "Button"))

    ouvrir_caisse_et_accueil()

    cnx = lire_base()
    try:
        admin = admin_db()
        assert admin["DoitChangerMotDePasse"] == 0, "flag changement toujours actif"
        assert verifier_mdp(admin["mot_de_passe"], MOT_DE_PASSE_FINAL)
        nombre = cnx.execute(
            "SELECT NombreConnexions FROM HistoriqueConnexions "
            "WHERE NomUtilisateur = 'admin'").fetchone()
        assert nombre is not None and nombre[0] >= 1, "historique de connexion absent"
    finally:
        cnx.close()
    log(f"PHASE 3 OK : admin/admin accepté après RAZ (mot de passe final "
        f"appliqué, historique de connexion enregistré)")


def _attendre_changement_apres_login(login):
    """Attend la modale obligatoire ; en cas d'échec, affiche l'erreur du login."""
    fin = time.time() + TIMEOUT
    while time.time() < fin:
        modal = attendre_fenetre(TITRE_CHANGEMENT, timeout=0.5)
        if modal is not None:
            return modal
        if verifier_contenu_texte(login, "incorrect"):
            raise AssertionError(
                "admin/admin refusé après RAZ : « Identifiant ou mot de passe incorrect »")
        time.sleep(0.2)
    raise AssertionError("Ni modale de changement ni Accueil après la connexion")


def preparer_base_sans_reset():
    """Fermeture de l'app uniquement (la base reste en l'état post-RAZ)."""
    tuer_application()


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if len(sys.argv) > 1:
        global EXE_DEFAUT, BASE
        EXE_DEFAUT = Path(sys.argv[1])
        BASE = EXE_DEFAUT.parent / "postek_demo.db"

    try:
        preparer_base()
        lancer_application()
        premiere_installation()
        effectuer_raz()
        reconnexion_admin()
        fermer_application()
        print("\n[RÉSULTAT] RAZ + reconnexion admin/admin : scénario complet validé à l'écran.")
    except Exception as exc:
        print(f"\n[ÉCHEC] {exc}")
        for hwnd, titre in fenetres_visibles(seulement_visibles=False):
            if "POSTEK" in titre or "Confirmation" in titre:
                print(f"    fenêtre hwnd={hwnd} titre={titre!r}")
        raise
    finally:
        tuer_application()


if __name__ == "__main__":
    main()
