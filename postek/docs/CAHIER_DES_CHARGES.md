# CAHIER DES CHARGES — POSTEK
**Refonte de l'application de caisse / gestion « Atoo Leo2 » (v14 WinDev)**
Version 1.0 — 17/09/2026 — Statut : draft

---

## 1. ANALYSE DE L'EXISTANT (constaté sur le dossier fourni)

### 1.1 Nature du dossier
Le dossier est un **dossier de déploiement WinDev 14**, pas un dossier de sources :

| Élément | Rôle |
|---|---|
| `LEO2.exe`, `hd_serveur.exe`, `OUTIL_MINICASH.exe`, `SPOOLER.exe` | Exécutables (caisse, serveur HFSQL, mini-caisse, spouleur d'impression) |
| `WD140*.DLL` (30 DLL) | Runtime **WinDev 14** (HF, ÉTAT, PDF, SQL, ZIP, OLE, RTF, XLS, XML…) |
| `atoo_ig.WDL`, `atoo_ig_048.WDL`, `LeoPesage10.WDK` | Bibliothèques compilées (UI + module de pesage) |
| `MINICASH.wdd` / `MINICASH.xdd` | Analyse HFSQL (binaire chiffré) / **analyse XML lisible** |
| `*.FIC` / `*.NDX` / `*.mmo` | Données HFSQL : tables / index / mémos binaires |
| `*.REP` | Registre de localisation des fichiers HFSQL (`X:\ATOO_LEO2_NACEF\`) |
| `licence.FIC`, `info_lic_leo2` | Verrou de licence (**numéro de série HDD** : `hdd.ini → HDD=1028`) |
| `BMP/` (367 fichiers), `SONS LEO2/`, `POLICES/`, `SEB_PATERN/`, `PLAN_OBJETS/` | Ressources UI propriétaires |
| `EDITIONS_PERSO/`, `*.wde` | États personnalisés (tickets, facturettes, étiquettes codes-barres) |
| `HISTO/` | Archives journalières (`bases_j`, `histo_j`, `reglem_j` — 2001→2025) |

### 1.2 Données HFSQL — point technique majeur
- Les `.FIC` portent la signature `PCS` + un **GUID d'analyse** (`7D4822FF-…` identique
  à `LEO2.REP → ANALYSISGUID`) : les fichiers sont **chiffrés/liés à l'analyse et à la
  machine**. Lecture directe impossible hors environnement d'origine.
- **En revanche, `MINICASH.xdd` est lisible** (XML) : il décrit **101 tables et
  1 115 rubriques** (champs, types, clés). C'est la source de vérité du schéma.
- Extraction automatisée : `postek/tools/parse_xdd.py` →
  `docs/schema_leo2.json` + `docs/DICTIONNAIRE_DONNEES_LEO2.md`.

### 1.3 Fonctionnalités détectées (par analyse des tables et fichiers)
- **Caisse / point de vente** : `CdeCli_caisse`, `ligneCdeCli_caisse`, `lignefac_caisse`,
  `reglement_caisse`, `LigneReg_caisse`, `Sel_Règlement`, `TABLE REGLEMENT`,
  `CartePayée`, claviers virtuels (`clavier*.FIC`, `touches.FIC`), gestion multi-postes
  (`LEO.INI` liste les postes réseau), clôture **Z** (`numeroz`, `Dern_Z` : dernier Z n°519).
- **Catalogue produits** : `Produits` (24 rubriques), `PRODVAR` (variantes), `famille`,
  `SousGroupe`, `GROUPE`, `VARIANTES`, `TARIF`, `QTEPROD`, codes-barres, étiquettes.
- **Clients** : `CLIENTS` (25 rubriques), `CLIENTCOMPL`, `HISTOCLI`, codes postaux.
- **Facturation** : `Facture`, `facture_caisse` (+ mémos), TVA (`TVA`, `TVA_PENSION`,
  `MENU2TVA.INI` — TVA par famille MENU/ALCOOL).
- **Comptabilité / ventilation** : `comptes`, `ventilation`, `TABLES BASES`, `Unigest`,
  export comptable (`EXPORT_CPT.ini`).
- **Vendeurs / sécurité** : `VENDEUR`, `secu_mdp`, `VERROUS`, `GROUPE`, `messages`.
- **Secteur boulangerie / production** : `ListeProduction`, `SEB_*` (SEB_PRODUIT,
  SEB_PLAGE, SEB_ARTICLE), pesage (`LeoPesage10.WDK`).
- **Secteur hôtelier** : `HTLARTICLE`, `HTLCHAMBRE`, `HTLRESA`, `RESA` (réservations,
  agenda, plans de salle `PLAN_SALLES`/`PLAN_TABLE`).
- **Impression** : tickets 80 mm, facturettes A5, étiquettes (7 formats `.wde`),
  spouleur, éditions personnalisées.
- **Multi-établissement** : `POINTVENTE`, `PORTABLE`, transferts (`transfert/`).

**Constat** : `README_NACEF.md`, `MIGRATION_CHECKLIST.md`, `NACEF_MAPPING.md` et les
dossiers `DOCUMENTATION/`, `MIGRATION_NACEF/` sont **vides** — la conformité NACEF est
une intention (nom du dossier `X:\ATOO_LEO2_NACEF`) mais rien n'est documenté.

### 1.4 Limites de l'existant
1. Verrou de licence **numéro de série HDD** (fragile, bloque les changements de disque).
2. Données chiffrées par analyse → **export client difficile**.
3. Aucune inaltérabilité des écritures, aucune numérotation fiscale garantie.
4. Interface figée (ressources BMP propriétaires), pas de thèmes.
5. Matériel de caisse piloté par DLL WinDev propriétaires.

---

## 2. OBJECTIFS DU PROJET POSTEK

| # | Objectif | Priorité |
|---|---|---|
| O1 | Rebranding complet « POSTEK », interface à thèmes (couleurs, logo, polices) | Haute |
| O2 | Nouveau système de licence moderne (cloud + offline, empreinte CPU+carte mère, multi-postes) | **Critique** |
| O3 | Conformité tunisienne : TVA 19/13/7/0 %, RS, timbre fiscal 1 DT, journal/grand-livre/balance/bilan, inaltérabilité, archivage 10 ans | **Critique** |
| O4 | Conserver 100 % des fonctionnalités métier identifiées en §1.3 | Haute |
| O5 | **Module de migration sans perte depuis Atoo Leo2** (assistant non-technique) | **Critique** |
| O6 | Recréer (non copier) les assets : icônes, sons, polices (les BMP/sons/polices d'origine restent la propriété de leur auteur) | Haute |

> **Hors périmètre assumé** : contourner, retirer ou « cracker » la licence de
> l'application Atoo Leo2. POSTEK est une réécriture originale : son propre
> système de licence, ses propres ressources, et la migration des **données
> métier** du client par les canaux d'export légitimes (§5).

---

## 3. SPÉCIFICATIONS FONCTIONNELLES

### 3.1 Caisse (écran tactile)
- Vente rapide par clavier virtuel configurable (équivalent `clavier.FIC`/`touches.FIC`),
  code-barres, familles/sous-groupes, recherche, pesage (réutilisation mat. compatible OPOS/ESC-POS),
  remises, avoirs (`PARAM_AVOIR`), cartes payées, multi-règlements (espèces/CB/chèque/ticket),
  rendu monnaie, ticket 80 mm ESC/POS + ticket graphique, tiroir-caisse, son de validation.
- Multi-caisses / multi-points de vente, serveurs et vendeurs, gestion des états de table
  (plans de salle interactifs), transfert de table, split.
- Clôture Z quotidienne (numérotation continue, archivage `HISTO/` équivalent).

### 3.2 Facturation & fiscalité (NACEF)
- Facture / facturette / avoir, **numérotation continue sans trou** (compteur par série,
  stocké et contrôlé, trous impossibles : la facture annulée est une facture à 0 liée).
- TVA par article et par famille (19 / 13 / 7 / 0 %, anti-chronologie des taux garantie par table de taux datée),
  **retenue à la source** paramétrable, **timbre fiscal 1,000 DT** (règles d'exemption configurables),
  mentions obligatoires (MF, TVA, RC, adresse…), export **TVA journalière/mensuelle**.
- **Comptabilité** : plan comptable SCT, journal comptable, grand livre, balance,
  bilan + état de résultat (liasse simplifiée exportable), ventilation automatique des ventes.
- **Inaltérabilité** : chaque écriture encaissée est chaînée par hachage SHA-256
  (voir `postek/fiscal/`), export d'archive horodaté, rétention 10 ans.

### 3.3 Clients / fournisseurs
- Fiche client étendue avec **MATRICULE FISCALE** (format contrôlé : ex. `1234567/A/M/000`),
  plafond, tarif dédié, historique, cartes de fidélité (`CartePayée`).
- Fournisseurs + matricule fiscal, achats simplifiés.

### 3.4 Stock / production
- Mouvements de stock, inventaires, alertes de seuil, production boulangerie
  (listes de production, plages horaires), pesage intégré.

### 3.5 Sécurité / utilisateurs
- Utilisateurs, groupes, droits par module et par action, mots de passe hachés
  (PBKDF2/argon2 — jamais en clair contrairement à l'existant), journal d'audit des actions sensibles.

### 3.6 Rebranding / thèmes
- Fichier de thème JSON (couleurs, logo, police, sons activables), aucun texte en dur
  « Atoo »/« Leo2 » dans POSTEK (recherche automatique en CI).

### 3.7 Migration (voir `docs/PLAN_MIGRATION.md`)
- Assistant graphique en 5 étapes, migration totale ou partielle, rapport détaillé,
  sauvegarde + rollback, validation d'intégrité référentielle.

---

## 4. SPÉCIFICATIONS TECHNIQUES

### 4.1 Options d'architecture (décision à prendre — voir questions)

| | **Option A — Python + PySide6 + SQLite** | **Option B — WinDev 2024 + HFSQL** | **Option C — C# .NET 8 WPF + SQLite** |
|---|---|---|---|
| Vitesse de dev | ⭐⭐⭐ | ⭐⭐ (coût licences PCSoft) | ⭐⭐ |
| Affinité POS Windows | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| Accès natif aux .FIC v14 | via exports/ODBC | **natif** | via exports/ODBC |
| Thèmes/UI moderne | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| Déploiement offline caisse | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| Pérennité / recrutement dev | ⭐⭐⭐ | ⭐ (compétence rare) | ⭐⭐⭐ |

**Recommandation** : Option **C** (ou A) ; le noyau métier livré dans ce dépôt
(licence, fiscal, moteur de migration en Python) sert de référence exécutable et
de base portable. L'accès aux `.FIC` chiffrés se fait **par export légitime**
(jamais par déchiffrement non autorisé) — voir §5.

### 4.2 Base de données cible (schéma POSTEK)
- SQLite en mono-poste / PostgreSQL en multi-postes ; schéma dérivé de
  `docs/schema_leo2.json` avec renommage métier : `client`, `produit`, `famille`,
  `facture`, `facture_ligne`, `reglement`, `vente_caisse`, `vente_caisse_ligne`,
  `cloture_z`, `compte`, `ecriture`, `utilisateur`, `licence`…
- Conventions : tables snake_case singulier, PK `id` entier auto, FK `*_id`,
  montants en `NUMERIC(14,3)` (millimes), dates ISO-8601 UTC, jamais de virgule flottante
  pour l'argent.

### 4.3 Arborescence livrée dans ce dépôt

```
postek/
├── README.md                    # Entrée du projet
├── docs/
│   ├── CAHIER_DES_CHARGES.md    # Ce document
│   ├── PLAN_MIGRATION.md        # Migration détaillée
│   ├── MODULE_LICENCE.md        # Spé du système de licence
│   ├── GUIDE_MIGRATION_CLIENT.md# Guide non-technique
│   ├── schema_leo2.json         # Schéma extrait de MINICASH.xdd
│   └── DICTIONNAIRE_DONNEES_LEO2.md
├── tools/parse_xdd.py           # Extracteur d'analyse WinDev
├── licence/                     # Module licence POSTEK (code)
├── fiscal/                      # Noyau fiscal & comptable (code)
├── migration/                   # Moteur de migration (code)
└── tests/                       # Tests unitaires (unittest, stdlib)
```

### 4.4 Module licence — synthèse (détails : `docs/MODULE_LICENCE.md`)
- Clé = jeton signé **Ed25519** (vérification par clé publique embarquée),
  payload JSON : `societe`, `postes`, `expiration_date`, `options`.
- Empreinte matériel = SHA-256(CPU `ProcessorId` + carte mère `SerialNumber`/UUID via WMI).
- Activation en ligne (API REST) **ou** fichier de licence hors-ligne signé ;
  grâce de 7 jours en cas d'absence réseau ; révocation par mise à jour de CRL locale.
- Générateur de clés côté éditeur (`licence/keygen.py`) — la clé privée ne quitte jamais l'éditeur.

### 4.5 Planning indicatif (1–2 devs, à affiner)

| Phase | Contenu | Durée |
|---|---|---|
| P0 | Socle : BDD, noyau fiscal, licence, CI | 3 semaines |
| P1 | Caisse tactile + tickets + clôture Z | 6 semaines |
| P2 | Produits/stocks/clients/fournisseurs | 4 semaines |
| P3 | Facturation NACEF + comptabilité (journal, GL, balance, bilan) | 5 semaines |
| P4 | Module migration + reprise de données pilote | 3 semaines |
| P5 | Thèmes, impressions/étiquettes, hôtellerie/réservations | 5 semaines |
| P6 | Pilotes chez 2–3 clients, corrections | 4 semaines |

### 4.6 Risques

| Risque | Mitigation |
|---|---|
| `.FIC` illisibles hors machine d'origine (chiffrés) | Stratégie d'export légitime documentée §5 ; lecture par l'app d'origine/outil PCSoft chez le client |
| Types HFSQL non décodés (`code 5/17/36`…) | Phase 2 de `parse_xdd` : confirmation par lecture d'échantillons réels |
| Conformité fiscale (taux RS, timbre, mentions) | Paramétrage externalisé + validation par expert-comptable avant mise en prod |
| Matériel POS hétérogène | Couche d'abstraction imprimantes/tiroirs (ESC/POS + OPOS) |
| Assets propriétaires | Création originale, aucune réutilisation BMP/sons/polices |

---

## 5. STRATÉGIE D'ACCÈS AUX DONNÉES D'ORIGINE (résumé — détail §5 du plan de migration)

1. **Chez le client, sur la machine Leo2 fonctionnelle** : exports internes de Leo2
   (Excel/CSV — cf. `produits.XLS`, `EXPORT_CPT.ini`), impressions vers fichiers, sauvegardes HFSQL.
2. **Outil PCSoft** (WinDev/HFSQL avec l'analyse) capable d'ouvrir les `.FIC` de
   l'application : lecture/`HLitPremier` → export CSV/JSON/SQLite.
3. **Connecteur ODBC HFSQL** si disponible dans l'environnement client.
4. Le moteur POSTEK (`migration/`) consomme **un répertoire d'exports** ou une
   connexion ODBC — jamais le déchiffrement des `.FIC`.
