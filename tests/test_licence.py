#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tests/test_licence.py — Tests du module licence POSTEK.

Couvre :
  1. Ed25519 contre les vecteurs de test officiels du RFC 8032
     (TEST 1, TEST 2, TEST 3 + SHA(abc)).
  2. Roundtrip jeton de licence : signature → vérification, altération,
     expiration, mauvaise empreinte, grâce offline.
  3. Empreinte matérielle : format et stabilité locale.
  4. Keygen CLI : génération de clés + licence offline de bout en bout.
"""

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from datetime import date, timedelta
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from licence import ed25519, fingerprint, token
from licence.keygen import main as keygen_main


# ---------------------------------------------------------------------------
# 1. Vecteurs RFC 8032 (§7.1)
# ---------------------------------------------------------------------------
VECTEURS_RFC8032 = [
    {
        # TEST 1 (clé vide, message vide)
        "secret": "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60",
        "public": "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a",
        "message": "",
        "signature": (
            "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e06522490155"
            "5fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b"
        ),
    },
    {
        # TEST 2 (1 octet 0x72)
        "secret": "4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb",
        "public": "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c",
        "message": "72",
        "signature": (
            "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da"
            "085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00"
        ),
    },
    {
        # TEST 3 (2 octets 0xaf82)
        "secret": "c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7",
        "public": "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025",
        "message": "af82",
        "signature": (
            "6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac"
            "18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40a"
        ),
    },
    {
        # TEST SHA(abc)
        "secret": "833fe62409237b9d62ec77587520911e9a759cec1d19755b7da901b96dca3d42",
        "public": "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf",
        "message": ("ddaf35a193617abacc417349ae204131"
                    "12e6fa4e89a97ea20a9eeee64b55d39a"
                    "2192992a274fc1a836ba3c23a3feebbd"
                    "454d4423643ce80e2a9ac94fa54ca49f"),
        "signature": (
            "dc2a4459e7369633a52b1bf277839a00201009a3efbf3ecb69bea2186c26b589"
            "09351fc9ac90b3ecfdfbc7c66431e0303dca179c138ac17ad9bef1177331a704"
        ),
    },
]


class TestEd25519RFC8032(unittest.TestCase):
    def test_vecteurs_officiels(self):
        for i, v in enumerate(VECTEURS_RFC8032, 1):
            with self.subTest(vecteur=i):
                secret = bytes.fromhex(v["secret"])
                message = bytes.fromhex(v["message"])
                attendue = bytes.fromhex(v["signature"])
                self.assertEqual(ed25519.secret_to_public(secret).hex(), v["public"])
                self.assertEqual(ed25519.sign(secret, message), attendue)
                self.assertTrue(
                    ed25519.verify(bytes.fromhex(v["public"]), message, attendue)
                )

    def test_signature_negatives(self):
        v = VECTEURS_RFC8032[0]
        public = bytes.fromhex(v["public"])
        message = b"autre message"
        signature = bytes.fromhex(v["signature"])
        self.assertFalse(ed25519.verify(public, message, signature))
        signature_alteree = bytearray(signature)
        signature_alteree[10] ^= 0x01
        self.assertFalse(ed25519.verify(public, b"", bytes(signature_alteree)))
        self.assertFalse(ed25519.verify(b"\x00" * 32, b"", signature))


# ---------------------------------------------------------------------------
# 2. Jetons de licence
# ---------------------------------------------------------------------------
SECRET_ED25519 = bytes.fromhex(
    "4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb"
)
PUBLIQUE = ed25519.secret_to_public(SECRET_ED25519)

PAYLOAD = {
    "format": token.FORMAT_LICENCE,
    "societe": "Boulangerie El Manar",
    "postes": 2,
    "empreinte": "K7Q29MTX3ABPCDEF0123",
    "emis_le": "2026-09-01",
    "expiration": "2027-09-01",
    "options": {"hotel": False},
}


def jeton_valide() -> dict:
    return token.signe_jeton(SECRET_ED25519, dict(PAYLOAD))


class TestJeton(unittest.TestCase):
    def test_roundtrip_valide(self):
        ok, raison = token.verifie_jeton(jeton_valide(), PUBLIQUE)
        self.assertTrue(ok, raison)

    def test_payload_altere(self):
        jeton = jeton_valide()
        jeton["payload"]["postes"] = 500  # fraude : plus de postes
        ok, raison = token.verifie_jeton(jeton, PUBLIQUE)
        self.assertFalse(ok)
        self.assertEqual(raison, "signature invalide")

    def test_signature_remplacee(self):
        jeton = jeton_valide()
        autre = token.signe_jeton(
            bytes.fromhex(
                "c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7"
            ),
            jeton["payload"],
        )
        ok, _ = token.verifie_jeton(autre, PUBLIQUE)
        self.assertFalse(ok)  # signée par une autre clé

    def test_champ_manquant(self):
        jeton = token.signe_jeton(
            SECRET_ED25519, {"societe": "X", "postes": 1}  # empreinte absente
        )
        ok, raison = token.verifie_jeton(jeton, PUBLIQUE)
        self.assertFalse(ok)
        self.assertIn("empreinte", raison)


class TestEtatLicence(unittest.TestCase):
    EMPREINTE_POSTE = "K7Q29MTX3ABPCDEF0123"

    def etat(self, jeton, **kwargs):
        params = dict(
            aujourd_hui=date(2026, 9, 17),
            dernier_contact_ok=date(2026, 9, 17),
        )
        params.update(kwargs)
        return token.etat_licence(
            jeton, PUBLIQUE, self.EMPREINTE_POSTE, **params
        )

    def test_valide(self):
        etat = self.etat(jeton_valide())
        self.assertTrue(etat.valide, etat.raison)

    def test_empreinte_differente(self):
        etat = self.etat(jeton_valide())
        # Simule un autre poste :
        etat2 = token.etat_licence(
            jeton_valide(), PUBLIQUE, "POSTE-DE-QUI",
            aujourd_hui=date(2026, 9, 17),
            dernier_contact_ok=date(2026, 9, 17),
        )
        self.assertFalse(etat2.valide)
        self.assertIn("autre poste", etat2.raison)
        self.assertTrue(etat.valide)  # contrôle croisé

    def test_expiree(self):
        jeton = jeton_valide()
        etat = self.etat(jeton, aujourd_hui=date(2027, 9, 2))
        self.assertFalse(etat.valide)
        self.assertEqual(etat.raison, "licence expirée")

    def test_grace_offline_respectee(self):
        etat = self.etat(
            jeton_valide(),
            dernier_contact_ok=date(2026, 9, 14),  # 3 jours sans contact
        )
        self.assertTrue(etat.valide, etat.raison)

    def test_grace_offline_depassee(self):
        etat = self.etat(
            jeton_valide(),
            dernier_contact_ok=date(2026, 9, 1),  # 16 jours sans contact
        )
        self.assertFalse(etat.valide)
        self.assertIn("validation", etat.raison)


# ---------------------------------------------------------------------------
# 3. Empreinte matérielle
# ---------------------------------------------------------------------------
class TestEmpreinte(unittest.TestCase):
    def test_format(self):
        e = fingerprint.empreinte_materielle()
        self.assertTrue(e.startswith("POSTEK1-"))
        corps = e.replace("POSTEK1-", "")
        blocs = corps.split("-")
        self.assertEqual(len(blocs), 5)
        for bloc in blocs:
            self.assertEqual(len(bloc), 4)
            self.assertTrue(set(bloc) <= set(fingerprint.BASE32_ALPHABET))

    def test_normalee(self):
        n = fingerprint.empreinte_normalisee()
        self.assertNotIn("-", n)
        self.assertNotIn("POSTEK1", n)


# ---------------------------------------------------------------------------
# 4. Keygen CLI (bout en bout, dossier temporaire)
# ---------------------------------------------------------------------------
class TestKeygenCLI(unittest.TestCase):
    def test_generation_licence_offline(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            cles = tmp / "cles"
            self.assertEqual(keygen_main(["generer", "--sortie", str(cles)]), 0)
            privee = (cles / "private_key.hex").read_text().strip()
            publique = bytes.fromhex((cles / "public_key.pub").read_text().strip())
            self.assertEqual(len(bytes.fromhex(privee)), 32)
            self.assertEqual(ed25519.secret_to_public(bytes.fromhex(privee)), publique)

            empreinte = fingerprint.empreinte_normalisee()
            sortie = tmp / "test.posteklic"
            self.assertEqual(
                keygen_main([
                    "offline", "--cles", str(cles),
                    "--societe", "Test SARL", "--postes", "1",
                    "--empreinte", empreinte,
                    "--emis-le", "2026-09-17",
                    "--expire", "2027-09-17",
                    "--sortie", str(sortie),
                ]),
                0,
            )
            jeton = json.loads(sortie.read_text(encoding="utf-8"))
            ok, raison = token.verifie_jeton(jeton, publique)
            self.assertTrue(ok, raison)
            etat = token.etat_licence(
                jeton, publique, empreinte,
                aujourd_hui=date.today(),
                dernier_contact_ok=date.today(),
            )
            self.assertTrue(etat.valide, etat.raison)


if __name__ == "__main__":
    unittest.main(verbosity=2)
