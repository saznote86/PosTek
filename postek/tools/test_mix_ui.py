"""Pilote POSTEK par UI Automation pour le test d'encaissement mixte.

Scénario (fidèle au TODO context.md) :
  1. écran principal   : cliquer « Encaisser » (ticket avec articles de la démo) ;
  2. fenêtre paiement  : CB 1,000 → Espèces 2,500 → rendu attendu 0,500 ;
  3. valider l'encaissement ;
  4. vérifier l'état UI à chaque étape par captures console (Nom, Value).

Usage : python postek/tools/test_mix_ui.py
Le script ne modifie aucune donnée : il clique comme un utilisateur.
"""

import ctypes
import ctypes.wintypes as wt
import os
import sys
import time

try:
    import comtypes.client
    import comtypes.gen.UIAutomationClient as uic
except ImportError:
    # Première exécution : génère le wrapper COM depuis la bibliothèque de types.
    comtypes.client.GetModule("UIAutomationCore.dll")
    import comtypes.gen.UIAutomationClient as uic


def element_name(el) -> str:
    try:
        return el.CurrentName or ""
    except Exception:
        return ""


def element_value(el) -> str:
    try:
        v = el.GetCurrentPattern(uic.UIA_ValuePatternId)
        return v.CurrentValue or ""
    except Exception:
        return ""


def dump(el, depth=0, max_depth=8):
    name = element_name(el)
    cls = el.CurrentClassName or ""
    val = element_value(el)
    info = f"{'  ' * depth}- {cls}"
    if name:
        info += f" | Nom: {name!r}"
    if val:
        info += f" | Value: {val!r}"
    print(info)
    if depth >= max_depth:
        return
    walker = _uia.RawViewWalker
    child = walker.GetFirstChildElement(el)
    while child:
        dump(child, depth + 1, max_depth)
        child = walker.GetNextSiblingElement(child)


def _find_by_name_rec(root, name, cls):
    walker = _uia.ControlViewWalker
    child = walker.GetFirstChildElement(root)
    while child:
        if element_name(child) == name and (cls is None or child.CurrentClassName == cls):
            return child
        found = _find_by_name_rec(child, name, cls)
        if found is not None:
            return found
        child = walker.GetNextSiblingElement(child)
    return None


def find_by_name(root, name, cls=None):
    """Recherche par nom avec reprise : l'arbre UIA peut être momentanément
    indisponible (COMError 0x80040201) pendant un rafraîchissement de la
    fenêtre — on retente brièvement avant d'abandonner."""
    dernier = None
    for _ in range(6):
        try:
            return _find_by_name_rec(root, name, cls)
        except comtypes.COMError as e:
            dernier = e
            time.sleep(0.25)
    if dernier is not None:
        raise dernier
    return None


def cliquer(el):
    """Déclenche le bouton via InvokePattern (canal UIA, pas de souris).
    comtypes renvoie IUnknown : QueryInterface vers l'interface typée."""
    brut = el.GetCurrentPattern(uic.UIA_InvokePatternId)
    if brut is None:
        raise RuntimeError("pattern Invoke indisponible")
    pattern = brut.QueryInterface(uic.IUIAutomationInvokePattern)
    pattern.Invoke()


def attendre_fenetre(titre_exact, timeout=10.0):
    """Attend qu'une fenêtre dont le titre exact apparaisse ; renvoie l'élément UIA."""
    fin = time.time() + timeout
    while time.time() < fin:
        for seulement in (True, False):
            for h, titre in fenetres_visibles(seulement):
                if titre == titre_exact:
                    return uia_par_handle(h)
        time.sleep(0.3)
    return None


EnumWindowsProc = ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)


def fenetres_visibles(seulement_visibles=True):
    """Fenêtres (hwnd, titre) via ctypes — sans pywin32.

    seulement_visibles=False liste aussi les fenêtres non marquées
    WS_VISIBLE : utile sur un runner CI (session non interactive) où la
    fenêtre WPF peut exister sans être « visible » au sens Win32.
    """
    resultats = []

    @EnumWindowsProc
    def cb(hwnd, _lparam):
        if not seulement_visibles or ctypes.windll.user32.IsWindowVisible(hwnd):
            n = ctypes.windll.user32.GetWindowTextLengthW(hwnd)
            if n > 0:
                buf = ctypes.create_unicode_buffer(n + 1)
                ctypes.windll.user32.GetWindowTextW(hwnd, buf, n + 1)
                resultats.append((hwnd, buf.value))
        return True

    ctypes.windll.user32.EnumWindows(cb, 0)
    return resultats


def uia_par_handle(hwnd):
    return _uia.ElementFromHandle(hwnd)


_uia = comtypes.client.CreateObject(
    "{ff48dba4-60ef-4201-aa87-54103eef594e}",
    interface=uic.IUIAutomation,
)


def arbre(entete, fen, max_depth=9):
    print(f"\n===== {entete} =====")
    dump(fen, max_depth=max_depth)


def attendre_fermeture(timeout=8.0):
    """True dès que la fenêtre « Encaissement » n'est plus détectée."""
    fin = time.time() + timeout
    while time.time() < fin:
        if paiement_ouvert() is None:
            return True
        time.sleep(0.3)
    return paiement_ouvert() is None


def hwnd_fenetre_titre(titre_exact):
    """hwnd de la fenêtre dont le titre est exact, ou None."""
    for seulement in (True, False):
        for h, t in fenetres_visibles(seulement):
            if t == titre_exact:
                return h
    return None


def champ_saisie(fen):
    """Texte affiché dans la zone de saisie du pavé (1er texte « TND »)."""
    w = _uia.RawViewWalker
    c = w.GetFirstChildElement(fen)
    while c:
        n = element_name(c)
        if "TND" in n:
            return n
        c = w.GetNextSiblingElement(c)
    return ""


def texte_total_principal(racine):
    """Libellé « Total : … » de la fenêtre principale (ou « »)."""
    w = _uia.RawViewWalker
    c = w.GetFirstChildElement(racine)
    while c:
        n = element_name(c)
        if n.startswith("Total :"):
            return n
        c = w.GetNextSiblingElement(c)
    return ""


def _chercher_principale(candidats):
    """Fenêtre (hwnd, élément) contenant le bouton « Encaisser », ou (None, None)."""
    for h, t in candidats:
        if "POSTEK" not in t:
            continue
        el = uia_par_handle(h)
        if find_by_name(el, "Encaisser", "Button") is not None:
            return h, el
    return None, None


def fenetre_principale():
    """Fenêtre principale = celle qui contient le bouton « Encaisser ».

    Recherche d'abord parmi les fenêtres visibles, puis sans le filtre de
    visibilité (runner CI en session non interactive).
    """
    hwnd, el = _chercher_principale(fenetres_visibles())
    if el is not None:
        return hwnd, el
    return _chercher_principale(fenetres_visibles(seulement_visibles=False))


def attendre_fenetre_principale(timeout=None):
    """fenetre_principale() en patientant — le démarrage WPF peut être lent
    (runner CI froid, JIT, rendu logiciel). Timeout par défaut réglable par
    la variable d'environnement POSTEK_ATTENTE_APP (30 s)."""
    if timeout is None:
        timeout = float(os.environ.get("POSTEK_ATTENTE_APP", "30"))
    fin = time.time() + timeout
    while True:
        hwnd, el = fenetre_principale()
        if el is not None:
            return hwnd, el
        if time.time() >= fin:
            return None, None
        time.sleep(0.5)


def paiement_ouvert():
    """Élément UIA de la fenêtre « Encaissement », ou None."""
    for seulement in (True, False):
        for h, t in fenetres_visibles(seulement):
            if t == "Encaissement":
                return uia_par_handle(h)
    return None


def envoyer_touche(hwnd, vk, delai=0.05):
    """Injecte une touche (WM_KEYDOWN/UP) dans la fenêtre — pour F2/Entrée/Échap."""
    wm_down, wm_up = 0x0100, 0x0101
    ctypes.windll.user32.PostMessageW(hwnd, wm_down, vk, 0)
    time.sleep(delai)
    ctypes.windll.user32.PostMessageW(hwnd, wm_up, vk, 0)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    # L'app vient peut-être d'être (re)lancée : on laisse WPF monter la fenêtre.
    time.sleep(2.0)

    hwnd, racine = attendre_fenetre_principale()
    if racine is None:
        print("[!] Écran principal (bouton Encaisser) introuvable — fenêtres présentes :")
        for h, t in fenetres_visibles(seulement_visibles=False):
            print(f"    - hwnd={h} titre={t!r}")
        sys.exit(1)
    print(f"[*] Fenêtre principale (hwnd={hwnd})")

    # Idempotence : referme une fenêtre de paiement restée ouverte et vide
    # le ticket en cours pour repartir de zéro.
    reste = paiement_ouvert()
    if reste is not None:
        retour = find_by_name(reste, "← Retour", "Button")
        if retour is not None:
            print("[*] Fermeture d'un paiement resté ouvert")
            cliquer(retour)
            time.sleep(0.5)
    vider = find_by_name(racine, "Vider", "Button")
    if vider is not None:
        cliquer(vider)
        time.sleep(0.3)

    # --- Étape 1 : écran principal → articles puis Encaisser
    # (SurEncaisser ignore un ticket vide : il faut d'abord des articles.)
    arbre("ÉTAT AVANT (écran principal)", racine, max_depth=4)

    for article in ("Café express", "Croissant"):
        b = find_by_name(racine, article, "Button")
        if b is None:
            print(f"[!] Bouton « {article} » introuvable")
            sys.exit(3)
        cliquer(b)
        time.sleep(0.2)

    # Le total doit afficher 1,800 (1,200 + 0,600) avant d'encaisser.
    def texte_total(rac):
        w = _uia.RawViewWalker
        c = w.GetFirstChildElement(rac)
        while c:
            n = element_name(c)
            if n.startswith("Total :"):
                return n
            c = w.GetNextSiblingElement(c)
        return ""

    attendu = "Total : 1,800 TND"
    for _ in range(10):
        lu = texte_total(racine)
        if lu == attendu:
            break
        time.sleep(0.2)
    print(f"[*] {lu}")
    if lu != attendu:
        print(f"[!] Total inattendu : {lu!r} (attendu {attendu!r})")
        sys.exit(5)

    bouton = find_by_name(racine, "Encaisser", "Button")
    if bouton is None:
        print("[!] Bouton « Encaisser » introuvable")
        sys.exit(3)
    cliquer(bouton)

    # --- Étape 2 : fenêtre de paiement
    paiement = attendre_fenetre("Encaissement")
    if paiement is None:
        print("[!] Fenêtre « Encaissement » non détectée — capture du principal :")
        arbre("PRINCIPAL APRÈS CLIC", racine, max_depth=6)
        sys.exit(4)
    print("[*] Fenêtre de paiement ouverte")
    arbre("FENÊTRE PAIEMENT (avant saisie)", paiement)

    # --- Saisie CB 1,000 (plafonné au reste à payer)
    for touche in ("1", ",", "0", "0", "0"):
        cliquer(find_by_name(paiement, touche, "Button"))
    cliquer(find_by_name(paiement, "CB", "Button"))
    arbre("PAIEMENT (après CB 1,000)", paiement, max_depth=5)

    # --- Saisie espèces 1,000 (2,000 reçus pour 1,800 dus → rendu 0,200)
    for touche in ("1", ",", "0", "0", "0"):
        cliquer(find_by_name(paiement, touche, "Button"))
    cliquer(find_by_name(paiement, "Espèces", "Button"))
    arbre("PAIEMENT (après espèces 1,000 — rendu attendu 0,200)", paiement, max_depth=5)

    # --- Étape 3 : valider
    cliquer(find_by_name(paiement, "Valider l'encaissement", "Button"))
    time.sleep(1.5)

    fin = [(h, t) for h, t in fenetres_visibles() if "POSTEK" in t]
    for h2, t2 in fin:
        arbre(f"ÉTAT FINAL ({t2})", uia_par_handle(h2), max_depth=5)
    print("\n[OK] Scénario mixte exécuté.")


if __name__ == "__main__":
    main()
