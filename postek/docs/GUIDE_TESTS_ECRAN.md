# Tests écran (UI Automation) — guide manuel

Les tests unitaires (63 .NET + 33 Python) valident le cœur métier hors écran.
Les scripts ci-dessous pilotent l'application **réelle** comme le ferait un
utilisateur (clics via UI Automation, aucun mouvement de souris) et vérifient
l'état à l'écran **et en base SQLite**.

## Pré-requis

1. Compiler et lancer l'app (depuis la racine du dépôt) :

   ```bat
   start.bat
   ```

   > ⚠️ Si l'app est lancée depuis un script qui se termine aussitôt (shell
   > non interactif, CI), le processus peut être tué à la fermeture du shell.
   > Dans le doute, vérifier : `tasklist /FI "IMAGENAME eq Postek.Caisse.exe"`
   > et voir la fenêtre « POSTEK — Caisse » à l'écran.

2. Installer le pilote UIA (une seule fois) :

   ```bash
   pip install comtypes
   ```

## Scripts (dans `postek/tools/`)

| Script | Ce qu'il vérifie | Effet de bord |
|---|---|---|
| `test_f2_ui.py` | F2 = encaisser : ignoré sur ticket vide, ouvre l'encaissement sur ticket plein (1,800), « Retour » referme sans rien changer | aucun |
| `test_clavier_ui.py` | Paiement : Échap annule (rien en base), Entrée ignorée si non soldé, Entrée valide si soldé (ticket + CB 1,800 en base) | 1 ticket démo encaissé |
| `test_mix_ui.py` | Encaissement mixte CB 1,000 + espèces 1,000 → rendu 0,200, règlements en base, flux ticket imprimé | 1 ticket démo encaissé |

Exécution (app lancée, depuis la racine du dépôt) :

```bash
python postek/tools/test_f2_ui.py
python postek/tools/test_clavier_ui.py
python postek/tools/test_mix_ui.py
```

Chaque script se termine par `[RÉSULTAT] … vérifications passées` (code 0)
ou un `[!]`/assert avec le détail de l'échec (code ≠ 0).

## Ce qui est vérifié, point par point

### F2 = encaisser (`test_f2_ui.py`)
1. F2 sur ticket **vide** : aucune fenêtre de paiement (garde anti-ticket-vide).
2. Café express + croissant → `Total : 1,800 TND` affiché.
3. F2 sur ticket plein → fenêtre « Encaissement » ouverte, à payer 1,800.
4. « ← Retour » → fenêtre fermée, ticket intact, aucun encaissement.

### Raccourcis paiement (`test_clavier_ui.py`)
1. **Échap** referme la fenêtre ; la saisie en cours (2,000 CB non validée)
   est abandonnée ; **aucun ticket en base** (compté avant/après).
2. **Entrée sur ticket non soldé** (1,000 réglé sur 1,800) : ignorée —
   la fenêtre reste ouverte, rien n'est encaissé.
3. **Entrée sur ticket soldé** (CB sans saisie = reste à payer) : valide —
   fenêtre fermée, +1 ticket en base, `reglement_ticket` = `("cb", "1.800")`.

La base est lue **en lecture seule** (`file:…?mode=ro`) pour ne jamais
interférer avec l'app ; un verrou momentané (app en écriture) est retenté.

### Encaissement mixte (`test_mix_ui.py`)
1. Ticket 1,800 → CB 1,000 puis espèces 1,000 (2,000 reçus).
2. Validation → rendu 0,200, règlements persistés (`cb` 1.000, `especes` 1.000),
   flux ticket dans `imprimante.logique` avec les lignes CB/ESPECES/RENDU.

## Intégration continue

Le job **« Tests écran UIA (app lancée) »** de `.github/workflows/ci.yml`
joue `test_f2_ui.py`, `test_mix_ui.py` et `test_clavier_ui.py` à chaque
push/PR sur un runner
`windows-latest` : build Release, lancement par PowerShell `Start-Process`
(le processus survit à la fin du pas, la fenêtre WPF reste visible pour
UIA), tests, puis arrêt de l'app (`if: always()`). Le job dépend de
`tests-dotnet` : il ne tourne pas si les tests unitaires cassent.

La base vérifiée par `test_clavier_ui.py` est résolue **depuis le processus
réellement lancé** (fenêtre POSTEK → PID → chemin de l'exe → `postek_demo.db`
voisin) : le même script marche en Debug comme en Release. Surcharge possible
par la variable d'environnement `POSTEK_DEMO_DB`.

## Dépannage

- **« Fenêtre principale POSTEK introuvable »** : l'app n'est pas lancée, ou
  elle vient de mourir (cf. avertissement ci-dessus) — relancer `start.bat`.
- **COMError 0x80040201** pendant un parcours d'arbre : l'UIA retente
  automatiquement (6 × 250 ms) ; si ça persiste, l'app était en train de
  changer d'écran — relancer le script.
- **Base verrouillée** : le script retente 5 × 400 ms ; au-delà, fermer un
  éventuel DB Browser ouvert sur `postek_demo.db`.
