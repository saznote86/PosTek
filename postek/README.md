# POSTEK

Refonte open du logiciel de caisse & gestion « Atoo Leo2 » (WinDev 14),
orientée conformité fiscale tunisienne (NACEF) et licence moderne.

> Le dépôt contient le **noyau métier exécutable** (licence, fiscal, moteur
> de migration), l'**application caisse desktop** (.NET/WPF) et leurs
> **tests**. Les documents d'analyse (cahier des charges, plans, schémas)
> sont conservés hors dépôt.

## Démarrage rapide

```bash
# Tests (stdlib uniquement, aucune dépendance)
python -m unittest discover -s tests -v

# Lancer la caisse (compile si besoin, anti-double-lancement)
./start.bat

# Empreinte matérielle du poste
python -m licence.keygen empreinte
```

## Structure

```
postek/
├── tools/parse_xdd.py             # Extracteur d'analyse WinDev (.xdd)
├── tools/test_*_ui.py             # Tests écran UIA (F2, paiement, mixte) — CI
├── desktop/                       # Caisse tactile .NET 8 / WPF + tests xUnit
├── licence/                       # Ed25519, empreinte CPU+carte mère, jetons, keygen
├── fiscal/                        # TVA/RS/timbre, journal chaîné SHA-256, balance
├── migration/                     # Lecteur d'exports, mapping, moteur, rapport
└── tests/                         # 33 tests unitaires (unittest)
```

## Tests écran (UIA)

`tools/test_f2_ui.py`, `test_mix_ui.py` et `test_clavier_ui.py` pilotent
l'application réelle (clics UI Automation, vérifications en base SQLite,
base démo résolue depuis le processus lancé). La CI exécute ces scripts
sur un runner Windows avec l'app lancée — voir `.github/workflows/ci.yml`,
job `tests-ecran`. Prérequis local : `pip install comtypes`, app lancée
via `start.bat`.

## Licences (module)

- Éditeur : `python -m licence.keygen generer --sortie ./cles`
- Client offline : `python -m licence.keygen offline --cles ./cles --societe "X" --postes 2 --empreinte <empreinte-client> --emis-le 2026-09-17`

## Migration (module)

Le moteur (`migration/engine.py`) consomme un répertoire d'exports
CSV+manifest et produit une base SQLite + rapport JSON, avec backup et
rollback. Extraction du schéma d'origine :

```bash
python tools/parse_xdd.py ../MINICASH.xdd schema_leo2.json dictionnaire.md
```

## Sécurité & conformité

- Montants en `Decimal` (jamais de flottant), arrondi au millime.
- Journal comptable chaîné SHA-256 : toute altération est détectée
  (`JournalComptable.verifier_chaine()`).
- Mots de passe : jamais migrés depuis Leo2 (stockés réversibles à l'origine).
- Les `.FIC` d'origine étant chiffrés, la migration passe par des exports
  légitimes — aucune tentative de contournement de la protection d'origine.
