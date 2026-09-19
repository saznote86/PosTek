#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
export_reader.py — Lecture d'un répertoire d'exports Atoo Leo2.

Format attendu (cf. docs/PLAN_MIGRATION.md §2) :

  export_leo2/
  ├── manifest.json      # {"date_export": "...", "tables": {"CLIENTS": {"fichier": "CLIENTS.csv", "lignes": N, "sha256": "..."}, ...}}
  └── tables/*.csv       # UTF-8, séparateur ';', point décimal

Fournit :
  - ExportReader.analyser()      -> inventaire + contrôles (fichiers, checksums)
  - ExportReader.lire(table)     -> itérateur de dicts (toutes colonnes str)
  - ExportReader.compter(table)  -> nombre de lignes (via manifest si présent)

Le manifest est optionnel : si absent, l'analyse s'appuie sur les seuls
CSV présents (mode tolérant pour les exports manuels).
"""

from __future__ import annotations

import csv
import codecs
import hashlib
import json
from dataclasses import dataclass, field
from pathlib import Path


def detecter_encodage(chemin_fichier: str | Path) -> str:
    """Détecte les encodages usuels des exports bureautiques locaux."""
    donnees = Path(chemin_fichier).read_bytes()[:65536]
    if donnees.startswith(codecs.BOM_UTF8):
        return "utf-8-sig"
    for encodage in ("utf-8", "cp1252", "latin-1"):
        try:
            donnees.decode(encodage)
            return encodage
        except UnicodeDecodeError:
            continue
    return "latin-1"


def lire_csv(chemin_fichier: str | Path) -> list[dict[str, str]]:
    chemin = Path(chemin_fichier)
    texte = chemin.read_text(encoding=detecter_encodage(chemin))
    lignes = list(csv.reader(texte.splitlines()))
    if not lignes:
        return []
    delimiteur = ";" if lignes[0].count(";") >= lignes[0].count(",") else ","
    lecteur = csv.DictReader(texte.splitlines(), delimiter=delimiteur)
    return [{str(k).strip(): (v or "").strip() for k, v in ligne.items() if k is not None}
            for ligne in lecteur]


def lire_xls(chemin_fichier: str | Path) -> list[dict[str, str]]:
    """Lit XLS binaire via xlrd, ou XLSX via openpyxl si disponible."""
    chemin = Path(chemin_fichier)
    if chemin.suffix.lower() == ".xlsx":
        try:
            from openpyxl import load_workbook
        except ImportError as exc:
            raise RuntimeError("La lecture XLSX nécessite openpyxl.") from exc
        lignes = list(load_workbook(chemin, read_only=True, data_only=True).active.values)
    else:
        try:
            import xlrd
        except ImportError as exc:
            raise RuntimeError("La lecture XLS nécessite xlrd.") from exc
        feuille = xlrd.open_workbook(str(chemin), on_demand=True).sheet_by_index(0)
        lignes = [[feuille.cell_value(i, j) for j in range(feuille.ncols)]
                  for i in range(feuille.nrows)]
    if not lignes:
        return []
    entetes = [str(v or "").strip() for v in lignes[0]]
    return [{entetes[i]: "" if i >= len(ligne) or ligne[i] is None else str(ligne[i]).strip()
             for i in range(len(entetes)) if entetes[i]}
            for ligne in lignes[1:]]


def normaliser_colonnes(lignes: list[dict[str, str]]) -> list[dict[str, str]]:
    aliases = {
        "CODEBARRE": "CODBAR", "CODE_BARRE": "CODBAR", "BARCODE": "CODBAR",
        "TELEPHONE": "TEL", "PRIXVENTE": "PRIX_VENTE", "PRIXACHAT": "PRIX_ACHAT",
    }
    return [{aliases.get(str(k).strip().upper(), str(k).strip().upper()): v
             for k, v in ligne.items()} for ligne in lignes]


def _sha256(fichier: Path) -> str:
    h = hashlib.sha256()
    with fichier.open("rb") as f:
        for bloc in iter(lambda: f.read(65536), b""):
            h.update(bloc)
    return h.hexdigest()


@dataclass
class ResultatAnalyse:
    dossier: Path
    tables: dict[str, Path] = field(default_factory=dict)   # nom -> CSV
    lignes: dict[str, int | None] = field(default_factory=dict)
    anomalies: list[str] = field(default_factory=list)

    @property
    def ok(self) -> bool:
        return bool(self.tables) and not self.anomalies


class ExportReader:
    def __init__(self, dossier: str | Path):
        self.dossier = Path(dossier)
        self.manifest: dict = {}
        # Comptages déclarés par le manifest, indexés en majuscules.
        self.lignes: dict[str, int | None] = {}
        chemin_manifest = self.dossier / "manifest.json"
        if chemin_manifest.is_file():
            self.manifest = json.loads(
                chemin_manifest.read_text(encoding="utf-8")
            )
            for nom, infos in self.manifest.get("tables", {}).items():
                self.lignes[nom.upper()] = infos.get("lignes")

    # ------------------------------------------------------------------
    def _dossier_tables(self) -> Path:
        d = self.dossier / "tables"
        return d if d.is_dir() else self.dossier

    def _fichier_table(self, nom: str) -> Path | None:
        base = self._dossier_tables()
        # Candidats usuels d'abord (rapide), puis balayage insensible à la
        # casse : un export réel peut s'appeler « Produits.csv » et un
        # filesystem Linux distingue PRODUITS.csv de Produits.csv — la
        # résolution ne doit pas dépendre de la sémantique de casse de l'OS.
        candidats = [base / f"{nom}{extension}"
                     for extension in (".csv", ".CSV", ".xls", ".XLS", ".xlsx", ".XLSX")]
        candidats += [base / f"{nom.upper()}{extension}"
                      for extension in (".csv", ".CSV", ".xls", ".XLS", ".xlsx", ".XLSX")]
        candidats += [base / f"{nom.lower()}{extension}"
                      for extension in (".csv", ".CSV", ".xls", ".XLS", ".xlsx", ".XLSX")]
        for c in candidats:
            if c.is_file():
                return c
        cible = nom.upper()
        for export in self.lister_exports_disponibles():
            if export.stem.upper() == cible:
                return export
        return None

    def lister_exports_disponibles(self) -> list[Path]:
        base = self._dossier_tables()
        return sorted([f for f in base.iterdir() if f.is_file()
                       and f.suffix.lower() in (".csv", ".xls", ".xlsx")])

    # ------------------------------------------------------------------
    def analyser(self) -> ResultatAnalyse:
        """Inventorie les tables exportées et vérifie les checksums du manifest."""
        res = ResultatAnalyse(dossier=self.dossier)
        base = self._dossier_tables()
        if not base.is_dir():
            res.anomalies.append(f"dossier introuvable : {base}")
            return res

        for export in self.lister_exports_disponibles():
            nom = export.stem.upper()
            res.tables[nom] = export

        # Contrôles manifest (checksums + comptages déclarés).
        tables_manifest = self.manifest.get("tables", {})
        for nom, infos in tables_manifest.items():
            nom_u = nom.upper()
            if nom_u not in res.tables:
                res.anomalies.append(f"manifest: table déclarée absente : {nom}")
                continue
            csv_f = res.tables[nom_u]
            sha_attendu = infos.get("sha256")
            if sha_attendu and _sha256(csv_f) != sha_attendu:
                res.anomalies.append(f"checksum invalide : {nom_u}")
            res.lignes[nom_u] = infos.get("lignes")

        for nom, csv_f in res.tables.items():
            if nom not in res.lignes:
                res.lignes[nom] = None  # comptage différé à la lecture

        return res

    # ------------------------------------------------------------------
    def lire(self, table: str):
        """Itère sur les lignes d'une table (dicts, valeurs str brutes)."""
        csv_f = self._fichier_table(table)
        if csv_f is None:
            raise FileNotFoundError(f"table exportée introuvable : {table}")
        lignes = lire_xls(csv_f) if csv_f.suffix.lower() in (".xls", ".xlsx") else lire_csv(csv_f)
        for ligne in lignes:
            yield ligne

    def compter(self, table: str) -> int:
        """Compte les lignes (rapide : manifest d'abord, sinon lecture)."""
        nom = table.upper()
        if nom in self.lignes and self.lignes[nom] is not None:
            return int(self.lignes[nom])
        csv_f = self._fichier_table(table)
        if csv_f is None:
            return 0
        if csv_f.suffix.lower() in (".xls", ".xlsx"):
            return len(lire_xls(csv_f))
        with csv_f.open("r", encoding=detecter_encodage(csv_f), newline="") as f:
            return sum(1 for _ in f) - 1
