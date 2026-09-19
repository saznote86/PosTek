"""Vérifie les raccourcis clavier de la fenêtre « Encaissement » (UI Automation).

Pré-requis : l'app POSTEK doit être lancée. Nécessite `pip install comtypes`.

Scénario :
  1. Échap referme la fenêtre de paiement — ticket intact, aucun règlement ;
  2. Entrée sur un ticket non soldé = ignorée (la fenêtre reste ouverte) ;
  3. Entrée sur un ticket soldé = valide l'encaissement : fenêtre fermée,
     +1 ticket en base, règlement CB exact persisté.

Le pas 3 encaisse réellement un ticket (1,800 TND, CB) dans la base démo
`postek_demo.db` du poste de test — c'est le seul effet de bord voulu.
"""
import ctypes
import ctypes.wintypes
import os
import sqlite3
import sys
import time

import importlib.util

_chemin = __file__.replace("test_clavier_ui.py", "test_mix_ui.py")
_spec = importlib.util.spec_from_file_location("test_mix_ui", _chemin)
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)   # exécute tout sauf main() (guard __main__)

find, cliquer = _mod.find_by_name, _mod.cliquer

VK_ESCAPE, VK_RETURN = 0x1B, 0x0D


def _exe_de_fenetre(hwnd):
    """Chemin de l'exécutable propriétaire d'une fenêtre (QueryFullProcessImageName)."""
    pid = ctypes.wintypes.DWORD()
    ctypes.windll.user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    if not pid.value:
        return None
    PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
    h = ctypes.windll.kernel32.OpenProcess(
        PROCESS_QUERY_LIMITED_INFORMATION, False, pid.value)
    if not h:
        return None
    try:
        taille = ctypes.wintypes.DWORD(1024)
        buf = ctypes.create_unicode_buffer(1024)
        ok = ctypes.windll.kernel32.QueryFullProcessImageNameW(
            h, 0, buf, ctypes.byref(taille))
        return buf.value if ok else None
    finally:
        ctypes.windll.kernel32.CloseHandle(h)


def _resoudre_base_demo():
    """Chemin de la base démo de l'app réellement lancée.

    Résolution depuis le processus (fenêtre POSTEK → PID → chemin de l'exe),
    sinon surcharge POSTEK_DEMO_DB, sinon repli Debug puis Release — l'ordre
    importe peu : ce qui compte, c'est la base du binaire qui tourne, car les
    répertoires Debug et Release coexistent et chacun a SA base.
    """
    hwnd, _ = _mod.attendre_fenetre_principale(timeout=10)
    if hwnd is not None:
        exe = _exe_de_fenetre(hwnd)
        if exe:
            return os.path.join(os.path.dirname(exe), "postek_demo.db")

    env = os.environ.get("POSTEK_DEMO_DB")
    if env:
        return env

    racine = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # postek/
    for conf in ("Debug", "Release"):
        chemin = os.path.join(racine, "desktop", "Postek.Caisse", "bin", conf,
                              "net8.0-windows", "postek_demo.db")
        if os.path.exists(chemin):
            return chemin
    return os.path.join(racine, "desktop", "Postek.Caisse", "bin", "Debug",
                        "net8.0-windows", "postek_demo.db")


_base_cache = None


def chemin_base_demo():
    global _base_cache
    if _base_cache is None:
        _base_cache = _resoudre_base_demo()
    return _base_cache


def tickets_en_base():
    """(nb tickets, règlements du dernier ticket) — lecture seule."""
    base = os.path.abspath(chemin_base_demo())
    for tentative in range(5):
        try:
            con = sqlite3.connect(f"file:{base}?mode=ro", uri=True)
            try:
                nb = con.execute("SELECT COUNT(*) FROM ticket").fetchone()[0]
                regs = con.execute(
                    "SELECT mode, montant FROM reglement_ticket "
                    "WHERE ticket_numero = (SELECT MAX(numero) FROM ticket) "
                    "ORDER BY position").fetchall()
            finally:
                con.close()
            return nb, regs
        except sqlite3.OperationalError:
            time.sleep(0.4)   # base momentanément verrouillée par l'app
    raise RuntimeError("base démo illisible (verrouillage persistant)")


def saisir(paiement, texte):
    """Tape `texte` sur le pavé tactile de la fenêtre de paiement."""
    for touche in texte:
        cliquer(find(paiement, touche, "Button"))


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    hwnd, racine = _mod.attendre_fenetre_principale()
    if racine is None:
        print("[!] Fenêtre principale POSTEK introuvable — lancez l'app d'abord.")
        sys.exit(1)
    print(f"[*] Fenêtre principale (hwnd={hwnd})")

    def etat_propre():
        """Referme un paiement resté ouvert et vide le ticket en cours."""
        reste = _mod.paiement_ouvert()
        if reste is not None:
            r = find(reste, "← Retour", "Button")
            if r is not None:
                cliquer(r)
                time.sleep(0.6)
        vider = find(racine, "Vider", "Button")
        if vider is not None:
            cliquer(vider)
            time.sleep(0.3)

    def ouvrir_paiement():
        """Ticket café + croissant (1,800) → fenêtre Encaissement ouverte."""
        etat_propre()
        for art in ("Café express", "Croissant"):
            b = find(racine, art, "Button")
            assert b is not None, f"bouton {art} introuvable"
            cliquer(b)
            time.sleep(0.2)
        b = find(racine, "Encaisser", "Button")
        assert b is not None, "bouton Encaisser introuvable"
        cliquer(b)
        p = _mod.attendre_fenetre("Encaissement", timeout=8.0)
        assert p is not None, "fenêtre « Encaissement » non ouverte"
        return p

    def hwnd_paiement():
        return _mod.hwnd_fenetre_titre("Encaissement")

    # ------------------------------------------------------------------
    # 1) Échap referme la fenêtre — rien n'est encaissé.
    # ------------------------------------------------------------------
    etat_propre()
    avant, _ = tickets_en_base()

    p = ouvrir_paiement()
    saisir(p, "2,000")                       # saisie abandonnée…
    cliquer(find(p, "CB", "Button"))         # …réglée en CB mais non validée
    time.sleep(0.4)

    _mod.envoyer_touche(hwnd_paiement(), VK_ESCAPE)
    assert _mod.attendre_fermeture(), "Échap n'a pas refermé la fenêtre"
    nb, _ = tickets_en_base()
    assert nb == avant, f"Échap a encaissé un ticket ({avant} → {nb}) !"
    print("[OK] 1. Échap : fenêtre refermée, ticket intact, rien en base")

    # ------------------------------------------------------------------
    # 2) Entrée sur ticket non soldé : ignorée, fenêtre toujours ouverte.
    # ------------------------------------------------------------------
    p = ouvrir_paiement()
    saisir(p, "1,000")
    cliquer(find(p, "CB", "Button"))         # 1,000 réglé sur 1,800 → non soldé
    time.sleep(0.4)

    _mod.envoyer_touche(hwnd_paiement(), VK_RETURN)

    # Attendre que l'app ait *traité* la touche : la fenêtre doit rester
    # ouverte (guard non soldé) — on sonde pour laisser passer le traitement.
    assert _mod.attendre_fermeture() is False, \
        "Entrée a refermé la fenêtre alors que le ticket n'était pas soldé !"
    nb, _ = tickets_en_base()
    assert nb == avant, "Entrée non soldée a encaissé un ticket !"
    print("[OK] 2. Entrée sans solde : ignorée, fenêtre toujours ouverte")

    # Sortie propre de l'étape 2 : Échap (la saisie 1,000 CB est abandonnée).
    _mod.envoyer_touche(hwnd_paiement(), VK_ESCAPE)
    assert _mod.attendre_fermeture()

    # ------------------------------------------------------------------
    # 3) Entrée sur ticket soldé = valider l'encaissement.
    #    Solde exact : CB sans saisie → règle le reste (1,800).
    # ------------------------------------------------------------------
    p = ouvrir_paiement()
    cliquer(find(p, "CB", "Button"))         # reste à payer ⇒ soldé
    time.sleep(0.4)

    _mod.envoyer_touche(hwnd_paiement(), VK_RETURN)

    # Entrée soldée = valider : on attend la fermeture (jusqu'à 15 s — le
    # traitement inclut persistance + impression ESC/POS).
    assert _mod.attendre_fermeture(timeout=15.0), "Entrée soldée n'a pas validé"
    nb, regs = tickets_en_base()
    assert nb == avant + 1, f"ticket non persisté par Entrée ({avant} → {nb})"
    assert regs == [("cb", "1.800")], f"règlement inattendu : {regs}"
    print("[OK] 3. Entrée soldée : encaissement validé, ticket + CB 1,800 en base")

    print("\n[RÉSULTAT] Raccourcis clavier paiement : 3/3 vérifications passées.")


if __name__ == "__main__":
    main()
