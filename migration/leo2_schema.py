#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
leo2_schema.py — Mapping déclaratif Atoo Leo2 → POSTEK.

Le schéma SOURCE est issu de l'analyse WinDev `MINICASH.xdd`
(docs/schema_leo2.json, 101 tables / 1115 rubriques extraites par
tools/parse_xdd.py). Ce module déclare, pour chaque table migrée :

  - la table source Leo2 (nom .FIC sans extension)
  - la table cible POSTEK
  - la clé naturelle (pour l'idempotence et la détection de doublons)
  - les conversions de champs (renommer / transformer)

Le mapping couvre les données MÉTIER. Les tables techniques internes de
Leo2 (VERROUS, index, fichiers de travail) ne sont pas migrées.
"""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass
class ConversionChamp:
    """Conversion d'un champ source vers un champ cible."""
    source: str
    cible: str
    transform: str = "copie"  # copie | majuscule | trim | matricule_fiscal | montant


@dataclass
class MappingTable:
    """Déclaration de migration d'une table."""
    source: str
    cible: str
    cle_naturelle: tuple[str, ...]
    champs: list[ConversionChamp] = field(default_factory=list)
    obligatoire: bool = False       # échec de migration si absente ?
    dependances: tuple[str, ...] = ()   # tables à migrer avant celle-ci

    @property
    def champs_simples(self) -> dict[str, str]:
        """Mapping {source: cible} pour les conversions de type 'copie'."""
        return {c.source: c.cible for c in self.champs if c.transform == "copie"}

    @property
    def cles_cibles(self) -> list[str]:
        """Colonnes cibles correspondant à la clé naturelle source.

        Ex. clé naturelle ('REF',) sur Produits → ['code'] (nom cible).
        Utilisé pour l'upsert (ON CONFLICT) côté moteur.
        """
        colonnes: list[str] = []
        for kn in self.cle_naturelle:
            for c in self.champs:
                if c.source.upper() == kn.upper():
                    colonnes.append(c.cible)
                    break
            else:
                colonnes.append(kn.lower())
        return colonnes


# ---------------------------------------------------------------------------
# Mappings (extraits cohérents avec docs/PLAN_MIGRATION.md §3)
# ---------------------------------------------------------------------------
MAPPINGS: dict[str, MappingTable] = {m.source.upper(): m for m in [
    MappingTable(
        source="FOURNISSEURS", cible="fournisseur", cle_naturelle=("CODE",),
        champs=[
            ConversionChamp("CODE", "code"), ConversionChamp("NOM", "nom", "trim"),
            ConversionChamp("RAISONSOCIALE", "raison_sociale", "trim"),
            ConversionChamp("ADRESSE", "adresse", "trim"), ConversionChamp("VILLE", "ville", "trim"),
            ConversionChamp("CODEPOSTAL", "code_postal"), ConversionChamp("TEL", "telephone"),
            ConversionChamp("EMAIL", "email"), ConversionChamp("MATRICULEFISCAL", "matricule_fiscal", "matricule_fiscal"),
            ConversionChamp("CONDITIONSREGLEMENT", "conditions_reglement"),
            ConversionChamp("DELAIPAIEMENT", "delai_paiement"), ConversionChamp("SOLDE", "solde", "montant"),
            ConversionChamp("COMPTECOMPTABLE", "compte_comptable"),
        ],
    ),
    MappingTable(
        source="UTILISATEURS", cible="utilisateur", cle_naturelle=("CODE",),
        champs=[
            ConversionChamp("CODE", "code"), ConversionChamp("NOM", "nom", "trim"),
            ConversionChamp("PRENOM", "prenom", "trim"), ConversionChamp("ROLE", "role", "trim"),
            ConversionChamp("DROITS", "droits_json", "trim"),
        ],
    ),
    MappingTable(
        source="MOUVEMENTS", cible="mouvement", cle_naturelle=("NUMEROPIECE", "CODEPRODUIT"),
        dependances=("Produits",),
        champs=[
            ConversionChamp("NUMEROPIECE", "numero_piece"), ConversionChamp("DATE", "date_mouvement"),
            ConversionChamp("TYPE", "type_mouvement"), ConversionChamp("CODECLIENT", "client_code"),
            ConversionChamp("CODEFOURNISSEUR", "fournisseur_code"), ConversionChamp("CODEPRODUIT", "produit_code"),
            ConversionChamp("QUANTITE", "quantite", "montant"), ConversionChamp("PRIXUNITAIRE", "prix_unitaire", "montant"),
            ConversionChamp("TOTALHT", "total_ht", "montant"), ConversionChamp("TVA", "tva", "montant"),
            ConversionChamp("TOTALTTC", "total_ttc", "montant"), ConversionChamp("MODEREGLEMENT", "mode_reglement"),
            ConversionChamp("REFERENCEPAIEMENT", "reference_paiement"),
        ],
    ),
    MappingTable(
        source="VENTES", cible="vente", cle_naturelle=("NUMEROTICKET",),
        champs=[
            ConversionChamp("DATE", "date_vente"), ConversionChamp("NUMEROTICKET", "numero_ticket"),
            ConversionChamp("CODECAISSIER", "caissier_code"), ConversionChamp("CODECLIENT", "client_code"),
            ConversionChamp("TOTALHT", "total_ht", "montant"), ConversionChamp("TOTALTVA", "total_tva", "montant"),
            ConversionChamp("TOTALTTC", "total_ttc", "montant"), ConversionChamp("MODEREGLEMENT", "mode_reglement"),
        ],
    ),
    MappingTable(
        source="CLIENTS",
        cible="client",
        cle_naturelle=("CODE",),
        obligatoire=True,
        champs=[
            ConversionChamp("CODE", "code"),
            ConversionChamp("NOM", "raison_sociale", "trim"),
            ConversionChamp("ADRESSE", "adresse", "trim"),
            ConversionChamp("CODEPOSTAL", "code_postal"),
            ConversionChamp("VILLE", "ville", "trim"),
            ConversionChamp("TEL", "telephone"),
            ConversionChamp("MATRICULE", "matricule_fiscal", "matricule_fiscal"),
            ConversionChamp("PLAFOND", "plafond_credit", "montant"),
        ],
    ),
    MappingTable(
        source="CLIENTCOMPL",
        cible="client_extras",
        cle_naturelle=("CODECLIENT",),
        dependances=("CLIENTS",),
        champs=[ConversionChamp("CODECLIENT", "client_code")],
    ),
    MappingTable(
        source="Produits",
        cible="produit",
        cle_naturelle=("REF",),
        obligatoire=True,
        dependances=("FAMILLE",),
        champs=[
            ConversionChamp("REF", "code"),
            ConversionChamp("DESIGNATION", "designation", "trim"),
            # trim : code-barres vide = pas de code-barres → NULL (UNIQUE OK)
            ConversionChamp("CODBAR", "code_barres", "trim"),
            ConversionChamp("PVTTTC", "prix_ttc", "montant"),
            ConversionChamp("PRIXACHAT", "prix_achat", "montant"),
            ConversionChamp("STOCK", "stock"),
            ConversionChamp("FAMILLE", "famille_code"),
            ConversionChamp("CODETVA", "taux_tva_code"),
        ],
    ),
    MappingTable(
        source="famille",
        cible="famille",
        cle_naturelle=("CODE",),
        champs=[ConversionChamp("CODE", "code"), ConversionChamp("LIBELLE", "libelle", "trim")],
    ),
    MappingTable(
        source="TARIF",
        cible="tarif",
        cle_naturelle=("CODEPROD", "CODETARIF"),
        dependances=("Produits",),
        champs=[
            ConversionChamp("CODEPROD", "produit_code"),
            ConversionChamp("CODETARIF", "grille_code"),
            ConversionChamp("PRIX", "prix", "montant"),
        ],
    ),
    MappingTable(
        source="VENDEUR",
        cible="utilisateur",
        cle_naturelle=("CODE",),
        champs=[
            ConversionChamp("CODE", "code"),
            ConversionChamp("NOM", "nom", "trim"),
            ConversionChamp("CODEGROUPE", "groupe_code"),
        ],
    ),
    MappingTable(
        source="Facture",
        cible="facture",
        cle_naturelle=("NUMERO",),
        obligatoire=True,
        dependances=("CLIENTS",),
        champs=[
            ConversionChamp("NUMERO", "numero"),
            ConversionChamp("DATE", "date_facture"),
            ConversionChamp("CODECLI", "client_code"),
            ConversionChamp("TOTALHT", "total_ht", "montant"),
            ConversionChamp("TOTALTVA", "total_tva", "montant"),
            ConversionChamp("TOTALTTC", "total_ttc", "montant"),
            ConversionChamp("NETAPAYER", "net_a_payer", "montant"),
        ],
    ),
    MappingTable(
        source="MOUVEMENTS_STOCK",
        cible="mouvement_stock",
        cle_naturelle=("BACLEUNIK",),
        dependances=("Produits",),
        champs=[
            ConversionChamp("BACLEUNIK", "id_source"),
            ConversionChamp("DATE", "date_mouvement"),
            ConversionChamp("CODEPROD", "produit_code"),
            ConversionChamp("QTE", "quantite"),
            ConversionChamp("SENS", "sens"),
        ],
    ),
    MappingTable(
        source="TVA",
        cible="taux_tva",
        cle_naturelle=("CODE",),
        champs=[
            ConversionChamp("CODE", "code"),
            ConversionChamp("TAUX", "taux", "montant"),
            ConversionChamp("LIBELLE", "libelle"),
        ],
    ),
]}

# Tables d'archives journalières (HISTO/) migrées vers des partitions dédiées.
SCHEMAS_ARCHIVE = ("bases_j", "histo_j", "reglem_j")


def mapping_pour(table_source: str) -> MappingTable | None:
    """Renvoie le mapping d'une table source (nom insensible à la casse)."""
    return MAPPINGS.get(table_source.upper())


def tables_migrables() -> list[str]:
    """Noms des tables source migrables, ordonnées par dépendances."""
    ordre: list[str] = []
    vues: set[str] = set()

    def visite(nom: str) -> None:
        nom = nom.upper()
        if nom in vues:
            return
        m = MAPPINGS.get(nom)
        if m is None:
            return
        for dep in m.dependances:
            visite(dep)
        if nom not in vues:
            vues.add(nom)
            ordre.append(nom)

    for nom in MAPPINGS:
        visite(nom)
    return ordre
