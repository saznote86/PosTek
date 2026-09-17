"""Vérifie le raccourci F2 = encaisser sur l'app POSTEK réelle (UI Automation).

Pré-requis : l'app doit être lancée (bin/Debug/net8.0-windows/Postek.Caisse.exe).
Ne modifie aucune donnée : encaissement jamais validé (Retour à la fin).

Scénario :
  1. F2 sur ticket vide   → aucune fenêtre de paiement ne s'ouvre ;
  2. café + croissant     → total 1,800 TND affiché ;
  3. F2 sur ticket plein  → fenêtre « Encaissement » ouverte, à payer 1,800 ;
  4. « ← Retour »         → fenêtre fermée, ticket intact, aucun ticket en base.
"""

import sys
import time

import importlib.util

_spec = importlib.util.spec_from_file_location(
    "test_mix_ui", __file__.replace("test_f2_ui.py", "test_mix_ui.py"))
_mod = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_mod)   # exécute tout sauf main() (guard __main__)

uia = _mod.uia_par_handle  # noqa: F841  (réexport pour lisibilité)
find, name, cliquer = _mod.find_by_name, _mod.element_name, _mod.cliquer


def texte_total(racine):
    w = _mod._uia.RawViewWalker
    c = w.GetFirstChildElement(racine)
    while c:
        n = name(c)
        if n.startswith("Total :"):
            return n
        c = w.GetNextSiblingElement(c)
    return ""


def lire_a_payer(paiement):
    w = _mod._uia.RawViewWalker
    c = w.GetFirstChildElement(paiement)
    while c:
        n = name(c)
        if "TND" in n and n != "":
            return n
        c = w.GetNextSiblingElement(c)
    return ""


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    hwnd, racine = _mod.attendre_fenetre_principale()
    if racine is None:
        print("[!] Fenêtre principale POSTEK introuvable — lancez l'app d'abord.")
        sys.exit(1)
    print(f"[*] Fenêtre principale (hwnd={hwnd})")

    # État initial : refermer un paiement resté ouvert + vider le ticket.
    reste = _mod.paiement_ouvert()
    if reste is not None:
        retour = find(reste, "← Retour", "Button")
        if retour is not None:
            cliquer(retour)
            time.sleep(0.6)
    vider = find(racine, "Vider", "Button")
    if vider is not None:
        cliquer(vider)
        time.sleep(0.3)

    # 1) F2 sur ticket vide : doit être ignoré.
    _mod.envoyer_touche(hwnd, 0x71)   # VK_F2
    time.sleep(1.0)
    assert _mod.paiement_ouvert() is None, "F2 a ouvert le paiement sur ticket vide !"
    print("[OK] 1. F2 sur ticket vide : ignoré")

    # 2) Articles → total attendu.
    for art in ("Café express", "Croissant"):
        b = find(racine, art, "Button")
        assert b is not None, f"bouton {art} introuvable"
        cliquer(b)
        time.sleep(0.2)
    total = texte_total(racine)
    assert total == "Total : 1,800 TND", f"total inattendu : {total!r}"
    print(f"[OK] 2. {total}")

    # 3) F2 sur ticket plein : la fenêtre de paiement s'ouvre.
    _mod.envoyer_touche(hwnd, 0x71)
    paiement = _mod.attendre_fenetre("Encaissement", timeout=15.0)
    assert paiement is not None, "F2 n'a pas ouvert la fenêtre de paiement"
    a_payer = lire_a_payer(paiement)
    assert a_payer == "1,800 TND", f"« à payer » inattendu : {a_payer!r}"
    print(f"[OK] 3. F2 a ouvert l'encaissement — À payer {a_payer}")

    # 4) Retour : rien n'est encaissé.
    cliquer(find(paiement, "← Retour", "Button"))
    assert _mod.attendre_fermeture(), "la fenêtre ne s'est pas refermée"
    print("[OK] 4. Retour : fenêtre fermée, ticket intact, aucun encaissement")
    print("\n[RÉSULTAT] F2 = encaisser : 4/4 vérifications passées.")


if __name__ == "__main__":
    main()
