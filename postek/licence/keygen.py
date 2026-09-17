#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
keygen.py — Génération de clés et licences POSTEK. Usage EXCLUSIF éditeur.

Commandes :
  generer  --sortie ./cles
      Crée `private_key.hex` (secret, à garder offline) et `public_key.pub`
      (embarqué dans l'application POSTEK).

  offline  --cles ./cles --societe "X" --postes 3 --empreinte ABCD... [--expire 2027-09-17] [--sortie x.posteklic]
      Fabrique un fichier de licence offline lié à l'empreinte d'un poste.

  empreinte
      Affiche l'empreinte du poste local (utile pour les tests).

La clé privée ne doit JAMAIS être installée sur un poste client.
"""

from __future__ import annotations

import argparse
import json
import secrets
import sys
from pathlib import Path

from . import ed25519
from .fingerprint import empreinte_normalisee
from .token import FORMAT_LICENCE, signe_jeton


def cmd_generer(args: argparse.Namespace) -> int:
    sortie = Path(args.sortie)
    sortie.mkdir(parents=True, exist_ok=True)
    chemin_privee = sortie / "private_key.hex"
    chemin_publique = sortie / "public_key.pub"
    if chemin_privee.exists() and not args.force:
        print(f"[refus] {chemin_privee} existe déjà (--force pour écraser)")
        return 1
    cle_privee = secrets.token_bytes(32)
    cle_publique = ed25519.secret_to_public(cle_privee)
    chemin_privee.write_text(cle_privee.hex(), encoding="ascii")
    chemin_publique.write_text(cle_publique.hex(), encoding="ascii")
    print(f"[OK] clé privée  : {chemin_privee}  (À CONSERVER HORS LIGNE)")
    print(f"[OK] clé publique: {chemin_publique}  (à embarquer dans POSTEK)")
    return 0


def cmd_offline(args: argparse.Namespace) -> int:
    chemin_privee = Path(args.cles) / "private_key.hex"
    if not chemin_privee.is_file():
        print(f"[ERREUR] clé privée introuvable : {chemin_privee}")
        return 1
    cle_privee = bytes.fromhex(chemin_privee.read_text(encoding="ascii").strip())

    payload = {
        "format": FORMAT_LICENCE,
        "societe": args.societe,
        "postes": args.postes,
        "empreinte": args.empreinte.strip().upper().replace("-", ""),
        "emis_le": args.emis_le,
        "options": json.loads(args.options) if args.options else {},
    }
    if args.expire:
        payload["expiration"] = args.expire

    jeton = signe_jeton(cle_privee, payload)
    fichier = Path(args.sortie) if args.sortie else Path(
        f"licence_{payload['empreinte'][:8]}.posteklic"
    )
    fichier.write_text(
        json.dumps(jeton, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"[OK] licence offline écrite : {fichier}")
    return 0


def cmd_empreinte(_args: argparse.Namespace) -> int:
    print(empreinte_normalisee())
    return 0


def main(argv: list[str] | None = None) -> int:
    parseur = argparse.ArgumentParser(prog="keygen", description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    sous = parseur.add_subparsers(dest="commande", required=True)

    p1 = sous.add_parser("generer", help="génère la paire de clés Ed25519")
    p1.add_argument("--sortie", default="./cles")
    p1.add_argument("--force", action="store_true")
    p1.set_defaults(fn=cmd_generer)

    p2 = sous.add_parser("offline", help="fabrique une licence offline signée")
    p2.add_argument("--cles", default="./cles", help="dossier contenant private_key.hex")
    p2.add_argument("--societe", required=True)
    p2.add_argument("--postes", type=int, default=1)
    p2.add_argument("--empreinte", required=True, help="empreinte du poste client")
    p2.add_argument("--expire", default=None, help="date ISO (AAAA-MM-JJ)")
    p2.add_argument("--emis-le", dest="emis_le", required=True, help="date ISO")
    p2.add_argument("--options", default=None, help="JSON, ex '{\"hotel\":true}'")
    p2.add_argument("--sortie", default=None)
    p2.set_defaults(fn=cmd_offline)

    p3 = sous.add_parser("empreinte", help="affiche l'empreinte du poste local")
    p3.set_defaults(fn=cmd_empreinte)

    args = parseur.parse_args(argv)
    return args.fn(args)


if __name__ == "__main__":
    sys.exit(main())
