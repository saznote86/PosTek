# Guide de migration Atoo Leo2 → POSTEK (pour les clients)
Version 1.0 — à conserver à portée de main pendant la migration.

> ⏱️ Durée habituelle : 30 à 60 minutes pour un dossier standard.
> 👥 Pour qui : gérant ou responsable informatique, **aucune compétence
> technique requise**. Le reste est fait par l'assistant POSTEK.

---

## Avant de commencer — checklist

- [ ] Le PC d'origine **démarre toujours Atoo Leo2** (il sert à préparer l'export).
- [ ] Espace disque libre : environ **2× la taille** du dossier Leo2.
- [ ] Vous savez où se trouve le dossier Leo2 (ex. `X:\ATOO_LEO2_NACEF`).
- [ ] La caisse est fermée : personne ne fait de vente pendant la migration.
- [ ] Avez-vous votre **matricule fiscal** sous la main ? (il sera demandé)

## Étape 1 — Sauvegarde (obligatoire)
1. Fermez Atoo Leo2 sur tous les postes.
2. Copiez le dossier Leo2 complet sur une clé USB **ou** dans
   `Documents\sauvegarde_leo2_AVANT_MIGRATION`.
3. Vérifiez que la copie contient bien des fichiers `.FIC` (clients, produits…).

> Cette copie est votre roue de secours : on ne touche jamais à l'original.

## Étape 2 — Export des données depuis Leo2
Lancez Atoo Leo2 sur le poste d'origine et faites, table par table
(clients, produits, familles, tarifs, factures, TVA) :
`Menu → Édition → Exporter → Fichier (XLS/CSV)`.

Regroupez les fichiers exportés dans un dossier `export_leo2\tables\`
au format CSV (séparateur `;`). Si un module d'export global est proposé,
utilisez-le : il crée aussi le fichier `manifest.json` attendu par POSTEK.

> Pas possible d'exporter une table ? Notez-la : votre revendeur POSTEK
> dispose de l'outil d'extraction adapté (analyse WinDev d'origine).

## Étape 3 — Import dans POSTEK
1. Ouvrez POSTEK → `Menu → Outils → Importer depuis Atoo Leo2`.
2. Choisissez le dossier `export_leo2` → l'assistant analyse et affiche
   le nombre de lignes par table.
3. Cochez ce que vous voulez migrer (tout, par défaut).
4. Cliquez **Lancer** — POSTEK :
   - sauvegarde sa propre base automatiquement (rollback possible),
   - importe les données avec progression visible,
   - vérifie : comptages, doublons, liens clients ↔ factures ↔ produits.
5. À la fin : un **rapport** s'affiche (et s'imprime). Conservez-le.

## Étape 4 — Contrôles conseillés
- Le **dernier Z** s'affiche dans POSTEK avec le même numéro qu'en fin de journée Leo2.
- Total de TVA du mois dernier identique entre Leo2 et POSTEK.
- Quelques clients et produits choisis au hasard : fiches complètes.
- Les clients « sans matricule fiscal » listés dans le rapport : à compléter
  (obligatoire pour la conformité tunisienne).

## Étape 5 — Après la migration
- Gardez le dossier Leo2 et la sauvegarde **10 ans** (obligation légale).
- POSTEK n'efface rien : en cas de doute, votre revendeur ré-exécute l'import.
- Les mots de passe des vendeurs sont **réinitialisés** (les anciens étaient
  stockés de façon réversible dans Leo2 — pour votre sécurité, ils ne sont
  jamais recopiés). Redéfinissez-les dès le premier démarrage.

---

## Questions fréquentes

**Mes factures de 2001 à 2025 seront-elles toutes migrées ?**
Oui : les archives journalières (`HISTO/`) sont migrées ou archivées à
l'identique, table par table, avec leur numéro de Z.

**Puis-je migrer seulement clients + produits d'abord ?**
Oui : l'assistant permet une migration partielle, relançable quand vous
voulez (jamais de doublons).

**L'import s'est arrêté à 80 %. Qu'est-ce que je perds ?**
Rien. POSTEK a sauvegardé sa base avant de commencer et revient à l'état
d'avant : lisez le rapport, corrigez le fichier incriminé (souvent un CSV
modifié à la main) et relancez.

**POSTEK fonctionnera-t-il sans Internet ?**
Oui. L'activation peut être faite par fichier de licence sur clé USB.
Une caisse sans réseau continue de vendre (fenêtre de grâce de 7 jours).

**Combien de postes ?**
Votre licence indique le nombre de postes. Chaque poste est activé une
fois ; votre revendeur peut ajouter un poste plus tard.
