#!/usr/bin/env python3
"""Valide une base SQLite produite par la migration offline réelle."""

from __future__ import annotations

import argparse
import json
import sqlite3
from pathlib import Path


def compter(cnx: sqlite3.Connection, table: str) -> int:
    try:
        return int(cnx.execute(f"SELECT COUNT(*) FROM {table}").fetchone()[0])
    except sqlite3.Error:
        return 0


def valider(base: Path) -> dict:
    with sqlite3.connect(base) as cnx:
        resultats = {
            "Produits": compter(cnx, "produit"),
            "Clients": compter(cnx, "client"),
            "Fournisseurs": compter(cnx, "fournisseur"),
            "Utilisateurs": compter(cnx, "utilisateur"),
            "Mouvements": compter(cnx, "mouvement"),
            "Ventes": compter(cnx, "vente"),
            "Familles": compter(cnx, "famille"),
        }
        incoherences = {}
        try:
            incoherences["mouvements_produit_inconnu"] = cnx.execute(
                "SELECT COUNT(*) FROM mouvement m LEFT JOIN produit p "
                "ON m.produit_code = p.code WHERE m.produit_code IS NOT NULL AND p.code IS NULL"
            ).fetchone()[0]
        except sqlite3.Error:
            incoherences["mouvements_produit_inconnu"] = 0
        try:
            incoherences["mouvements_client_inconnu"] = cnx.execute(
                "SELECT COUNT(*) FROM mouvement m LEFT JOIN client c "
                "ON m.client_code = c.code WHERE m.client_code IS NOT NULL AND c.code IS NULL"
            ).fetchone()[0]
        except sqlite3.Error:
            incoherences["mouvements_client_inconnu"] = 0
        try:
            incoherences["produits_sans_famille"] = cnx.execute(
                "SELECT COUNT(*) FROM produit WHERE famille_code IS NULL OR famille_code = ''"
            ).fetchone()[0]
            incoherences["utilisateurs_sans_role"] = cnx.execute(
                "SELECT COUNT(*) FROM utilisateur WHERE role IS NULL OR role = ''"
            ).fetchone()[0]
        except sqlite3.Error:
            incoherences["produits_sans_famille"] = 0
            incoherences["utilisateurs_sans_role"] = 0
        return {"base": str(base.resolve()), "entites": resultats, "incoherences": incoherences}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("base", type=Path)
    parser.add_argument("--rapport", type=Path)
    args = parser.parse_args()
    resultat = valider(args.base)
    texte = json.dumps(resultat, ensure_ascii=False, indent=2)
    print(texte)
    if args.rapport:
        args.rapport.write_text(texte, encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
