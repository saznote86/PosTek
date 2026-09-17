#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
token.py — Jetons de licence POSTEK : création, vérification, état.

Format d'un fichier de licence `*.posteklic` (format offline) :
  {"format": "POSTEK-LIC-1",
   "jeton": {"payload": {...}, "signature_b64": "..."}}

Payload type :
  {"societe": "Boulangerie X",
   "postes": 3,
   "empreinte": "K7Q29MTX3ABPCDEF0123",
   "expiration": "2027-09-17",          # ISO-8601 (date), ou absent = perpétuelle
   "options": {"hotel": true, "pesage": false},
   "emis_le": "2026-09-17"}

Vérification : Ed25519 + contrôle expiration + contrôle empreinte du poste.
La fenêtre de grâce offline (7 jours sans validation réseau réussie) est
gérée par l'application hôte via etat_licence(...).
"""

from __future__ import annotations

import base64
import json
from dataclasses import dataclass
from datetime import date, timedelta
from pathlib import Path

from . import ed25519

GRACE_JOURS = 7  # grâce offline après la dernière validation réussie

FORMAT_LICENCE = "POSTEK-LIC-1"


# ---------------------------------------------------------------------------
# Clés
# ---------------------------------------------------------------------------
def charger_cle_publique(chemin: str | Path) -> bytes:
    """Charge `public_key.pub` (64 hex) → 32 octets."""
    hexa = Path(chemin).read_text(encoding="ascii").strip()
    if len(hexa) != 64:
        raise ValueError("public_key.pub invalide (64 caractères hex attendus)")
    return bytes.fromhex(hexa)


# ---------------------------------------------------------------------------
# Jetons
# ---------------------------------------------------------------------------
def signe_jeton(cle_privee: bytes, payload: dict) -> dict:
    """Signe un payload et renvoie le jeton {'payload':..., 'signature_b64':...}."""
    message = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()
    signature = ed25519.sign(cle_privee, message)
    return {
        "payload": payload,
        "signature_b64": base64.b64encode(signature).decode("ascii"),
    }


def verifie_jeton(jeton: dict, cle_publique: bytes) -> tuple[bool, str]:
    """Vérifie signature + cohérence minimale du jeton.

    Renvoie (ok, raison). Ne vérifie PAS l'empreinte ni l'expiration :
    c'est le rôle de etat_licence().
    """
    if not isinstance(jeton, dict) or "payload" not in jeton or "signature_b64" not in jeton:
        return False, "jeton malformé"
    payload = jeton["payload"]
    message = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()
    try:
        signature = base64.b64decode(jeton["signature_b64"], validate=True)
    except Exception:
        return False, "signature illisible"
    if not ed25519.verify(cle_publique, message, signature):
        return False, "signature invalide"
    for champ in ("societe", "postes", "empreinte"):
        if champ not in payload:
            return False, f"champ obligatoire absent : {champ}"
    return True, "ok"


@dataclass
class EtatLicence:
    valide: bool
    raison: str
    jours_grace_restants: int | None = None
    expiration: date | None = None


def _parse_date_iso(s: str) -> date | None:
    try:
        return date.fromisoformat(s)
    except (TypeError, ValueError):
        return None


def etat_licence(
    jeton: dict,
    cle_publique: bytes,
    empreinte_poste: str,
    aujourd_hui: date | None = None,
    dernier_contact_ok: date | None = None,
) -> EtatLicence:
    """Décide si la licence autorise le démarrage de POSTEK.

    dernier_contact_ok : date de la dernière activation/validation en ligne
    réussie (None si jamais). Alimente la grâce offline de GRACE_JOURS.
    """
    aujourd_hui = aujourd_hui or date.today()

    ok, raison = verifie_jeton(jeton, cle_publique)
    if not ok:
        return EtatLicence(False, raison)

    payload = jeton["payload"]
    if payload["empreinte"] != empreinte_poste:
        return EtatLicence(False, "licence liée à un autre poste")

    expiration = _parse_date_iso(payload.get("expiration"))
    if expiration is not None and aujourd_hui > expiration:
        return EtatLicence(False, "licence expirée", expiration=expiration)

    # Grâce offline : si une validation en ligne a déjà eu lieu, tolérer
    # GRACE_JOURS sans contact ; sinon exiger l'activation dans les 7 jours.
    if dernier_contact_ok is not None:
        if aujourd_hui - dernier_contact_ok > timedelta(days=GRACE_JOURS):
            return EtatLicence(
                False,
                f"pas de validation depuis plus de {GRACE_JOURS} jours",
                expiration=expiration,
            )
    else:
        emis_le = _parse_date_iso(payload.get("emis_le"))
        if emis_le is not None and aujourd_hui - emis_le > timedelta(days=GRACE_JOURS):
            return EtatLicence(
                False,
                "licence jamais activée en ligne (délai dépassé)",
                expiration=expiration,
            )

    return EtatLicence(True, "licence valide", expiration=expiration)
