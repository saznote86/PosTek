# POSTEK

Refonte open du logiciel de caisse & gestion « Atoo Leo2 » (WinDev 14),
orientée conformité fiscale tunisienne (NACEF) et licence moderne.

> Ce dépôt contient l'**analyse de l'existant**, le **cahier des charges**,
> le **noyau métier exécutable** (licence, fiscal, moteur de migration), leurs
> **tests** et l'**application caisse desktop** (.NET/WPF, `desktop/`),
> avec son moteur de migration et ses tests — voir `docs/CAHIER_DES_CHARGES.md` §4.

## Démarrage rapide

```bash
# Tests (stdlib uniquement, aucune dépendance)
python -m unittest discover -s tests -v

# Extraire le schéma de l'analyse WinDev d'origine
python tools/parse_xdd.py ../MINICASH.xdd docs/schema_leo2.json docs/DICTIONNAIRE_DONNEES_LEO2.md

# Empreinte matérielle du poste
python -m licence.keygen empreinte
```

## Structure

```
postek/
├── docs/
│   ├── CAHIER_DES_CHARGES.md      # Analyse existant + spécifications + planning
│   ├── PLAN_MIGRATION.md          # Formats, mapping, procédure, validation
│   ├── MODULE_LICENCE.md          # Spé du système de licence
│   ├── GUIDE_MIGRATION_CLIENT.md  # Guide non-technique
│   ├── GUIDE_ENCAISSEMENT.md      # Doc utilisateur : du ticket au rapport Z
│   ├── GUIDE_TESTS_ECRAN.md       # Tests écran UIA : F2, clavier, mixte
│   ├── schema_leo2.json           # 101 tables / 1115 rubriques extraites
│   └── DICTIONNAIRE_DONNEES_LEO2.md
├── tools/parse_xdd.py             # Extracteur d'analyse WinDev (.xdd)
├── tools/test_*_ui.py             # Tests écran UIA (F2, clavier paiement, mixte)
│                                  #   — voir docs/GUIDE_TESTS_ECRAN.md
├── desktop/                       # Caisse tactile .NET 8 / WPF + tests xUnit
├── licence/                       # Ed25519, empreinte CPU+carte mère, jetons, keygen
├── fiscal/                        # TVA/RS/timbre, journal chaîné SHA-256, balance
├── migration/                     # Lecteur d'exports, mapping, moteur, rapport
└── tests/                         # 33 tests unitaires (unittest)
```

## Licences (module)
- Éditeur : `python -m licence.keygen generer --sortie ./cles`
- Client offline : `python -m licence.keygen offline --cles ./cles --societe "X" --postes 2 --empreinte <empreinte-client> --emis-le 2026-09-17`
- Voir `docs/MODULE_LICENCE.md`.

## Migration (module)
Voir `docs/PLAN_MIGRATION.md` (procédure) et `docs/GUIDE_MIGRATION_CLIENT.md`
(version non technique). Le moteur (`migration/engine.py`) consomme un
répertoire d'exports CSV+manifest et produit une base SQLite + rapport JSON,
avec backup et rollback.

## Sécurité & conformité
- Montants en `Decimal` (jamais de flottant), arrondi au millime.
- Journal comptable chaîné SHA-256 : toute altération est détectée
  (`JournalComptable.verifier_chaine()`).
- Mots de passe : jamais migrés depuis Leo2 (stockés réversibles à l'origine).
- Les `.FIC` d'origine étant chiffrés, la migration passe par des exports
  légitimes — aucune tentative de contournement de la protection d'origine.
