# Migration offline Leo2 vers POSTEK

## Principe

La migration ne nécessite aucune connexion réseau. Depuis le Back-Office administrateur, ouvrez **Importer depuis Leo2**. La fenêtre recherche un dossier local contenant des fichiers `.FIC`, `.CSV`, `.XLS`, `.XLSX` ou un répertoire `export_leo2`.

Les imports natifs sont prioritaires :

1. CSV ou XLS/XLSX disponibles localement ;
2. exports normalisés `export_leo2/tables/*.csv` avec `manifest.json` facultatif ;
3. assistant guidé si seuls les fichiers `.FIC` chiffrés sont présents.

## Entités

Le moteur crée ou met à jour de façon idempotente les tables suivantes :

- produits et familles ;
- clients ;
- fournisseurs ;
- utilisateurs migrés, avec réinitialisation obligatoire du mot de passe ;
- mouvements ;
- ventes et factures.

Les montants sont normalisés avec trois décimales. Un mouvement qui référence un produit absent est rejeté et apparaît dans le rapport.

## Sécurité et reprise

Avant une migration sur une base existante, une copie `*_backup_YYYYMMDD_HHMMSS.db` est créée. Une exception fatale restaure automatiquement cette copie. Les rapports sont écrits dans `rapport_migration.json` à côté de la base cible.

Les mots de passe provenant d'un export ne sont jamais importés. La colonne `mot_de_passe_a_reinitialiser` est activée pour les utilisateurs migrés.

## Fichiers `.FIC`

Les `.FIC` WinDev sont détectés comme preuve de présence du dossier source, mais leur lecture directe exige le moteur/DLL WinDev correspondant. Sans cette dépendance locale, utilisez dans Leo2 : **Outils → Copie des données → Exporter vers XLS**, puis relancez l'import sur le dossier exporté. Aucune DLL ou donnée propriétaire n'est téléchargée par POSTEK.

## Tests

La suite Python couvre la détection de dossier, l'encodage, les imports CSV, le rejet de références incohérentes et le rollback. Depuis la racine :

```powershell
python -m unittest discover -s postek\tests -v
dotnet test .\postek\desktop\Postek.Caisse.Tests\Postek.Caisse.Tests.csproj --no-restore
```
