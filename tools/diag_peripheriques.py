"""Diagnostic temporaire : arbre UIA du Back-Office après clic Périphériques."""

import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

from test_helpers import (
    _uia, attendre_fenetre, cliquer, find_by_automation_id, find_by_name,
    naviguer_vers,
)


def dump(el, depth=0, max_depth=9):
    if depth > max_depth:
        return
    try:
        name = el.CurrentName or ""
        cls = el.CurrentClassName or ""
        aid = el.CurrentAutomationId or ""
    except Exception:
        return
    interessant = (name or aid or cls in (
        "TextBlock", "Button", "ContentControl", "UserControl", "TabItem"))
    if interessant:
        print("  " * depth + f"{cls} id={aid!r} name={name[:60]!r}")
    try:
        child = _uia.ControlViewWalker.GetFirstChildElement(el)
    except Exception:
        return
    while child:
        dump(child, depth + 1, max_depth)
        try:
            child = _uia.ControlViewWalker.GetNextSiblingElement(child)
        except Exception:
            return


backoffice = attendre_fenetre("POSTEK - Back-Office", timeout=3.0)
if backoffice is None:
    accueil = attendre_fenetre("POSTEK - Accueil", timeout=5.0)
    if accueil is None:
        print("Accueil absente")
        sys.exit(1)
    naviguer_vers(accueil, "Caisse")
    caisse = attendre_fenetre("POSTEK - Caisse", timeout=20.0)
    if caisse is None:
        print("Caisse absente")
        sys.exit(1)
    naviguer_vers(caisse, "Back-Office")
    backoffice = attendre_fenetre("POSTEK - Back-Office", timeout=20.0)
if backoffice is None:
    print("Back-Office absent")
    sys.exit(1)

print("Back-Office ouvert")
bouton = find_by_name(backoffice, "Périphériques")
if bouton is None:
    print("Bouton Périphériques introuvable")
    sys.exit(1)
cliquer(bouton)
time.sleep(3.0)

hote = find_by_automation_id(backoffice, "MainContentControl")
print("MainContentControl :", hote is not None)
zone = find_by_name(backoffice, "ZoneContenuPrincipal")
print("ZoneContenuPrincipal :", zone is not None)
print("--- arbre Back-Office (profondeur 9) ---")
dump(backoffice)
