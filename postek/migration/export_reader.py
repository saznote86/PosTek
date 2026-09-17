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
import hashlib
import json
from dataclasses import dataclass, field
from pathlib import Path


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
        for c in (base / f"{nom}.csv", base / f"{nom.upper()}.csv",
                  base / f"{nom.lower()}.csv"):
            if c.is_file():
                return c
        cible = nom.upper()
        for csv_f in sorted(base.glob("*.csv")):
            if csv_f.stem.upper() == cible:
                return csv_f
        return None

    # ------------------------------------------------------------------
    def analyser(self) -> ResultatAnalyse:
        """Inventorie les tables exportées et vérifie les checksums du manifest."""
        res = ResultatAnalyse(dossier=self.dossier)
        base = self._dossier_tables()
        if not base.is_dir():
            res.anomalies.append(f"dossier introuvable : {base}")
            return res

        for csv_f in sorted(base.glob("*.csv")):
            nom = csv_f.stem.upper()
            res.tables[nom] = csv_f

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
        with csv_f.open("r", encoding="utf-8-sig", newline="") as f:
            lecteur = csv.DictReader(f, delimiter=";")
            for ligne in lecteur:
                yield {k: v for k, v in ligne.items() if k is not None}

    def compter(self, table: str) -> int:
        """Compte les lignes (rapide : manifest d'abord, sinon lecture)."""
        nom = table.upper()
        if nom in self.lignes and self.lignes[nom] is not None:
            return int(self.lignes[nom])
        csv_f = self._fichier_table(table)
        if csv_f is None:
            return 0
        with csv_f.open("r", encoding="utf-8-sig", newline="") as f:
            return sum(1 for _ in f) - 1  # moins l'en-tête
