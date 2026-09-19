#!/usr/bin/env python3
"""Analyse offline d'un dossier source Leo2 et produit un rapport JSON."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import datetime
from pathlib import Path


def analyser(source: Path) -> dict:
    fichiers = [f for f in source.rglob("*") if f.is_file()]
    par_extension = Counter((f.suffix.lower() or "[sans extension]") for f in fichiers)
    exports = sorted(
        str(f.relative_to(source)) for f in fichiers
        if f.suffix.lower() in {".xls", ".xlsx", ".csv"}
    )
    tables = sorted(
        str(f.relative_to(source)) for f in fichiers
        if f.suffix.lower() == ".fic"
    )
    sous_dossiers = sorted(
        str(d.relative_to(source)) for d in source.rglob("*") if d.is_dir()
    )
    taille = sum(f.stat().st_size for f in fichiers)
    dernier = max((f.stat().st_mtime for f in fichiers), default=source.stat().st_mtime)
    return {
        "source": str(source.resolve()),
        "fichiers_par_extension": dict(sorted(par_extension.items())),
        "exports_natifs": exports,
        "tables_fic": tables,
        "sous_dossiers": sous_dossiers,
        "nombre_fichiers": len(fichiers),
        "taille_totale_octets": taille,
        "derniere_modification": datetime.fromtimestamp(dernier).isoformat(),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("--rapport", type=Path, required=True)
    args = parser.parse_args()
    if not args.source.is_dir():
        parser.error(f"dossier introuvable : {args.source}")
    rapport = analyser(args.source)
    args.rapport.parent.mkdir(parents=True, exist_ok=True)
    args.rapport.write_text(json.dumps(rapport, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(rapport, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
