#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
fingerprint.py — Empreinte matérielle POSTEK : CPU + carte mère.

Remplace le verrou « numéro de série HDD » de l'application d'origine
(`hdd.ini` → `HDD=1028`, `info_lic_leo2`). L'empreinte combine :
  - le ProcessorId du premier CPU       [Win32_Processor]
  - le SerialNumber de la carte mère    [Win32_BaseBoard]
  - le fabricant de la carte mère       [Win32_BaseBoard]

Résultat : chaîne `POSTEK1-XXXX-XXXX-XXXX-XXXX` (Base32 sans 0/O/1/I),
stable sur la machine : identique tant que CPU **et** carte mère sont
présents. Sous Windows, l'interrogation WMI est tentée en premier ; si
WMI est indisponible (ou hors Windows), on retombe sur une empreinte
plateforme (machine + platform + cpu info) de qualité moindre, ce qui
garantit qu'une caisse démarre toujours.
"""

from __future__ import annotations

import hashlib
import platform
import subprocess
import sys

BASE32_ALPHABET = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"  # sans 0,O,1,I


def _wmi_query(wmi_class: str, wmi_property: str) -> str | None:
    """Interroge WMI en local ; renvoie None si indisponible."""
    if sys.platform != "win32":
        return None
    try:
        out = subprocess.run(
            ["wmic", wmi_class, "get", wmi_property],
            capture_output=True,
            text=True,
            timeout=8,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
    except (OSError, subprocess.SubprocessError):
        return None
    if out.returncode != 0:
        return None
    lines = [
        ln.strip()
        for ln in (out.stdout or "").splitlines()
        if ln.strip() and ln.strip().lower() != wmi_property.lower()
    ]
    return lines[0] if lines else None


def _seed_materiel() -> str:
    """Construit la chaîne brute servant de base à l'empreinte."""
    parties: list[str] = []

    cpu = _wmi_query("cpu", "ProcessorId") if sys.platform == "win32" else None
    if cpu:
        parties.append(f"CPU:{cpu}")
    else:
        parties.append(f"CPU:{platform.processor() or 'inconnu'}")

    if sys.platform == "win32":
        board_sn = _wmi_query("baseboard", "SerialNumber") or ""
        board_mfr = _wmi_query("baseboard", "Manufacturer") or ""
        if board_sn or board_mfr:
            parties.append(f"BOARD:{board_mfr}|{board_sn}")
        else:
            # WMI indisponible : seed dégradé, clairement marqué.
            parties.append(f"BOARD:DEGRADED|{platform.node()}")
    else:
        parties.append(f"BOARD:{platform.node()}")

    return "||".join(parties)


def empreinte_materielle() -> str:
    """Empreinte matérielle lisible, ex. `POSTEK1-K7Q2-9MTX-3ABP-CDEF`."""
    digest = hashlib.sha256(_seed_materiel().encode("utf-8")).digest()
    chiffres = []
    n = int.from_bytes(digest[:10], "big")
    for _ in range(20):
        n, r = divmod(n, 32)
        chiffres.append(BASE32_ALPHABET[r])
    blocs = ["".join(chiffres[i:i + 4]) for i in range(0, 20, 4)]
    return "POSTEK1-" + "-".join(blocs)


def empreinte_normalisee() -> str:
    """Forme normalisée (sans préfixe) utilisée dans les jetons de licence."""
    return empreinte_materielle().replace("POSTEK1-", "").replace("-", "")


if __name__ == "__main__":
    print("Seed       :", _seed_materiel())
    print("Empreinte  :", empreinte_materielle())
