#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tests/test_fiscal.py — Tests du noyau fiscal tunisien POSTEK.

Couvre : TVA (19/13/7/0), TVA extraite du TTC, retenue à la source,
timbre fiscal, écriture de facture NACEF, équilibrage partie double,
chaînage SHA-256 du journal (détection d'altération), grand livre,
balance, persistance JSONL.
"""

from __future__ import annotations

import sys
import tempfile
import unittest
from decimal import Decimal
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from fiscal import comptabilite as cpta

D = Decimal


class TestTVA(unittest.TestCase):
    def test_taux_tunisiens(self):
        self.assertEqual(cpta.TAUX_TVA_TUNISIE["19"], D("0.190"))
        self.assertEqual(cpta.TAUX_TVA_TUNISIE["13"], D("0.130"))
        self.assertEqual(cpta.TAUX_TVA_TUNISIE["7"], D("0.070"))
        self.assertEqual(cpta.TAUX_TVA_TUNISIE["0"], D("0.000"))

    def test_tva_simple(self):
        # 1 000,000 HT à 19 % → 190,000 TVA
        self.assertEqual(cpta.tva_montant("1000.000", "0.19"), D("190.000"))
        # 100,000 HT à 7 % → 7,000
        self.assertEqual(cpta.tva_montant("100.000", "0.07"), D("7.000"))

    def test_arrondi_millime(self):
        # 100,005 × 19 % = 19,00095 → arrondi commercial → 19,001
        self.assertEqual(cpta.tva_montant("100.005", "0.19"), D("19.001"))

    def test_tva_depuis_ttc(self):
        # TTC 119,000 à 19 % → TVA 19,000 ; HT 100,000
        tva = cpta.tva_depuis_ttc("119.000", "0.19")
        self.assertEqual(tva, D("19.000"))
        ht = D("119.000") - tva
        self.assertEqual(ht, D("100.000"))

    def test_tva_zero(self):
        self.assertEqual(cpta.tva_montant("500.000", "0"), D("0.000"))


class TestRS_Timbre(unittest.TestCase):
    def test_retenue_source(self):
        # RS 25 % sur 2 000,000 d'honoraires → 500,000
        self.assertEqual(cpta.retenue_source("2000.000", "0.25"), D("500.000"))

    def test_timbre_defaut_1_dt(self):
        self.assertEqual(cpta.timbre_fiscal(), D("1.000"))


class TestEcritureFacture(unittest.TestCase):
    def test_facture_equilibree_avec_tva_rs_timbre(self):
        lignes = cpta.ecriture_facture(
            numero_facture="F2026-000042",
            date_facture="2026-09-17",
            client_code="C001",
            total_ht="1000.000",
            taux_tva="0.19",
            timbre=D("1.000"),
            rs_taux="0.25",
        )
        ecr = cpta.Ecriture(numero=1, date="2026-09-17", journal="VTE",
                            libelle="test", lignes=lignes)
        self.assertTrue(ecr.est_equilibree(), "débits != crédits")
        # Débit client = TTC (1190) + timbre (1) − RS (250) = 941,000
        client = next(l for l in lignes if l.compte == cpta.COMPTE_CLIENT)
        self.assertEqual(client.debit, D("941.000"))
        # TVA collectée 190,000
        tva = next(l for l in lignes if l.compte == cpta.COMPTE_TVA_COLLECTEE)
        self.assertEqual(tva.credit, D("190.000"))

    def test_facture_sans_rs_sans_timbre(self):
        lignes = cpta.ecriture_facture(
            "F2026-000043", "2026-09-17", "C002", "100.000", "0.19"
        )
        ecr = cpta.Ecriture(numero=1, date="2026-09-17", journal="VTE",
                            libelle="test", lignes=lignes)
        self.assertTrue(ecr.est_equilibree())
        client = next(l for l in lignes if l.compte == cpta.COMPTE_CLIENT)
        self.assertEqual(client.debit, D("119.000"))


class TestJournalChainé(unittest.TestCase):
    def ecriture_exemple(self, journal, numero_attendu, precedent):
        lignes = [
            cpta.LigneEcriture("411", "client", debit=D("119.000")),
            cpta.LigneEcriture("701", "ventes", credit=D("100.000")),
            cpta.LigneEcriture("4367", "TVA", credit=D("19.000")),
        ]
        return lignes

    def test_bilan_et_resultat_nacef(self):
        with tempfile.TemporaryDirectory() as tmp:
            j = cpta.JournalComptable(Path(tmp) / "journal.jsonl")
            j.passer_ecriture(
                "2026-09-17", "VTE", "vente 1",
                [
                    cpta.LigneEcriture("411", "client", debit=D("119.000")),
                    cpta.LigneEcriture("701", "ventes", credit=D("100.000")),
                    cpta.LigneEcriture("4367", "TVA", credit=D("19.000")),
                ],
            )
            j.passer_ecriture(
                "2026-09-17", "ACH", "achat 1",
                [
                    cpta.LigneEcriture("601", "achats", debit=D("200.000")),
                    cpta.LigneEcriture("512", "banque", credit=D("200.000")),
                ],
            )

            etat = j.etat_resultat()
            self.assertEqual(etat["produits_total"], D("100.000"))
            self.assertEqual(etat["charges_total"], D("200.000"))
            self.assertEqual(etat["resultat_net"], D("-100.000"))

            bilan = j.bilan()
            self.assertTrue(bilan["equilibre"])
            self.assertGreater(bilan["total_actif"], D("0"))
            self.assertGreater(bilan["total_passif"], D("0"))

    def test_chaine_et_verif(self):
        with tempfile.TemporaryDirectory() as tmp:
            j = cpta.JournalComptable(Path(tmp) / "journal.jsonl")
            e1 = j.passer_ecriture("2026-09-17", "VTE", "Vente 1",
                                   self.ecriture_exemple("VTE", 1, "0"))
            e2 = j.passer_ecriture("2026-09-17", "VTE", "Vente 2",
                                   self.ecriture_exemple("VTE", 2, e1.hash))
            self.assertEqual(e1.numero, 1)
            self.assertEqual(e2.numero, 2)
            self.assertEqual(e2.hash_precedent, e1.hash)
            ok, raison = j.verifier_chaine()
            self.assertTrue(ok, raison)

            # Persistance JSONL : réouverture → même chaîne.
            j2 = cpta.JournalComptable(Path(tmp) / "journal.jsonl")
            self.assertEqual(len(j2.ecritures), 2)
            ok2, _ = j2.verifier_chaine()
            self.assertTrue(ok2)

    def test_detection_alteration(self):
        with tempfile.TemporaryDirectory() as tmp:
            chemin = Path(tmp) / "journal.jsonl"
            j = cpta.JournalComptable(chemin)
            j.passer_ecriture("2026-09-17", "VTE", "Vente 1",
                              self.ecriture_exemple("VTE", 1, "0"))
            j.passer_ecriture("2026-09-17", "VTE", "Vente 2",
                              self.ecriture_exemple("VTE", 2, j.ecritures[0].hash))

            # Altération frauduleuse : on modifie le libellé de l'écriture 1.
            texte = chemin.read_text(encoding="utf-8")
            texte_falsifie = texte.replace("Vente 1", "Vente X")
            chemin.write_text(texte_falsifie, encoding="utf-8")

            j3 = cpta.JournalComptable(chemin)
            ok, raison = j3.verifier_chaine()
            self.assertFalse(ok)
            self.assertIn("altérée", raison)

    def test_ecriture_desequilibree_refusee(self):
        with tempfile.TemporaryDirectory() as tmp:
            j = cpta.JournalComptable(Path(tmp) / "journal.jsonl")
            mauvaises = [
                cpta.LigneEcriture("411", "client", debit=D("100.000")),
                cpta.LigneEcriture("701", "ventes", credit=D("90.000")),
            ]
            with self.assertRaises(ValueError):
                j.passer_ecriture("2026-09-17", "VTE", "faux", mauvaises)

    def test_grand_livre_et_balance(self):
        with tempfile.TemporaryDirectory() as tmp:
            j = cpta.JournalComptable(Path(tmp) / "journal.jsonl")
            j.passer_ecriture("2026-09-17", "VTE", "Vente 1",
                              self.ecriture_exemple("VTE", 1, "0"))
            livre = j.grand_livre()
            self.assertIn("411", livre)
            self.assertIn("701", livre)
            bal = {b["compte"]: b for b in j.balance()}
            self.assertEqual(bal["411"]["debit"], D("119.000"))
            self.assertEqual(bal["701"]["credit"], D("100.000"))
            # Balance équilibrée : somme soldes D = somme soldes C
            total_d = sum(b["solde_debiteur"] for b in j.balance())
            total_c = sum(b["solde_crediteur"] for b in j.balance())
            self.assertEqual(total_d, total_c)


if __name__ == "__main__":
    unittest.main(verbosity=2)
