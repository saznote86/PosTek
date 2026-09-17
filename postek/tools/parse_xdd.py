#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
parse_xdd.py — Extracteur de schéma pour l'analyse WinDev (fichier .xdd).

Lit l'analyse XML de l'application Atoo Leo2 (MINICASH.xdd, encodage ISO-8859-1)
et produit :
  1. un schéma machine-lisible (JSON)  -> utilisé par le moteur de migration POSTEK
  2. un dictionnaire de données lisible (Markdown) -> documentation / mapping

Ce script ne lit PAS les données : il documente la STRUCTURE (tables, rubriques,
types, clés). Les données des .FIC sont chiffrées et ne sont pas accessibles
directement (voir docs/PLAN_MIGRATION.md pour la stratégie d'extraction).

Usage:
    python tools/parse_xdd.py <fichier.xdd> <sortie.json> <sortie.md>
"""

from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

# --------------------------------------------------------------------------
# Types HFSQL (codes du format .WDD/.XDD "VersionStructure=1").
# Seuls les codes observés et vérifiés sur l'analyse Atoo Leo2 sont affirmés :
#   38 -> entier automatique (clé BACLEUNIK), 2 -> texte (avec TAILLE).
# Les autres codes sont affichés bruts ("code N") le temps de la phase 2,
# où chaque type sera confirmé par des lectures réelles (voir PLAN_MIGRATION).
# --------------------------------------------------------------------------
TYPE_LABELS_VERIFIES = {
    38: "entier auto (clé)",
    2: "texte",
}

TYPE_CLE_LABELS = {
    0: "",
    1: "clé unique",
    2: "clé avec doublons",
}


def _text(rubrique: ET.Element, tag: str, default: str = "") -> str:
    node = rubrique.find(tag)
    if node is None or node.text is None:
        return default
    return node.text.strip()


def parse_xdd(path_xdd: Path) -> dict:
    """Parse l'analyse .xdd et renvoie un dictionnaire de schéma normalisé."""
    # L'en-tête XML déclare ISO-8859-1 : ElementTree le respecte.
    tree = ET.parse(str(path_xdd))
    root = tree.getroot()

    analyse = {
        "source": str(path_xdd.name),
        "gen_num": root.get("GenNum", ""),
        "version_structure": root.get("VersionStructure", ""),
    }

    tables = []
    for fichier in root.iter("FICHIER"):
        table = {
            "nom": fichier.get("Nom", ""),
            "abreviation": fichier.get("Abreviation", ""),
            "nom_physique": fichier.get("NomPhysique", ""),
            "base": fichier.get("NomLogiqueDataBase", ""),
            "rubriques": [],
        }
        for rub in fichier.findall("RUBRIQUE"):
            taille_txt = _text(rub, "TAILLE")
            type_txt = _text(rub, "TYPE")
            code_type = int(type_txt) if type_txt.isdigit() else None
            type_cle_txt = _text(rub, "TYPE_CLE")
            code_cle = int(type_cle_txt) if type_cle_txt.isdigit() else 0
            indice = rub.find("INDICERUBRIQUE")
            table["rubriques"].append(
                {
                    "nom": rub.get("Nom", ""),
                    "code_type": code_type,
                    "type": TYPE_LABELS_VERIFIES.get(code_type, f"code {code_type}"),
                    "taille": int(taille_txt) if taille_txt.isdigit() else None,
                    "cle": TYPE_CLE_LABELS.get(code_cle, f"code {code_cle}"),
                    "indice": indice.get("Indice", "0") if indice is not None else "0",
                }
            )
        tables.append(table)

    tables.sort(key=lambda t: t["nom"].lower())
    return {"analyse": analyse, "tables": tables}


def write_json(schema: dict, out_json: Path) -> None:
    out_json.parent.mkdir(parents=True, exist_ok=True)
    out_json.write_text(
        json.dumps(schema, ensure_ascii=False, indent=2), encoding="utf-8"
    )


def write_markdown(schema: dict, out_md: Path) -> None:
    """Génère le dictionnaire de données lisible, groupé par table."""
    lines: list[str] = []
    lines.append("# Dictionnaire de données — Atoo Leo2 (analyse MINICASH.xdd)")
    lines.append("")
    lines.append(
        f"> Généré automatiquement par `tools/parse_xdd.py` — "
        f"{len(schema['tables'])} tables, "
        f"{sum(len(t['rubriques']) for t in schema['tables'])} rubriques. "
        f"GenNum={schema['analyse']['gen_num']}."
    )
    lines.append("")
    lines.append(
        "> ⚠️ Les codes de types non vérifiés sont affichés bruts (`code N`) ; "
        "le décodage complet sera confirmé en phase 2 par lectures réelles."
    )
    lines.append("")

    for table in schema["tables"]:
        lines.append(f"## {table['nom']}")
        lines.append("")
        meta = []
        if table["abreviation"]:
            meta.append(f"abr. `{table['abreviation']}`")
        if table["nom_physique"]:
            meta.append(f"physique : `{table['nom_physique']}`")
        if meta:
            lines.append("(" + ", ".join(meta) + ")")
            lines.append("")
        if not table["rubriques"]:
            lines.append("_(aucune rubrique déclarée)_")
            lines.append("")
            continue
        lines.append("| Rubrique | Type | Taille | Clé |")
        lines.append("|---|---|---|---|")
        for r in table["rubriques"]:
            taille = str(r["taille"]) if r["taille"] is not None else ""
            cle = r["cle"]
            lines.append(f"| `{r['nom']}` | {r['type']} | {taille} | {cle} |")
        lines.append("")

    out_md.parent.mkdir(parents=True, exist_ok=True)
    out_md.write_text("\n".join(lines), encoding="utf-8")


def main(argv: list[str]) -> int:
    if len(argv) != 4:
        print(__doc__)
        return 2
    src, out_json, out_md = (Path(a) for a in argv[1:])
    if not src.is_file():
        print(f"[ERREUR] Fichier introuvable : {src}")
        return 1

    schema = parse_xdd(src)
    write_json(schema, out_json)
    write_markdown(schema, out_md)

    n_tables = len(schema["tables"])
    n_rub = sum(len(t["rubriques"]) for t in schema["tables"])
    print(f"[OK] {n_tables} tables / {n_rub} rubriques extraites.")
    print(f"     JSON : {out_json}")
    print(f"     MD   : {out_md}")

    # Aperçu des 15 plus grosses tables (utile pour prioriser la migration).
    print("\nTables les plus riches (par nombre de rubriques) :")
    for t in sorted(schema["tables"], key=lambda x: -len(x["rubriques"]))[:15]:
        print(f"  {t['nom']:<22} {len(t['rubriques']):>3} rubriques")
    types = Counter(
        r["type"] for t in schema["tables"] for r in t["rubriques"]
    )
    print("\nRépartition des types :", dict(types.most_common()))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
