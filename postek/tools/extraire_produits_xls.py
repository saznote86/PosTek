#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
extraire_produits_xls.py — Canal d'extraction réel n°1 (cf. PLAN_MIGRATION §2).

Convertit l'export XLS natif de Leo2 (`produits.XLS`, classeur 'A',
79 produits) vers le répertoire d'exports normalisé attendu par le
moteur de migration (`export_leo2/tables/*.csv` + `manifest.json`,
UTF-8, séparateur ';', point décimal).

Colonnes XLS Leo2 → CSV (noms attendus par leo2_schema.MAPPINGS) :
  famille → FAMILLE   (code famille, ex. 1..22)
  libelle → REF + DESIGNATION  (pas de code produit dans l'export :
              REF synthétique P001…P0NN dérivé de l'ordre, stable
              tant que l'export d'origine ne change pas)
  prix    → PVTTTC    (point décimal, 3 décimales — millimes)
  tva     → CODETVA   (code TVA Leo2 1..4, join à TVA.csv)
  cbarre  → CODBAR    (normalisé : chiffres seuls ; vide si absent ;
              doublons détectés et signalés — contrainte UNIQUE cible)
  compta  → COMPTA    (compte comptable, conservé en extras)

Produit une table PRODUITS.csv conforme au mapping, plus FAMILLES.csv
(codes rencontrés, libellés vides à compléter depuis l'export famille
du client — les .FIC étant chiffrés, les libellés réels viendront de
l'export natif famille). Le manifest porte les checksums SHA-256.

Usage :
    python postek/tools/extraire_produits_xls.py produits.XLS export_leo2
Nécessite : pip install xlrd  (XLS binaire, stdlib ne le lit pas)
"""

from __future__ import annotations

import hashlib
import json
import sys
import unicodedata
from pathlib import Path

# Codes TVA Leo2 observés dans l'export réel (1..4). La table TVA.FIC
# étant chiffrée, les taux par défaut NACEF sont posés ici ; ils seront
# recoupés avec l'export TVA du client dès disponible (PLAN_MIGRATION §3).
TAUX_TVA_DEFAUT = {"1": 0.190, "2": 0.130, "3": 0.070, "4": 0.0}


def normaliser_code_barres(brut: str) -> str:
    """Garde les chiffres seuls (l'export Leo2 padge à droite d'espaces)."""
    return "".join(ch for ch in brut if ch.isdigit())


def normaliser_entier(brut: str) -> str:
    """Normalise un code numérique lu par xlrd : '1.0' → '1' (sinon inchangé)."""
    brut = brut.strip()
    if brut.endswith(".0") and brut[:-2].isdigit():
        return brut[:-2]
    return brut


def sans_accents_maj(texte: str) -> str:
    """Base de référence synthétique (comparaisons, tri stable)."""
    decomp = unicodedata.normalize("NFD", texte)
    return "".join(ch for ch in decomp if not unicodedata.combining(ch)).upper()


def convertir(xls: Path) -> tuple[list[dict], list[dict], list[str]]:
    """Lit le classeur et renvoie (produits, familles, anomalies)."""
    import xlrd  # import paresseux : seul cet outil en a besoin

    classeur = xlrd.open_workbook(str(xls))
    feuille = classeur.sheet_by_index(0)
    entete = [str(feuille.cell_value(0, j)).strip().lower()
              for j in range(feuille.ncols)]
    attendu = ["famille", "libelle", "prix", "tva", "cbarre", "compta"]
    if entete != attendu:
        raise SystemExit(
            f"en-tête XLS inattendu : {entete} (attendu {attendu}) — "
            "ce fichier n'est pas un export produits Leo2 connu")

    produits: list[dict] = []
    familles: dict[str, int] = {}
    anomalies: list[str] = []
    vus_cbarre: dict[str, str] = {}
    vus_libelle: set[str] = set()

    for i in range(1, feuille.nrows):
        ligne = [feuille.cell_value(i, j) for j in range(feuille.ncols)]
        fam_code = normaliser_entier(str(ligne[0]))
        libelle = str(ligne[1]).strip()
        prix_brut = str(ligne[2]).strip().replace(",", ".")
        tva_code = normaliser_entier(str(ligne[3]))
        cbarre = normaliser_code_barres(str(ligne[4]))
        compta = str(ligne[5]).strip()

        if not libelle:
            anomalies.append(f"ligne {i + 1} : libellé vide, ignorée")
            continue
        if libelle.upper() in vus_libelle:
            anomalies.append(f"ligne {i + 1} : libellé dupliqué « {libelle} »")
        vus_libelle.add(libelle.upper())

        try:
            prix = f"{float(prix_brut):.3f}" if prix_brut else ""
        except ValueError:
            prix = ""
            anomalies.append(f"ligne {i + 1} : prix illisible {prix_brut!r}")
        if not prix:
            anomalies.append(f"ligne {i + 1} : « {libelle} » sans prix (à compléter)")
        if tva_code not in TAUX_TVA_DEFAUT:
            anomalies.append(f"ligne {i + 1} : code TVA inconnu {tva_code!r}")

        ref = f"P{i:03d}"   # stable : dérivée de la position dans l'export
        if cbarre:
            precedent = vus_cbarre.get(cbarre)
            if precedent is not None:
                anomalies.append(
                    f"ligne {i + 1} : code-barres {cbarre} déjà vu « {precedent} » "
                    "(contrainte UNIQUE cible — un seul sera importé)")
            else:
                vus_cbarre[cbarre] = libelle

        familles.setdefault(fam_code, 0)
        familles[fam_code] += 1
        produits.append({
            "REF": ref,
            "DESIGNATION": libelle,
            "CODBAR": cbarre,
            "PVTTTC": prix,
            "FAMILLE": fam_code,
            "CODETVA": tva_code,
            "COMPTA": compta,
        })

    # Les .FIC famille étant chiffrés, libellé provisoire (NOT NULL cible) :
    # sera écrasé par l'export famille natif du client (upsert par code).
    familles_csv = [
        {"CODE": c, "LIBELLE": f"Famille {c}"} for c in sorted(familles, key=int)
    ]
    return produits, familles_csv, anomalies


def ecrire_csv(chemin: Path, entete: list[str], lignes: list[dict]) -> str:
    """Écrit un CSV UTF-8 ';' AVEC ligne d'en-tête (attendue par ExportReader) ;
    renvoie le SHA-256 des octets écrits."""
    contenu = [";".join(entete)]
    contenu += [";".join(str(ligne[c]) for c in entete) for ligne in lignes]
    donnees = ("\n".join(contenu) + "\n").encode("utf-8")
    chemin.write_bytes(donnees)
    return hashlib.sha256(donnees).hexdigest()


def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(2)
    xls, dossier = Path(sys.argv[1]), Path(sys.argv[2])

    produits, familles, anomalies = convertir(xls)
    tables = dossier / "tables"
    tables.mkdir(parents=True, exist_ok=True)

    manifest = {"date_export": "", "source": str(xls), "tables": {}}
    for nom, entete, lignes in (
        ("PRODUITS", ["REF", "DESIGNATION", "CODBAR", "PVTTTC", "FAMILLE", "CODETVA", "COMPTA"], produits),
        ("FAMILLE", ["CODE", "LIBELLE"], familles),
    ):
        sha = ecrire_csv(tables / f"{nom}.csv", entete, lignes)
        manifest["tables"][nom] = {
            "fichier": f"{nom}.csv",
            "lignes": len(lignes),
            "sha256": sha,
        }

    manifest["date_export"] = __import__("datetime").date.today().isoformat()
    (dossier / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"[OK] {len(produits)} produits → {tables / 'PRODUITS.csv'}")
    print(f"[OK] {len(familles)} familles → {tables / 'FAMILLE.csv'} (libellés à compléter)")
    if anomalies:
        print(f"[!] {len(anomalies)} anomalie(s) :")
        for a in anomalies:
            print(f"     - {a}")


if __name__ == "__main__":
    main()
