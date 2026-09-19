#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
engine.py — Orchestration de la migration Atoo Leo2 → POSTEK.

Pipeline (5 étapes, cf. docs/PLAN_MIGRATION.md §5) :
  1. ANALYSE    : inventaire des exports + contrôle manifest/checksums
  2. SAUVEGARDE : backup de la base POSTEK (copie fichier SQLite)
  3. IMPORT     : transformation ligne à ligne (mapping leo2_schema),
                  transaction par table (sqlite3, stdlib)
  4. VALIDATION : comptages source/cible, intégrité référentielle,
                  détection de doublons sur clés naturelles
  5. RAPPORT    : résultat par table, anomalies, chemin du backup

Cible : base SQLite (fichier). Le moteur est volontairement enveloppable
par une UI (assistant graphique) : chaque étape renvoie un objet résultat.
Idempotent : les relances écrasent les lignes déjà migrées (clés naturelles).
"""

from __future__ import annotations

import json
import shutil
import sqlite3
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

from . import leo2_schema
from .export_reader import ExportReader

# Schéma cible minimal (tables touchées par les mappings livrés).
SCHEMA_CIBLE_SQL = """
CREATE TABLE IF NOT EXISTS client (
    code TEXT PRIMARY KEY,
    raison_sociale TEXT NOT NULL,
    adresse TEXT, code_postal TEXT, ville TEXT, telephone TEXT,
    matricule_fiscal TEXT, plafond_credit NUMERIC(14,3)
);
CREATE TABLE IF NOT EXISTS fournisseur (
    code TEXT PRIMARY KEY, nom TEXT, raison_sociale TEXT, adresse TEXT, ville TEXT,
    code_postal TEXT, telephone TEXT, email TEXT, matricule_fiscal TEXT,
    conditions_reglement TEXT, delai_paiement TEXT, solde NUMERIC(14,3), compte_comptable TEXT
);
CREATE TABLE IF NOT EXISTS client_extras (
    client_code TEXT PRIMARY KEY REFERENCES client(code),
    extras_json TEXT
);
CREATE TABLE IF NOT EXISTS famille (
    code TEXT PRIMARY KEY, libelle TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS famille_article (
    code TEXT PRIMARY KEY, libelle TEXT NOT NULL,
    ordre INTEGER NOT NULL DEFAULT 0,
    couleur TEXT NOT NULL DEFAULT '#3B82F6'
);
CREATE TABLE IF NOT EXISTS produit (
    code TEXT PRIMARY KEY,
    designation TEXT NOT NULL,
    code_barres TEXT UNIQUE,
    prix_ttc NUMERIC(14,3), prix_achat NUMERIC(14,3),
    stock NUMERIC(14,3) DEFAULT 0,
    famille_code TEXT REFERENCES famille(code),
    taux_tva_code TEXT
);
CREATE TABLE IF NOT EXISTS article (
    code TEXT PRIMARY KEY,
    designation TEXT NOT NULL,
    prix_ttc TEXT NOT NULL,
    taux_tva TEXT NOT NULL,
    famille_code TEXT REFERENCES famille_article(code),
    description TEXT
);
CREATE TABLE IF NOT EXISTS tarif (
    produit_code TEXT REFERENCES produit(code),
    grille_code TEXT, prix NUMERIC(14,3),
    PRIMARY KEY (produit_code, grille_code)
);
CREATE TABLE IF NOT EXISTS utilisateur (
    code TEXT PRIMARY KEY, nom TEXT NOT NULL, prenom TEXT, role TEXT, groupe_code TEXT,
    droits_json TEXT, mot_de_passe_hash TEXT,
    mot_de_passe_a_reinitialiser INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS mouvement (
    numero_piece TEXT NOT NULL, date_mouvement TEXT, type_mouvement TEXT,
    client_code TEXT, fournisseur_code TEXT, produit_code TEXT,
    quantite NUMERIC(14,3), prix_unitaire NUMERIC(14,3), total_ht NUMERIC(14,3),
    tva NUMERIC(14,3), total_ttc NUMERIC(14,3), mode_reglement TEXT, reference_paiement TEXT,
    PRIMARY KEY(numero_piece, produit_code)
);
CREATE TABLE IF NOT EXISTS vente (
    numero_ticket TEXT PRIMARY KEY, date_vente TEXT, caissier_code TEXT, client_code TEXT,
    total_ht NUMERIC(14,3), total_tva NUMERIC(14,3), total_ttc NUMERIC(14,3), mode_reglement TEXT
);
CREATE TABLE IF NOT EXISTS facture (
    numero TEXT PRIMARY KEY,
    date_facture TEXT NOT NULL,
    client_code TEXT REFERENCES client(code),
    total_ht NUMERIC(14,3), total_tva NUMERIC(14,3),
    total_ttc NUMERIC(14,3), net_a_payer NUMERIC(14,3)
);
"""


@dataclass
class ResultatTable:
    table: str
    lignes_source: int = 0
    lignes_importees: int = 0
    erreurs: list[str] = field(default_factory=list)


@dataclass
class ResultatMigration:
    tables: list[ResultatTable] = field(default_factory=list)
    anomalies: list[str] = field(default_factory=list)
    avertissements: list[str] = field(default_factory=list)
    backup: str = ""
    ok: bool = True

    def resume(self) -> str:
        lignes = f"{sum(t.lignes_importees for t in self.tables)} lignes importées"
        errs = f", {sum(len(t.erreurs) for t in self.tables)} erreurs"
        return f"{'OK' if self.ok else 'ÉCHEC'} — {lignes}{errs}, backup: {self.backup or 'aucun'}"


class MoteurMigration:
    def __init__(self, dossier_exports: str | Path, base_sqlite: str | Path):
        self.reader = ExportReader(dossier_exports)
        self.base = Path(base_sqlite)
        self.rapport: ResultatMigration = ResultatMigration()
        # Avertissements : non bloquants, signalés dans le rapport.
        self.avertissements: list[str] = []

    # ------------------------------------------------------------------
    # 1. ANALYSE
    # ------------------------------------------------------------------
    def analyser(self) -> ResultatAnalyse:
        return self.reader.analyser()

    # ------------------------------------------------------------------
    # 2. SAUVEGARDE
    # ------------------------------------------------------------------
    def _sauvegarder(self) -> str:
        if not self.base.is_file():
            return ""  # base neuve : rien à sauvegarder
        horodatage = datetime.now().strftime("%Y%m%d_%H%M%S")
        dest = self.base.with_name(f"{self.base.stem}_backup_{horodatage}.db")
        shutil.copy2(self.base, dest)
        return str(dest)

    @staticmethod
    def detecter_dossier_leo2(chemin_postek: str | Path) -> Path | None:
        """Recherche un dossier source local sans accès réseau."""
        racine = Path(chemin_postek)
        candidats = [racine]
        if racine.is_dir():
            candidats.extend(dossier for dossier in racine.rglob("*") if dossier.is_dir())
        for dossier in candidats:
            if not dossier.is_dir():
                continue
            fichiers = [f for f in dossier.iterdir() if f.is_file()]
            if any(f.suffix.lower() in (".fic", ".xls", ".csv", ".xdd") for f in fichiers):
                return dossier
        return None

    def lister_exports_disponibles(self, chemin_leo2: str | Path | None = None) -> list[Path]:
        return ExportReader(chemin_leo2 or self.reader.dossier).lister_exports_disponibles()

    def importer_toutes_entites(self, chemin_leo2: str | Path | None = None) -> ResultatMigration:
        if chemin_leo2 is not None:
            self.reader = ExportReader(chemin_leo2)
        return self.executer()

    def _importer_entite(self, table: str, chemin_export: str | Path) -> ResultatMigration:
        chemin = Path(chemin_export)
        self.reader = ExportReader(chemin if chemin.is_dir() else chemin.parent)
        return self.executer([table])

    def importer_produits(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("PRODUITS", chemin_export)

    def importer_clients(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("CLIENTS", chemin_export)

    def importer_fournisseurs(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("FOURNISSEURS", chemin_export)

    def importer_utilisateurs(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("UTILISATEURS", chemin_export)

    def importer_mouvements(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("MOUVEMENTS", chemin_export)

    def importer_familles(self, chemin_export: str | Path) -> ResultatMigration:
        return self._importer_entite("FAMILLE", chemin_export)

    # ------------------------------------------------------------------
    # Conversions de champs
    # ------------------------------------------------------------------
    @staticmethod
    def _convertir(valeur: str | None, transform: str):
        if valeur is None:
            return None
        v = valeur.strip() if transform in ("trim", "matricule_fiscal") else valeur
        if transform == "trim":
            return v or None
        if transform == "majuscule":
            return v.upper() or None
        if transform == "matricule_fiscal":
            # Format tunisien type 1234567/A/M/000 — validation souple à
            # l'import : on normalise, l'anomalie est signalée en validation.
            v = v.replace(" ", "").upper()
            return v or None
        if transform == "montant":
            v = v.replace(" ", "").replace(",", ".")
            try:
                return round(float(v), 3) if v else None
            except ValueError:
                return None
        return valeur

    def _lignes_transformees(self, table_source: str):
        mapping = leo2_schema.mapping_pour(table_source)
        for index, brut in enumerate(self.reader.lire(table_source), start=1):
            brut = {str(k).upper().replace(" ", ""): v for k, v in brut.items()}
            if table_source.upper() == "PRODUITS" and "LIBELLE" in brut:
                brut = {
                    "REF": f"P{index:03d}",
                    "DESIGNATION": brut.get("LIBELLE", ""),
                    "CODBAR": brut.get("CBARRE", ""),
                    "PVTTTC": brut.get("PRIX", ""),
                    "PRIXACHAT": "0.000",
                    "STOCK": "0",
                    "FAMILLE": brut.get("FAMILLE", ""),
                    "CODETVA": brut.get("TVA", ""),
                }
            alias = {
                "CODE": "REF", "NOM": "DESIGNATION", "CODBAR": "CODEBARRE",
                "TEL": "TELEPHONE", "QUANTITE": "QTE",
            }
            ligne: dict = {}
            for conv in mapping.champs:
                valeur = brut.get(conv.source.upper())
                if valeur is None and alias.get(conv.source.upper()):
                    valeur = brut.get(alias[conv.source.upper()])
                ligne[conv.cible] = self._convertir(valeur, conv.transform)
            yield ligne

    # ------------------------------------------------------------------
    # 3. IMPORT
    # ------------------------------------------------------------------
    def _creer_schema(self, cnx: sqlite3.Connection) -> None:
        cnx.executescript(SCHEMA_CIBLE_SQL)

    @staticmethod
    def _taux_tva_article(code: str | None) -> str:
        taux_par_code = {"1": "0.190", "2": "0.130", "3": "0.070", "4": "0.000"}
        valeur = (code or "").strip()
        if valeur.endswith(".0"):
            valeur = valeur[:-2]
        if valeur in taux_par_code:
            return taux_par_code[valeur]
        try:
            taux = float(valeur.replace(",", "."))
            if taux > 1:
                taux /= 100
            return f"{taux:.3f}"
        except ValueError:
            return "0.190"

    def _synchroniser_catalogue(self, cnx: sqlite3.Connection) -> None:
        """Publie le catalogue migré dans les tables lues par la caisse WPF."""
        cnx.execute(
            "INSERT OR REPLACE INTO famille_article(code, libelle) "
            "SELECT code, libelle FROM famille"
        )
        cnx.execute(
            """
            INSERT OR REPLACE INTO article(
                code, designation, prix_ttc, taux_tva, famille_code, description
            )
            SELECT p.code, p.designation,
                   printf('%.3f', COALESCE(p.prix_ttc, 0)),
                   ?, p.famille_code, NULL
            FROM produit p
            """,
            ("0.190",),
        )
        for code, taux in cnx.execute(
            "SELECT code, taux_tva_code FROM produit"
        ).fetchall():
            cnx.execute(
                "UPDATE article SET taux_tva = ? WHERE code = ?",
                (self._taux_tva_article(taux), code),
            )

    def _importer_table(self, cnx: sqlite3.Connection, nom_source: str) -> ResultatTable:
        mapping = leo2_schema.mapping_pour(nom_source)
        res = ResultatTable(table=mapping.cible)
        try:
            res.lignes_source = self.reader.compter(nom_source)
        except FileNotFoundError:
            res.erreurs.append("fichier d'export introuvable")
            return res

        # Ordre INSERT selon la table (upsert sur la clé naturelle cible).
        colonnes = [c.cible for c in mapping.champs]
        cibles = ", ".join(colonnes)
        espaces = ", ".join("?" for _ in colonnes)
        cles = [c.lower() for c in mapping.cles_cibles]
        conflits = ", ".join(cles)
        sql = (
            f"INSERT INTO {mapping.cible} ({cibles}) VALUES ({espaces}) "
            f"ON CONFLICT({conflits}) DO UPDATE SET "
            + ", ".join(f"{c}=excluded.{c}" for c in colonnes if c.lower() not in cles)
        )

        try:
            if nom_source.upper() == "PRODUITS":
                for brut in self.reader.lire(nom_source):
                    brut = {str(k).upper().replace(" ", ""): v for k, v in brut.items()}
                    famille = (brut.get("FAMILLE") or "").strip()
                    if famille:
                        cnx.execute(
                            "INSERT OR IGNORE INTO famille(code, libelle) VALUES(?, ?)",
                            (famille, f"Famille {famille}"),
                        )
            for numero_ligne, ligne in enumerate(
                self._lignes_transformees(nom_source), start=1
            ):
                try:
                    if mapping.cible == "mouvement" and ligne.get("produit_code"):
                        existe = cnx.execute(
                            "SELECT 1 FROM produit WHERE code = ?", (ligne["produit_code"],)
                        ).fetchone()
                        if existe is None:
                            res.erreurs.append(
                                f"produit inexistant : {ligne['produit_code']}"
                            )
                            continue
                    if mapping.cible == "produit" and ligne.get("code_barres"):
                        doublon = cnx.execute(
                            "SELECT code FROM produit "
                            "WHERE code_barres = ? AND code <> ? LIMIT 1",
                            (ligne["code_barres"], ligne.get("code")),
                        ).fetchone()
                        if doublon is not None:
                            self.avertissements.append(
                                f"produit ligne {numero_ligne}: code-barres "
                                f"{ligne['code_barres']} deja utilise par {doublon[0]}; "
                                "code-barres ignore pour ce produit"
                            )
                            ligne["code_barres"] = None
                    cnx.execute(sql, [ligne.get(c) for c in colonnes])
                    res.lignes_importees += 1
                except sqlite3.Error as exc:
                    res.erreurs.append(f"ligne {numero_ligne}: {exc}")
        except FileNotFoundError as exc:
            res.erreurs.append(str(exc))
        return res

    # ------------------------------------------------------------------
    # 4. VALIDATION
    # ------------------------------------------------------------------
    def _valider(self, cnx: sqlite3.Connection) -> None:
        rap = self.rapport
        for res in rap.tables:
            # Comptage cible réel (upserts compris).
            (n,) = cnx.execute(
                f"SELECT COUNT(*) FROM {res.table}"
            ).fetchone()
            if n < res.lignes_importees:
                rap.anomalies.append(
                    f"{res.table}: comptage cible ({n}) < importés ({res.lignes_importees})"
                )

        # Intégrité référentielle minimale.
        for fk_sql in (
            ("produit", "famille_code", "famille", "code"),
            ("tarif", "produit_code", "produit", "code"),
            ("facture", "client_code", "client", "code"),
            ("client_extras", "client_code", "client", "code"),
        ):
            table, col, table_ref, col_ref = fk_sql
            (orphelins,) = cnx.execute(
                f"SELECT COUNT(*) FROM {table} t LEFT JOIN {table_ref} r "
                f"ON t.{col} = r.{col_ref} WHERE t.{col} IS NOT NULL AND r.{col_ref} IS NULL"
            ).fetchone()
            if orphelins:
                rap.anomalies.append(
                    f"{table}.{col} : {orphelins} référence(s) orpheline(s)"
                )

        # Matricules fiscaux manquants : avertissement (la migration reste
        # un succès ; la conformité NACEF exige de compléter ces fiches).
        (mf_vides,) = cnx.execute(
            "SELECT COUNT(*) FROM client WHERE matricule_fiscal IS NULL OR matricule_fiscal = ''"
        ).fetchone()
        if mf_vides:
            self.avertissements.append(
                f"{mf_vides} client(s) sans matricule fiscal (à compléter)"
            )

    # ------------------------------------------------------------------
    # Orchestration publique
    # ------------------------------------------------------------------
    def executer(self, tables: list[str] | None = None) -> ResultatMigration:
        """Exécute la migration complète. `tables` = sous-ensemble source optionnel."""
        # 1. Analyse
        analyse = self.analyser()
        rap = self.rapport = ResultatMigration()
        rap.avertissements = self.avertissements = []
        if not analyse.tables:
            rap.ok = False
            rap.anomalies.append("aucun export trouvé (voir docs/PLAN_MIGRATION.md §2)")
            return rap
        rap.anomalies.extend(analyse.anomalies)

        # Ordre des dépendances, filtré si sous-ensemble demandé.
        noms = [s.upper() for s in (tables or analyse.tables.keys())]
        ordre = [t for t in leo2_schema.tables_migrables() if t in noms]

        # 2. Sauvegarde
        rap.backup = self._sauvegarder()

        # 3. Import + 4. Validation en une connexion (transaction globale)
        self.base.parent.mkdir(parents=True, exist_ok=True)
        cnx = sqlite3.connect(str(self.base))
        try:
            self._creer_schema(cnx)
            for nom in ordre:
                mapping = leo2_schema.mapping_pour(nom)
                if mapping is None:
                    rap.anomalies.append(f"table inconnue au mapping : {nom}")
                    continue
                res = self._importer_table(cnx, nom)
                rap.tables.append(res)
                rap.anomalies.extend(f"{res.table}: {erreur}" for erreur in res.erreurs)
            self._synchroniser_catalogue(cnx)
            self._valider(cnx)
            # Import conservé même en cas d'anomalies (rapport détaillé) ;
            # rollback uniquement sur erreur fatale (exception).
            cnx.commit()
            rap.ok = not rap.anomalies
        except Exception:
            cnx.rollback()
            cnx.close()
            if rap.backup:
                shutil.copy2(rap.backup, self.base)
            elif self.base.is_file():
                self.base.unlink()
            rap.ok = False
            rap.anomalies.append("erreur fatale : transaction annulée (rollback)")
            raise
        finally:
            cnx.close()

        # 5. Rapport (avertissements remontés par la validation)
        rap.avertissements = list(self.avertissements)
        return rap


def ecrire_rapport(rap: ResultatMigration, chemin: str | Path) -> None:
    """Écrit le rapport de migration (JSON) pour l'assistant graphique."""
    Path(chemin).write_text(
        json.dumps(
            {
                "ok": rap.ok,
                "resume": rap.resume(),
                "backup": rap.backup,
                "anomalies": rap.anomalies,
                "avertissements": rap.avertissements,
                "tables": [
                    {"table": t.table, "source": t.lignes_source,
                     "importees": t.lignes_importees, "erreurs": t.erreurs}
                    for t in rap.tables
                ],
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
