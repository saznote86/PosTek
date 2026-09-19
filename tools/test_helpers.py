"""Helpers UIA partagés par les scénarios écran POSTEK."""

from __future__ import annotations

import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "postek_test_mix_ui", Path(__file__).with_name("test_mix_ui.py"))
_mix = importlib.util.module_from_spec(_spec)
assert _spec.loader is not None
_spec.loader.exec_module(_mix)

_uia = _mix._uia
_uic = _mix.uic
cliquer = _mix.cliquer
element_name = _mix.element_name
element_value = _mix.element_value
fenetres_visibles = _mix.fenetres_visibles
uia_par_handle = _mix.uia_par_handle
attendre_fenetre = _mix.attendre_fenetre
attendre_fenetre_principale = _mix.attendre_fenetre_principale
hwnd_fenetre_titre = _mix.hwnd_fenetre_titre
attendre_fermeture = _mix.attendre_fermeture


def descendants(root):
    walker = _uia.ControlViewWalker
    try:
        child = walker.GetFirstChildElement(root)
    except Exception:
        return
    while child:
        yield child
        yield from descendants(child)
        try:
            child = walker.GetNextSiblingElement(child)
        except Exception:
            return


def find_by_name(root, name, cls=None, retries=6):
    """Recherche UIA avec reprise silencieuse sur arbre WPF transitoire."""
    for _ in range(retries):
        try:
            cible = _mix._find_by_name_rec(root, name, cls)
            if cible is not None:
                return cible
            cible = _find_by_name_raw(root, name, cls)
            if cible is not None:
                return cible
        except Exception:
            pass
        import time
        time.sleep(0.25)
    return None


def find_by_automation_id(root, automation_id, retries=6):
    for _ in range(retries):
        try:
            condition = _uia.CreatePropertyCondition(
                _uic.UIA_AutomationIdPropertyId, automation_id)
            elements = root.FindAll(_uic.TreeScope_Descendants, condition)
            if elements.Length:
                return elements.GetElement(0)
            for element in descendants(root):
                if getattr(element, "CurrentAutomationId", "") == automation_id:
                    return element
            for element in raw_descendants(root):
                if getattr(element, "CurrentAutomationId", "") == automation_id:
                    return element
        except Exception:
            pass
        import time
        time.sleep(0.25)
    return None


def _find_by_name_raw(root, name, cls):
    for element in raw_descendants(root):
        if element_name(element) == name and (cls is None or element.CurrentClassName == cls):
            return element
    return None


def raw_descendants(root):
    walker = _uia.RawViewWalker
    try:
        child = walker.GetFirstChildElement(root)
    except Exception:
        return
    while child:
        yield child
        yield from raw_descendants(child)
        try:
            child = walker.GetNextSiblingElement(child)
        except Exception:
            return


def find_contains(root, text, cls=None):
    cible = text.casefold()
    try:
        for element in descendants(root):
            if cible in element_name(element).casefold():
                if cls is None or element.CurrentClassName == cls:
                    return element
    except Exception:
        return None
    return None


def verifier_contenu_texte(element, texte_attendu):
    cible = texte_attendu.casefold()
    try:
        return (cible in element_name(element).casefold()
              or any(cible in element_name(child).casefold()
                  for child in descendants(element))
              or any(cible in element_name(child).casefold()
                  for child in raw_descendants(element)))
    except Exception:
        return False


def naviguer_vers(fenetre, bouton_name):
    bouton = (find_by_name(fenetre, bouton_name, "Button")
              or find_contains(fenetre, bouton_name, "Button")
              or find_by_name(fenetre, bouton_name, "TabItem")
              or find_contains(fenetre, bouton_name, "TabItem"))
    if bouton is None:
        raise AssertionError(f"Contrôle UIA introuvable : {bouton_name!r}")
    if bouton.CurrentControlType == _uic.UIA_TabItemControlTypeId:
        brut = bouton.GetCurrentPattern(_uic.UIA_SelectionItemPatternId)
        if brut is None:
            raise RuntimeError(f"onglet sans SelectionItemPattern : {bouton_name!r}")
        brut.QueryInterface(_uic.IUIAutomationSelectionItemPattern).Select()
    else:
        cliquer(bouton)
    return bouton


def clavier_tactile_saisir(fenetre, valeur):
    for touche in str(valeur):
        bouton = find_by_name(fenetre, touche, "Button")
        if bouton is None:
            raise AssertionError(f"Touche tactile introuvable : {touche!r}")
        cliquer(bouton)


def attendre_fermeture_titre(titre, timeout=10.0):
    import time
    fin = time.time() + timeout
    while time.time() < fin:
        if all(t != titre for _, t in fenetres_visibles(False)):
            return True
        time.sleep(0.25)
    return False


def attendre_fermeture(hwnd, timeout=10.0):
    """Attend la disparition d'une fenêtre Win32 précise."""
    import ctypes
    import time
    fin = time.time() + timeout
    while time.time() < fin:
        if not ctypes.windll.user32.IsWindow(hwnd):
            return True
        time.sleep(0.25)
    return not ctypes.windll.user32.IsWindow(hwnd)
