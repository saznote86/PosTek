#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
licence/ — Module de licence POSTEK.

Concepts
--------
- Une LICENCE est un jeton JSON signé **Ed25519** (RFC 8032).
- L'éditeur signe avec la clé privée (jamais distribuée) ; l'application
  POSTEK embarque uniquement la **clé publique** (`public_key.pub`) et peut
  donc vérifier n'importe quelle licence sans secret.
- Le jeton lie la licence à : la société, le nombre de postes, l'empreinte
  matérielle du poste (CPU + carte mère, cf. fingerprint.py), la date
  d'expiration et des options métier.
- Deux canaux d'activation :
    * ONLINE  : l'application appelle l'API du serveur de licences et
      reçoit le jeton signé (voir spec docs/MODULE_LICENCE.md).
    * OFFLINE : l'éditeur fabrique un fichier `*.posteklic` avec
      keygen.py ; le client l'importe depuis une clé USB.

Fichiers remplacés par rapport à l'existant : licence.FIC, licence.NDX,
info_lic_leo2 (et le verrou HDD hdd.ini) disparaissent totalement.
"""

from .fingerprint import empreinte_materielle, empreinte_normalisee
from .token import (
    EtatLicence,
    charger_cle_publique,
    etat_licence,
    signe_jeton,
    verifie_jeton,
)

__all__ = [
    "empreinte_materielle",
    "empreinte_normalisee",
    "EtatLicence",
    "charger_cle_publique",
    "signe_jeton",
    "verifie_jeton",
    "etat_licence",
]
