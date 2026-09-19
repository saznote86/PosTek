"""Attribution des fenêtres POSTEK à leur processus propriétaire.

Usage : python postek/tools/attribuer_fenetres.py
Liste chaque fenêtre visible dont le titre commence par POSTEK,
Encaissement ou Confirmation, avec (PID, nom du process, titre).
"""
import ctypes
import subprocess
import sys
from ctypes import wintypes

EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)


def fenetres_filtrees():
    """(pid, titre) des fenêtres visibles dont le titre intéresse le diagnostic."""
    resultats = []

    @EnumWindowsProc
    def callback(hwnd, _lparam):
        if not ctypes.windll.user32.IsWindowVisible(hwnd):
            return True
        n = ctypes.windll.user32.GetWindowTextLengthW(hwnd)
        if n <= 0:
            return True
        buf = ctypes.create_unicode_buffer(n + 1)
        ctypes.windll.user32.GetWindowTextW(hwnd, buf, n + 1)
        titre = buf.value
        prefixes = ("POSTEK", "Encaissement", "Confirmation")
        if titre.startswith(prefixes):
            pid = wintypes.DWORD()
            ctypes.windll.user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            resultats.append((int(pid.value), titre))
        return True

    ctypes.windll.user32.EnumWindows(callback, 0)
    return resultats


def nom_processus(pid):
    """Nom d'exécutable du PID, vide si introuvable."""
    sortie = subprocess.run(
        ["tasklist", "/FI", f"PID eq {pid}", "/FO", "CSV", "/NH"],
        capture_output=True, text=True,
    ).stdout
    if not sortie.strip():
        return "(process terminé)"
    premier = sortie.strip().splitlines()[0]
    return premier.split('","')[0].strip('"')


def main():
    fenetres = fenetres_filtrees()
    if not fenetres:
        print("Aucune fenêtre POSTEK/Encaissement/Confirmation visible.")
        return
    for pid, titre in sorted(fenetres):
        print(f"PID {pid:7d} | {nom_processus(pid):20s} | {titre}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
