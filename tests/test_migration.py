#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tests/test_migration.py — Tests du moteur de migration Leo2 → POSTEK.

Scénario : un répertoire d'exports CSV (clients, produits, familles,
factures, TVA) est créé en mémoire temporaire, puis migré vers une base
SQLite. Vérifie : comptages, upsert idempotent, mapping des champs,
conversions de montants, intégrité référentielle, rapport JSON, backup.
"""

from __future__ import annotations

import json
import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from migration.assistant import AssistantMigration
from migration.engine import MoteurMigration, ecrire_rapport
from migration.export_reader import ExportReader, detecter_encodage

# Données d'export simulées (noms de colonnes = noms Leo2).
EXPORTS = {
    "CLIENTS.csv": [
        ["CODE", "NOM", "ADRESSE", "CODEPOSTAL", "VILLE", "TEL", "MATRICULE", "PLAFOND"],
        ["C001", "  Société Alpha ", "12 rue de la République", "1000", "Tunis", "71123456", "1234567/A/M/000", "5000,000"],
        ["C002", "Beta SARL", "avenue Habib Bourguiba", "4000", "Sousse", "73234567", "", "1200,500"],
    ],
    "famille.csv": [
        ["CODE", "LIBELLE"],
        ["F01", "BOULANGERIE"],
        ["F02", "PÂTISSERIE"],
    ],
    "Produits.csv": [
        ["REF", "DESIGNATION", "CODBAR", "PVTTTC", "PRIXACHAT", "STOCK", "FAMILLE", "CODETVA"],
        ["P001", "Baguette tradition", "6191234500017", "0,250", "0,180", "120", "F01", "7"],
        ["P002", "Croissant", "6191234500024", "0,600", "0,400", "80", "F02", "13"],
    ],
    "Facture.csv": [
        ["NUMERO", "DATE", "CODECLI", "TOTALHT", "TOTALTVA", "TOTALTTC", "NETAPAYER"],
        ["F2026-000001", "2026-09-01", "C001", "100,000", "19,000", "119,000", "119,000"],
        ["F2026-000002", "2026-09-02", "C002", "50,000", "6,500", "56,500", "56,500"],
    ],
}


def creer_exports(dossier: Path) -> None:
    tables = dossier / "tables"
    tables.mkdir(parents=True)
    manifest = {"date_export": "2026-09-17", "tables": {}}
    for nom, contenu in EXPORTS.items():
        import hashlib
        # Écriture en BYTES : le checksum doit porter sur les octets réels
        # du fichier (le mode texte Windows transformerait \n en \r\n).
        donnees = ("\n".join(";".join(l) for l in contenu) + "\n").encode("utf-8")
        (tables / nom).write_bytes(donnees)
        manifest["tables"][Path(nom).stem] = {
            "fichier": nom,
            "lignes": len(contenu) - 1,
            "sha256": hashlib.sha256(donnees).hexdigest(),
        }
    (dossier / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False), encoding="utf-8"
    )


class TestMigration(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        racine = Path(self._tmp.name)
        self.exports = racine / "export_leo2"
        self.base = racine / "postek.db"
        creer_exports(self.exports)

    def tearDown(self):
        self._tmp.cleanup()

    def migrer(self, tables=None):
        moteur = MoteurMigration(self.exports, self.base)
        return moteur.executer(tables)

    # ------------------------------------------------------------------
    def test_analyse_exports(self):
        moteur = MoteurMigration(self.exports, self.base)
        analyse = moteur.analyser()
        self.assertTrue(analyse.ok, analyse.anomalies)
        self.assertEqual(
            {t.upper() for t in analyse.tables},
            {"CLIENTS", "FAMILLE", "PRODUITS", "FACTURE"},
        )
        self.assertEqual(analyse.lignes["CLIENTS"], 2)

    def test_migration_complete(self):
        rap = self.migrer()
        self.assertTrue(rap.ok, rap.anomalies)
        par_table = {t.table.upper(): t for t in rap.tables}
        self.assertEqual(par_table["CLIENT"].lignes_importees, 2)
        self.assertEqual(par_table["PRODUIT"].lignes_importees, 2)
        self.assertEqual(par_table["FACTURE"].lignes_importees, 2)

        cnx = sqlite3.connect(str(self.base))
        try:
            # Mapping + trim + montant avec virgule décimale.
            row = cnx.execute(
                "SELECT raison_sociale, matricule_fiscal, plafond_credit "
                "FROM client WHERE code='C001'"
            ).fetchone()
            self.assertEqual(row[0], "Société Alpha")
            self.assertEqual(row[1], "1234567/A/M/000")
            self.assertAlmostEqual(row[2], 5000.0, places=3)

            # Produit lié à sa famille (intégrité référentielle).
            (n,) = cnx.execute(
                "SELECT COUNT(*) FROM produit p JOIN famille f "
                "ON p.famille_code=f.code"
            ).fetchone()
            self.assertEqual(n, 2)
        finally:
            cnx.close()

    def test_idempotence(self):
        self.migrer()
        rap2 = self.migrer()
        self.assertTrue(rap2.ok, rap2.anomalies)
        cnx = sqlite3.connect(str(self.base))
        try:
            (n,) = cnx.execute("SELECT COUNT(*) FROM client").fetchone()
            self.assertEqual(n, 2)  # pas de doublon après relance
        finally:
            cnx.close()

    def test_doublon_code_barres_devient_avertissement(self):
        produits = self.exports / "tables" / "Produits.csv"
        produits.write_text(
            "REF;DESIGNATION;CODBAR;PVTTTC;PRIXACHAT;STOCK;FAMILLE;CODETVA\n"
            "P001;Produit 1;6191234500017;1,000;0,500;1;F01;1\n"
            "P002;Produit 2;6191234500017;2,000;1,000;1;F01;1\n",
            encoding="utf-8",
        )
        (self.exports / "manifest.json").unlink()

        rap = self.migrer(["PRODUITS"])

        self.assertTrue(rap.ok, rap.anomalies)
        resultat = rap.tables[0]
        self.assertEqual(resultat.lignes_importees, 2)
        self.assertFalse(resultat.erreurs)
        self.assertTrue(any("code-barres" in message for message in rap.avertissements))

        cnx = sqlite3.connect(str(self.base))
        try:
            codes = cnx.execute(
                "SELECT code_barres FROM produit ORDER BY code"
            ).fetchall()
            self.assertEqual(codes, [("6191234500017",), (None,)])
        finally:
            cnx.close()

    def test_backup_cree(self):
        self.migrer()
        rap2 = self.migrer()
        self.assertTrue(rap2.backup, "backup attendu à la 2e passe")
        self.assertTrue(Path(rap2.backup).is_file())

    def test_rapport_json(self):
        rap = self.migrer()
        chemin = self.base.with_name("rapport.json")
        ecrire_rapport(rap, chemin)
        donnees = json.loads(chemin.read_text(encoding="utf-8"))
        self.assertTrue(donnees["ok"])
        self.assertTrue(donnees["tables"])

    def test_dossier_vide_echec_propre(self):
        vide = self.exports.parent / "vide"
        vide.mkdir()
        moteur = MoteurMigration(vide, self.base)
        rap = moteur.executer()
        self.assertFalse(rap.ok)
        self.assertTrue(any("aucun export" in a for a in rap.anomalies))


class TestAssistantMigration(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        racine = Path(self._tmp.name)
        self.exports = racine / "export_leo2"
        self.base = racine / "postek.db"
        creer_exports(self.exports)

    def tearDown(self):
        self._tmp.cleanup()

    def test_assistant_generates_step_progress(self):
        assistant = AssistantMigration(self.exports, self.base)
        result = assistant.executer()
        self.assertTrue(result["ok"])
        etapes = [e["etape"] for e in result["etapes"]]
        self.assertEqual(etapes, ["analyse", "sauvegarde", "import", "validation", "rapport"])
        self.assertTrue(any(e["etape"] == "rapport" and e["ok"] for e in result["etapes"]))


class TestMigrationOffline(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.racine = Path(self._tmp.name)
        self.source = self.racine / "leo2"
        self.source.mkdir()
        self.base = self.racine / "postek.db"

    def tearDown(self):
        self._tmp.cleanup()

    def ecrire_csv(self, nom: str, contenu: str):
        chemin = self.source / nom
        chemin.write_text(contenu, encoding="utf-8")
        return chemin

    def test_detecter_dossier_leo2_trouve(self):
        self.ecrire_csv("clients.csv", "CODE;NOM\nC1;Client\n")
        self.assertEqual(MoteurMigration.detecter_dossier_leo2(self.racine), self.source)

    def test_detecter_dossier_leo2_non_trouve(self):
        self.assertIsNone(MoteurMigration.detecter_dossier_leo2(self.racine))

    def test_lister_exports_disponibles_et_encodage(self):
        fichier = self.ecrire_csv("clients.CSV", "CODE;NOM\nC1;Été\n")
        reader = ExportReader(self.source)
        self.assertEqual([fichier], reader.lister_exports_disponibles())
        self.assertEqual("utf-8", detecter_encodage(fichier))
        self.assertEqual("Été", next(reader.lire("CLIENTS"))["NOM"])

    def test_importer_clients_depuis_csv(self):
        fichier = self.ecrire_csv(
            "clients.csv",
            "CODE;NOM;VILLE;SOLDE\nC1;Client Alpha;Tunis;12,500\n",
        )
        resultat = MoteurMigration(self.source, self.base).importer_clients(fichier)
        self.assertTrue(resultat.ok, resultat.anomalies)
        with sqlite3.connect(self.base) as cnx:
            self.assertEqual(("Client Alpha",), cnx.execute(
                "SELECT raison_sociale FROM client WHERE code='C1'"
            ).fetchone())

    def test_importer_mouvements_rejette_produit_inconnu(self):
        self.ecrire_csv("Produits.csv", "REF;DESIGNATION;PVTTTC\nP1;Produit;1,000\n")
        self.ecrire_csv(
            "Mouvements.csv",
            "NUMEROPIECE;CODEPRODUIT;QUANTITE\nM1;P1;1\nM2;INCONNU;1\n",
        )
        resultat = MoteurMigration(self.source, self.base).executer()
        self.assertFalse(resultat.ok)
        self.assertTrue(any("produit inexistant" in a for a in resultat.anomalies))
        with sqlite3.connect(self.base) as cnx:
            self.assertEqual(1, cnx.execute("SELECT COUNT(*) FROM mouvement").fetchone()[0])

    def test_rollback_apres_echec(self):
        self.ecrire_csv("clients.csv", "CODE;NOM\nC1;Client\n")
        with sqlite3.connect(self.base) as cnx:
            cnx.execute("CREATE TABLE marqueur (valeur TEXT)")
            cnx.execute("INSERT INTO marqueur VALUES ('avant')")
            cnx.commit()
        moteur = MoteurMigration(self.source, self.base)

        def echec(*args):
            raise RuntimeError("échec simulé")

        moteur._importer_table = echec
        with self.assertRaises(RuntimeError):
            moteur.executer()
        with sqlite3.connect(self.base) as cnx:
            self.assertEqual(("avant",), cnx.execute("SELECT valeur FROM marqueur").fetchone())


if __name__ == "__main__":
    unittest.main(verbosity=2)
