# Guide des tests écran POSTEK

Les tests écran utilisent UI Automation Windows via `comtypes`. Ils ne simulent
pas de clic souris : les boutons sont déclenchés par `InvokePattern` et les
champs par les patterns UIA disponibles.

## Pré-requis

- Windows avec .NET 8 et Python 3.x.
- Installer le pilote UIA : `pip install comtypes`.
- Construire l'application en Debug ou Release.
- Pour une base neuve, le premier compte est `admin`. Le mot de passe du test
  admin est lu dans `POSTEK_ADMIN_PASSWORD` et vaut `1234` par défaut.
- `POSTEK_DEMO_DB` peut pointer vers une base de test dédiée.
- `POSTEK_ATTENTE_APP` règle le délai d'attente des fenêtres, 30 secondes par
  défaut.

## Tests de caisse

Depuis `postek/`, avec l'application lancée :

```powershell
python tools/test_f2_ui.py
python tools/test_mix_ui.py
python tools/test_clavier_ui.py
```

Les scripts attendent aussi les fenêtres non marquées `WS_VISIBLE`, ce qui
permet leur exécution dans une session GitHub Actions non interactive.

## Tests Admin

Le scénario admin couvre le login, l'accueil, la gestion, les outils, les
modales de réindexation et de mots de passe, puis les périphériques :

```powershell
$env:POSTEK_ADMIN_USER = "admin"
$env:POSTEK_ADMIN_PASSWORD = "1234"
python tools/test_admin_ui.py
```

Les six vérifications sont exécutées dans cet ordre :

1. Login administrateur et ouverture de `AccueilWindow`.
2. Navigation `Gestion` puis retour.
3. Navigation `Outils` et présence de `Réindexer`, `Mots de passe` et `RAZ`.
4. Ouverture/fermeture de `ReindexerModal`.
5. Ouverture de `MotsDePasseModal`, vérification des onglets et de `CLÉ USB`.
6. Ouverture de `BackOffice`, navigation vers `Périphériques` et retour.

Le script réutilise les helpers UIA de `test_helpers.py`, journalise chaque étape
et liste les fenêtres détectées en cas d'échec. Il peut être lancé sur une
application déjà authentifiée ou prendre en charge le `LoginWindow` présent.

## CI

Le job `tests-ecran` construit l'application, installe `comtypes`, lance
l'exécutable puis exécute les quatre pilotes dans l'ordre : F2, mixte, clavier,
admin. Le job dispose d'un timeout global de 10 minutes et publie les artefacts
`imprimante.logique` et `postek_demo.db` en cas d'échec.
