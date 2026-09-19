"""Tests UI Automation du shell administrateur POSTEK.

Le script peut être lancé avec une application déjà ouverte (mode CI) ou
attendre une instance démarrée par l'appelant. Les actions passent par UIA,
jamais par la souris.

Variables utiles :
  POSTEK_ADMIN_USER      identifiant admin (défaut: admin)
  POSTEK_ADMIN_PASSWORD  mot de passe admin (défaut: 1234)
  POSTEK_ATTENTE_APP     délai d'attente des fenêtres
"""

import os
import sys
import time
from pathlib import Path

from test_helpers import (
    _uia, _uic, attendre_fenetre, attendre_fenetre_principale,
    cliquer, element_name, fenetres_visibles, find_by_name,
    find_by_automation_id,
    hwnd_fenetre_titre, naviguer_vers, verifier_contenu_texte,
    clavier_tactile_saisir, find_contains, attendre_fermeture,
)

TIMEOUT = float(os.environ.get("POSTEK_ATTENTE_APP", "30"))
ADMIN_USER = os.environ.get("POSTEK_ADMIN_USER", "admin")
ADMIN_PASSWORD = os.environ.get("POSTEK_ADMIN_PASSWORD", "1234")


def log(message):
    print(f"[*] {message}", flush=True)


def _descendants(root):
    walker = _uia.ControlViewWalker
    try:
        child = walker.GetFirstChildElement(root)
    except Exception:
        return
    while child:
        yield child
        yield from _descendants(child)
        try:
            child = walker.GetNextSiblingElement(child)
        except Exception:
            return


def attendre_contenu(titre, texte, timeout=TIMEOUT):
    fin = time.time() + timeout
    while time.time() < fin:
        fenetre = attendre_fenetre(titre, timeout=0.5)
        if fenetre is not None and verifier_contenu_texte(fenetre, texte):
            return fenetre
        time.sleep(0.2)
    return None


def attendre_vue_peripheriques(fenetre=None, timeout=TIMEOUT):
    fin = time.time() + timeout
    while time.time() < fin:
        root = fenetre or _fenetre_backoffice()
        if root is not None:
            vue = find_by_automation_id(root, "ParametresPeripheriquesView")
            if vue is None:
                hote = (find_by_automation_id(root, "MainContentControl")
                    or find_by_name(root, "ZoneContenuPrincipal"))
                if hote is not None:
                    vue = find_by_automation_id(hote, "ParametresPeripheriquesView")
            if vue is not None:
                boutons = (find_contains(vue, "Tester port COM", "Button"),
                       find_contains(vue, "Ouvrir tiroir", "Button"))
                if all(bouton is not None for bouton in boutons):
                    return vue
        time.sleep(0.3)
    return None


def _set_value(element, value):
    brut = element.GetCurrentPattern(_uic.UIA_ValuePatternId)
    if brut is None:
        raise RuntimeError(f"Champ sans ValuePattern : {element_name(element)!r}")
    brut.QueryInterface(_uic.IUIAutomationValuePattern).SetValue(value)


def _fenetre_login():
    return attendre_fenetre("POSTEK - Connexion", timeout=2.0)


def _fenetre_accueil():
    return attendre_fenetre("POSTEK - Accueil", timeout=2.0)


def _fenetre_backoffice():
    return attendre_fenetre("POSTEK - Back-Office", timeout=2.0)


def _connecter_si_necessaire():
    accueil = _fenetre_accueil()
    if accueil is not None:
        return accueil
    login = _fenetre_login()
    if login is None:
        raise AssertionError("Ni LoginWindow ni AccueilWindow ne sont détectés")

    log("LoginWindow détectée")
    identifiant = (find_by_name(login, "Identifiant", "ComboBox")
                   or find_by_name(login, "Identifiant", "TextBox")
                   or find_by_name(login, "Identifiant", "Edit"))
    if identifiant is None:
        raise AssertionError("Champ Identifiant introuvable")
    _set_value(identifiant, ADMIN_USER)

    clavier_tactile_saisir(login, ADMIN_PASSWORD)
    log("Mot de passe saisi via NumericKeypad")

    bouton = (find_contains(login, "Se connecter", "Button")
              or find_contains(login, "Sauvegarder", "Button")
              or find_contains(login, "Valider", "Button"))
    if bouton is None:
        raise AssertionError("Bouton de validation du login introuvable")
    cliquer(bouton)

    # Première installation : le champ de confirmation peut être visible.
    time.sleep(0.5)
    confirmation = find_contains(login, "Confirmation", "TextBlock")
    if confirmation is not None:
        log("Première installation détectée : confirmation du mot de passe")
        champs = [e for e in _descendants(login) if e.CurrentClassName == "PasswordBox"]
        if len(champs) > 1:
            champs[1].SetFocus()
            clavier_tactile_saisir(login, ADMIN_PASSWORD)
            cliquer(find_contains(login, "Sauvegarder", "Button"))

    accueil = attendre_fenetre("POSTEK - Accueil", timeout=TIMEOUT)
    if accueil is None:
        raise AssertionError("AccueilWindow non ouverte après authentification")
    return accueil


def _ouvrir_outils():
    accueil = _fenetre_accueil() or _connecter_si_necessaire()
    naviguer_vers(accueil, "Outils")
    outils = attendre_contenu("POSTEK - Accueil", "OUTILS POSTEK")
    if outils is None:
        raise AssertionError("OutilsView non ouverte")
    return outils


def test_login_admin():
    log("1/6 Login administrateur")
    accueil = _connecter_si_necessaire()
    assert verifier_contenu_texte(accueil, "Administrateur")
    log("AccueilWindow ouverte")
    # Le shell affiche le rôle via le contexte de session ; on accepte aussi
    # l'absence de texte si l'application est déjà authentifiée par la CI.
    assert _fenetre_accueil() is not None


def test_navigation_accueil_gestion():
    log("2/6 Navigation Accueil -> Gestion -> Retour")
    accueil = _fenetre_accueil() or _connecter_si_necessaire()
    naviguer_vers(accueil, "Gestion")
    assert attendre_contenu("POSTEK - Accueil", "Menu Gestion") is not None
    gestion = _fenetre_accueil()
    naviguer_vers(gestion, "RETOUR")
    assert attendre_contenu("POSTEK - Accueil", "Caisse") is not None
    log("Retour à AccueilWindow confirmé")


def test_navigation_outils():
    log("3/6 Navigation Accueil -> Outils")
    outils = _ouvrir_outils()
    for texte in ("Réindexer", "Mots de passe", "RAZ"):
        assert find_contains(outils, texte, "Button") is not None, texte
    naviguer_vers(outils, "Retour")
    assert _fenetre_accueil() is not None
    log("OutilsView et boutons principaux confirmés")


def test_modale_reindexer():
    log("4/6 Modale Réindexer")
    outils = _ouvrir_outils()
    naviguer_vers(outils, "Réindexer")
    modal = attendre_fenetre("Réindexer", timeout=TIMEOUT)
    assert modal is not None, "ReindexerModal absente"
    modal_hwnd = hwnd_fenetre_titre("Réindexer")
    assert modal_hwnd is not None
    assert find_contains(modal, "Répertoire", None) is not None
    naviguer_vers(modal, "Abandon")
    assert attendre_fermeture(modal_hwnd, timeout=10.0)
    naviguer_vers(outils, "Retour")
    log("ReindexerModal ouverte puis fermée")


def test_modale_mots_de_passe():
    log("5/6 Modale Mots de passe")
    outils = _ouvrir_outils()
    naviguer_vers(outils, "Mots de passe")
    modal = attendre_fenetre("Mots de passe", timeout=TIMEOUT)
    assert modal is not None, "MotsDePasseModal absente"
    modal_hwnd = hwnd_fenetre_titre("Mots de passe")
    assert modal_hwnd is not None
    for onglet in ("Mots de passe", "Fonctions caisse", "Accès gestion", "CLÉ USB"):
        assert find_contains(modal, onglet, "TabItem") is not None, onglet
    naviguer_vers(modal, "CLÉ USB")
    assert find_contains(modal, "Lecteur USB", None) is not None
    naviguer_vers(modal, "Valider")
    assert attendre_fermeture(modal_hwnd, timeout=10.0)
    naviguer_vers(outils, "Retour")
    log("MotsDePasseModal et onglets confirmés")


def _ouvrir_backoffice():
    backoffice = _fenetre_backoffice()
    if backoffice is not None:
        return backoffice
    # Depuis le shell admin, ouvrir la caisse affiche le bouton Back-Office.
    accueil = _fenetre_accueil() or _connecter_si_necessaire()
    naviguer_vers(accueil, "Caisse")
    caisse = attendre_fenetre("POSTEK - Caisse", timeout=TIMEOUT)
    if caisse is None:
        raise AssertionError("MainWindow absente pour ouvrir le Back-Office")
    naviguer_vers(caisse, "Back-Office")
    backoffice = attendre_fenetre("POSTEK - Back-Office", timeout=TIMEOUT)
    if backoffice is None:
        raise AssertionError("BackOffice absent")
    return backoffice


def test_navigation_peripheriques():
    log("6/6 Navigation Back-Office -> Administration (onglet Paramètres)")
    backoffice = _ouvrir_backoffice()
    naviguer_vers(backoffice, "Administration")
    administration = attendre_fenetre("POSTEC - Administration", timeout=TIMEOUT)
    assert administration is not None, "AdministrationView absente"
    admin_hwnd = hwnd_fenetre_titre("POSTEC - Administration")
    vue = attendre_vue_peripheriques(administration, timeout=TIMEOUT)
    assert vue is not None, "ParametresPeripheriquesView absente"
    for texte in ("VERSIONS", "NOM", "OS", "Tester port COM", "Ouvrir tiroir"):
        assert find_contains(vue, texte, None) is not None, texte
    # Fermer l'Administration (équivalent Retour)
    administration.Close()
    if admin_hwnd is not None:
        assert attendre_fermeture(admin_hwnd, timeout=10.0)
    log("AdministrationView et onglet Paramètres confirmés")


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    tests = (test_login_admin, test_navigation_accueil_gestion, test_navigation_outils,
             test_modale_reindexer, test_modale_mots_de_passe,
             test_navigation_peripheriques)
    try:
        for test in tests:
            test()
        print("\n[RÉSULTAT] Tests Admin UIA : 6/6 vérifications passées.")
    except Exception as exc:
        print(f"\n[ÉCHEC] {exc}")
        for hwnd, titre in fenetres_visibles(seulement_visibles=False):
            print(f"    fenêtre hwnd={hwnd} titre={titre!r}")
        raise


if __name__ == "__main__":
    main()
