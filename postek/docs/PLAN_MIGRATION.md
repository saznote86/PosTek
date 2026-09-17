# PLAN DE MIGRATION — Atoo Leo2 → POSTEK
Version 1.0 — 17/09/2026

---

## 1. État des lieux des formats

| Format | Rôle | Lisibilité constatée |
|---|---|---|
| `.FIC` | Tables HFSQL (données) | **Chiffrés** : signature `PCS` + GUID d'analyse `7D4822FF-…` (cf. `LEO2.REP → ANALYSISGUID`). Illisibles hors environnement d'origine. |
| `.NDX` | Index B-tree des `.FIC` | Non exploitables directement (dérivent des `.FIC`) |
| `.mmo` | Mémos binaires (images, textes longs, PDF) | Liés aux `.FIC` |
| `.wdd` | Analyse binaire WinDev | Chiffré |
| **`.xdd`** | **Analyse WinDev en XML** | ✅ **Lisible** : 101 tables / 1 115 rubriques extraites |
| `.REP` | Localisation des fichiers + GUID d'analyse | ✅ Lisible |
| `.XLS`, `.ini`, `.wde` | Exports, config, états | ✅ Lisibles |

**Découverte clé** : `MINICASH.xdd` donne le **schéma complet** (noms de tables,
rubriques, types, clés). Les codes de types sont partiellement décodés
(`38` = entier auto/clé, `2` = texte) ; les autres (`5`, `17`, `36`…) seront
confirmés en phase 2 par lecture d'échantillons réels.

## 2. Stratégie d'extraction (canaux légitimes uniquement)

> Les `.FIC` étant chiffrés par l'analyse/machine, POSTEK **ne les déchiffre pas**.
> La migration s'appuie sur les capacités d'export de l'environnement existant :

1. **Exports natifs de Leo2** — l'application embarque déjà des exports
   (cf. `produits.XLS`, `EXPORT_CPT.ini`, éditions vers fichiers). Canal préféré :
   refaire tourner Leo2 sur la machine d'origine et exporter les tables principales.
2. **Outils PCSoft** — WDMAP/WDLog ou un petit programme WinDev recompilé avec
   l'analyse d'origine (`MINICASH.wdd`) : ouverture des `.FIC` avec le framework
   d'origine (licencieusement autorisé chez le client possédant Leo2) puis export
   CSV/JSON/XML par table.
3. **Connecteur ODBC HFSQL** (si présent chez le client) — lecture SQL des tables
   et transfert direct.

Dans tous les cas, la sortie est un **répertoire d'exports normalisé** :

```
export_leo2/
├── manifest.json          # métadonnées : date, source, versions, checksums
└── tables/
    ├── CLIENTS.csv        # 1 CSV par table, UTF-8, séparateur ';' décimal '.'
    ├── produits.csv
    ├── facture_caisse.csv
    └── ...
```

## 3. Mapping ancien → nouveau (extrait ; table complète dans `schema_leo2.json`)

| Leo2 (.FIC) | POSTEK (table) | Notes |
|---|---|---|
| `CLIENTS` | `client` | + `matricule_fiscal` validé (format 7ch/lettre/lettre/3-4ch) |
| `CLIENTCOMPL` | fusion dans `client` (JSON `extras`) | champs complémentaires |
| `Produits` | `produit` | + `tva_id` obligatoire (défaut 19 %) |
| `PRODVAR`, `VARIANTES` | `produit_variante` | |
| `famille`, `SousGroupe`, `GROUPE` | `famille` (hiérarchie parent_id) | |
| `TARIF`, `QTEPROD` | `tarif`, `tarif_ligne` | |
| `Facture`, `facture_caisse` | `facture` | numérotation continue recalculée + contrôle |
| `lignefac_caisse` | `facture_ligne` | TVA recalculée et réconciliée |
| `CdeCli_caisse`, `ligneCdeCli_caisse` | `vente_caisse`, `vente_caisse_ligne` | |
| `reglement_caisse`, `LigneReg_caisse`, `TABLE REGLEMENT` | `reglement`, `moyen_reglement` | |
| `Mouvements` (+ `.mmo`) | `mouvement_stock` (+ pièce jointe table `piece_jointe`) | |
| `TVA`, `TVA_PENSION`, `MENU2TVA.INI` | `taux_tva` (table datée) | 19/13/7/0 % |
| `comptes`, `ventilation` | `compte`, `ecriture` (journal chaîné) | plan SCT |
| `VENDEUR`, `secu_mdp`, `VERROUS` | `utilisateur`, `role` | **mots de passe réinitialisés** (jamais migrés : ils étaient stockés de façon réversible) |
| `numeroz`, `Dern_Z` | `cloture_z` | dernier Z n° 519 (17/04/2024) → compteur repris |
| `HISTO/bases_j*`, `histo_j*`, `reglem_j*` | schémas d'archive identiques, partitions par date | 10 ans |
| `.mmo` (mémos) | `piece_jointe` (blobs) | re-associés par clé |

## 4. Moteur de migration livré (`postek/migration/`)

- `leo2_schema.py` : mapping déclaratif table→table + types de conversion.
- `export_reader.py` : lecteur de répertoire d'exports (CSV/manifest, contrôle de
  somme, détection d'encodage, mode ODBC stub pour extension).
- `engine.py` : orchestration — 5 étapes (analyse → sauvegarde → import → validation → rapport),
  transaction par table, reprise sur erreur, journalisation, **rollback** (restauration du backup).
- API volontairement enveloppable par n'importe quelle UI (PySide6, WPF, CLI).

## 5. Procédure type chez le client (checklist)

1. **Pré-requis** : Leo2 fonctionnel sur le poste d'origine ; espace disque ≈ 2× taille du dossier ; POSTEK installé.
2. **Sauvegarde complète** du dossier Leo2 (copie + zip horodaté).
3. Exports des tables (canaux §2) dans `export_leo2/tables/`.
4. Lancer l'assistant POSTEK « Import depuis Atoo Leo2 » :
   - sélection du dossier → analyse (compte de lignes, checksums, tables manquantes) ;
   - choix des données à migrer (tout ou partie) ;
   - **backup auto** de la base POSTEK avant import ;
   - import transactionnel avec progression ;
   - **validation** : comptages, sommes de contrôle par table, intégrité référentielle
     (chaque FK résolue), recalcul et rapprochement TVA par période ;
   - **rapport** imprimable (succès/erreurs/lignes par table) ;
   - en cas d'erreur : rollback automatique et rapport d'anomalies.
5. **Recette utilisateur** : dernier Z rapproché, ticket de contrôle, éditions, total TVA du mois.
6. Conservation du dossier Leo2 **au moins 10 ans** (archivage légal).

## 6. Validation & intégrité (exigences)
- Aucune perte : contrôle `count(source) == count(cible)` par table + sommes de contrôle sur champs numériques.
- Doublons : détection par clés naturelles (code client, code-barres produit, n° facture).
- Referential : Post-import FK audit (moteur livré dans `engine.py`).
- Rollback : restoration du snapshot SQLite (fichier) ou transaction SQL unique.
- Rangement : la migration est **idempotente** (relance sans doublon grâce aux clés naturelles).
