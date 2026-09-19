#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
comptabilite.py — Noyau fiscal & comptable POSTEK (conformité tunisienne).

Couvre :
  - TVA tunisienne : taux 19 / 13 / 7 / 0 %, calculs en Decimal (millimes),
    arrondi commercial ROUND_HALF_UP — jamais de flottant pour l'argent.
  - Retenue à la source (RS) paramétrable (taux par nature de prestation).
  - Timbre fiscal (montant fixe paramétrable, 1,000 DT par défaut).
  - Journal des écritures **chaîné par SHA-256** : chaque écriture porte
    l'empreinte de la précédente ; toute modification ou suppression
    d'écriture invalide la chaîne (inaltérabilité, archivage 10 ans).
  - Grand livre et balance dérivés du journal.

Comptes par défaut (plan SCT tunisien) : à confirmer avec l'expert-comptable
avant mise en production — voir docs/CAHIER_DES_CHARGES.md §3.2.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

MILLIMES = Decimal("0.001")
CENTIME = Decimal("0.01")

# Taux de TVA tunisiens (table de référence — POSTEK les stocke datés en base).
TAUX_TVA_TUNISIE = {
    "19": Decimal("0.190"),
    "13": Decimal("0.130"),
    "7": Decimal("0.070"),
    "0": Decimal("0.000"),
}

# Comptes par défaut (paramétrables en base).
COMPTE_CLIENT = "411"
COMPTE_VENTES = "701"
COMPTE_TVA_COLLECTEE = "4367"
COMPTE_TTBRE = "6581"      # droits de timbre (défaut, à valider)
COMPTE_RS = "4432"         # retenue à la source subie (défaut, à valider)


def _d(valeur) -> Decimal:
    """Convertit en Decimal sans erreur de représentation binaire."""
    return valeur if isinstance(valeur, Decimal) else Decimal(str(valeur))


def tva_montant(montant_ht, taux) -> Decimal:
    """Montant de TVA = HT × taux, arrondi au millime (ROUND_HALF_UP)."""
    return (_d(montant_ht) * _d(taux)).quantize(MILLIMES, rounding=ROUND_HALF_UP)


def tva_depuis_ttc(montant_ttc, taux) -> Decimal:
    """TVA contenue dans un TTC : TTC × taux/(1+taux), arrondie au millime."""
    taux = _d(taux)
    return (_d(montant_ttc) * taux / (Decimal(1) + taux)).quantize(
        MILLIMES, rounding=ROUND_HALF_UP
    )


def retenue_source(montant_assiette, taux) -> Decimal:
    """Retenue à la source = assiette × taux (paramétrable par prestation)."""
    return (_d(montant_assiette) * _d(taux)).quantize(MILLIMES, rounding=ROUND_HALF_UP)


def timbre_fiscal(tarif=Decimal("1.000")) -> Decimal:
    """Timbre fiscal (1,000 DT par défaut ; exemptions gérées en amont)."""
    return _d(tarif)


# ---------------------------------------------------------------------------
# Écritures & journal chaîné
# ---------------------------------------------------------------------------
@dataclass
class LigneEcriture:
    compte: str
    libelle: str
    debit: Decimal = Decimal("0.000")
    credit: Decimal = Decimal("0.000")


@dataclass
class Ecriture:
    numero: int
    date: str                      # ISO-8601
    journal: str                   # ex. "VTE" ventes, "BQ" banque, "CS" caisse
    libelle: str
    lignes: list[LigneEcriture] = field(default_factory=list)
    hash_precedent: str = "0" * 64
    hash: str = ""

    def est_equilibree(self) -> bool:
        total_d = sum((l.debit for l in self.lignes), Decimal("0"))
        total_c = sum((l.credit for l in self.lignes), Decimal("0"))
        return total_d == total_c and total_d > 0

    def payload_canonique(self) -> str:
        """Charge canonique hashée (JSON trié) — stable et reproductible."""
        payload = {
            "numero": self.numero,
            "date": self.date,
            "journal": self.journal,
            "libelle": self.libelle,
            "hash_precedent": self.hash_precedent,
            "lignes": [
                [l.compte, l.libelle, str(l.debit), str(l.credit)]
                for l in self.lignes
            ],
        }
        return json.dumps(payload, sort_keys=True, separators=(",", ":"), ensure_ascii=False)

    def scelle(self) -> None:
        """Calcule et pose l'empreinte SHA-256 de l'écriture."""
        self.hash = hashlib.sha256(self.payload_canonique().encode("utf-8")).hexdigest()


class JournalComptable:
    """Journal d'écritures inaltérable (chaînage SHA-256) persisté en JSONL."""

    def __init__(self, chemin: str | Path):
        self.chemin = Path(chemin)
        self.ecritures: list[Ecriture] = []
        self._nb_persistees = 0
        if self.chemin.is_file():
            self.charger()

    # ------------------------------------------------------------------
    def charger(self) -> None:
        self.ecritures = []
        for ligne in self.chemin.read_text(encoding="utf-8").splitlines():
            if not ligne.strip():
                continue
            d = json.loads(ligne)
            lignes = [
                LigneEcriture(
                    compte=l[0], libelle=l[1],
                    debit=Decimal(l[2]), credit=Decimal(l[3]),
                )
                for l in d["lignes"]
            ]
            self.ecritures.append(
                Ecriture(
                    numero=d["numero"], date=d["date"], journal=d["journal"],
                    libelle=d["libelle"], lignes=lignes,
                    hash_precedent=d["hash_precedent"], hash=d["hash"],
                )
            )
        self._nb_persistees = len(self.ecritures)

    def _persister(self) -> None:
        self.chemin.parent.mkdir(parents=True, exist_ok=True)
        with self.chemin.open("a", encoding="utf-8") as f:
            for e in self.ecritures[self._nb_persistees:]:
                f.write(json.dumps(
                    {
                        "numero": e.numero, "date": e.date, "journal": e.journal,
                        "libelle": e.libelle,
                        "lignes": [[l.compte, l.libelle, str(l.debit), str(l.credit)]
                                   for l in e.lignes],
                        "hash_precedent": e.hash_precedent, "hash": e.hash,
                    },
                    ensure_ascii=False,
                ) + "\n")
        self._nb_persistees = len(self.ecritures)

    # ------------------------------------------------------------------
    def passer_ecriture(self, date: str, journal: str, libelle: str,
                        lignes: list[LigneEcriture]) -> Ecriture:
        """Ajoute une écriture équilibrée, scellée sur la chaîne.

        Une écriture déséquilibrée est refusée (principe partie double).
        Les écritures ne sont JAMAIS supprimées ni modifiées : une correction
        est une écriture inversée (contre-passation).
        """
        numero = (self.ecritures[-1].numero + 1) if self.ecritures else 1
        ecr = Ecriture(
            numero=numero, date=date, journal=journal, libelle=libelle,
            lignes=lignes,
            hash_precedent=self.ecritures[-1].hash if self.ecritures else "0" * 64,
        )
        if not ecr.est_equilibree():
            raise ValueError(f"écriture non équilibrée : {libelle}")
        ecr.scelle()
        self.ecritures.append(ecr)
        self._persister()
        return ecr

    # ------------------------------------------------------------------
    def verifier_chaine(self) -> tuple[bool, str]:
        """Re-scelle chaque écriture et compare : détecte toute altération."""
        precedent = "0" * 64
        for ecr in self.ecritures:
            if ecr.hash_precedent != precedent:
                return False, f"chaîne brisée à l'écriture n° {ecr.numero}"
            hash_calculé = hashlib.sha256(ecr.payload_canonique().encode("utf-8")).hexdigest()
            if hash_calculé != ecr.hash:
                return False, f"écriture altérée : n° {ecr.numero}"
            precedent = ecr.hash
        return True, "chaîne intègre"

    # ------------------------------------------------------------------
    # États de synthèse
    # ------------------------------------------------------------------
    def grand_livre(self) -> dict[str, list[Ecriture]]:
        """{compte: [écritures mouvementant ce compte]}."""
        livre: dict[str, list[Ecriture]] = {}
        for ecr in self.ecritures:
            for l in ecr.lignes:
                livre.setdefault(l.compte, []).append(ecr)
        return livre

    def grand_livre_detail(self) -> list[dict]:
        """Version structurée du grand livre pour un export comptable / UI."""
        details: list[dict] = []
        for compte, ecritures in sorted(self.grand_livre().items()):
            lignes: list[dict] = []
            for e in ecritures:
                for l in e.lignes:
                    if l.compte != compte:
                        continue
                    lignes.append(
                        {
                            "numero": e.numero,
                            "date": e.date,
                            "journal": e.journal,
                            "libelle": e.libelle,
                            "debit": l.debit,
                            "credit": l.credit,
                        }
                    )
            solde = sum((ligne["debit"] for ligne in lignes), Decimal("0")) - sum(
                (ligne["credit"] for ligne in lignes), Decimal("0")
            )
            details.append({
                "compte": compte,
                "solde": solde,
                "lignes": lignes,
            })
        return details

    def balance(self) -> list[dict]:
        """Balance générale : totaux débit/crédit et soldes par compte."""
        totaux: dict[str, dict[str, Decimal]] = {}
        for ecr in self.ecritures:
            for l in ecr.lignes:
                t = totaux.setdefault(
                    l.compte, {"debit": Decimal("0"), "credit": Decimal("0")}
                )
                t["debit"] += l.debit
                t["credit"] += l.credit
        return [
            {
                "compte": compte,
                "debit": t["debit"],
                "credit": t["credit"],
                "solde_debiteur": t["debit"] - t["credit"]
                if t["debit"] >= t["credit"] else Decimal("0"),
                "solde_crediteur": t["credit"] - t["debit"]
                if t["credit"] > t["debit"] else Decimal("0"),
            }
            for compte, t in sorted(totaux.items())
        ]

    def etat_resultat(self) -> dict:
        """État de résultat synthétique (NACEF, version de base)."""
        produits: dict[str, Decimal] = {}
        charges: dict[str, Decimal] = {}

        for ecr in self.ecritures:
            for l in ecr.lignes:
                compte = str(l.compte)
                if compte.startswith("7"):
                    produits.setdefault(compte, Decimal("0"))
                    produits[compte] += l.credit
                elif compte.startswith("6"):
                    charges.setdefault(compte, Decimal("0"))
                    charges[compte] += l.debit

        produits_total = sum(produits.values(), Decimal("0"))
        charges_total = sum(charges.values(), Decimal("0"))
        resultat_net = produits_total - charges_total
        return {
            "produits_total": produits_total,
            "charges_total": charges_total,
            "resultat_net": resultat_net,
            "produits_details": dict(sorted(produits.items())),
            "charges_details": dict(sorted(charges.items())),
        }

    def bilan(self) -> dict:
        """Bilan synthétique : actif / passif / équilibre."""
        totaux: dict[str, Decimal] = {}
        for ecr in self.ecritures:
            for l in ecr.lignes:
                compte = str(l.compte)
                totaux.setdefault(compte, Decimal("0"))
                totaux[compte] += (l.debit - l.credit)

        actifs = Decimal("0")
        passifs = Decimal("0")
        details: list[dict] = []
        for compte, solde in sorted(totaux.items()):
            if solde > 0:
                nature = "actif"
                actifs += solde
            elif solde < 0:
                nature = "passif"
                passifs += abs(solde)
            else:
                nature = "nul"
            details.append({
                "compte": compte,
                "solde": solde,
                "nature": nature,
            })

        if passifs == Decimal("0"):
            passifs = actifs

        return {
            "actif": actifs,
            "passif": passifs,
            "total_actif": actifs,
            "total_passif": passifs,
            "equilibre": abs(actifs - passifs) <= Decimal("0.001"),
            "details": details,
        }


# ---------------------------------------------------------------------------
# Écriture de vente NACEF type (facture → journal)
# ---------------------------------------------------------------------------
def ecriture_facture(
    numero_facture: str,
    date_facture: str,
    client_code: str,
    total_ht,
    taux_tva,
    timbre=Decimal("0.000"),
    rs_taux=Decimal("0"),
) -> list[LigneEcriture]:
    """Construit les lignes comptables d'une facture de vente tunisienne.

    Débit  411  Client (TTC + timbre − RS)
    Crédit 701  Ventes (HT)
    Crédit 4367 TVA collectée
    Débit  4432 RS (si applicable)  /  Crédit/Débit timbre selon régime.
    """
    ht = _d(total_ht)
    tva = tva_montant(ht, taux_tva)
    rs = retenue_source(ht, rs_taux) if _d(rs_taux) > 0 else Decimal("0")
    ttc = ht + tva
    net_client = ttc - rs + _d(timbre)

    lignes = [
        LigneEcriture(COMPTE_CLIENT, f"Facture {numero_facture} — {client_code}",
                      debit=net_client),
        LigneEcriture(COMPTE_VENTES, f"Ventes {numero_facture}", credit=ht),
        LigneEcriture(COMPTE_TVA_COLLECTEE, f"TVA collectée {numero_facture}", credit=tva),
    ]
    if rs > 0:
        lignes.append(LigneEcriture(COMPTE_RS, f"RS {numero_facture}", debit=rs))
    if _d(timbre) > 0:
        lignes.append(LigneEcriture(COMPTE_TTBRE, f"Timbre fiscal {numero_facture}",
                                    credit=_d(timbre)))
    return lignes
