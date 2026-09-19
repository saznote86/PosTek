# plan.md — Plan de travail POSTEK

> **Généré le 2026-09-19 à partir de `context.md`** (source de vérité unique).
> État de référence : suite .NET **114/114** verte, Python **41/41**, CI verte
> (run n°13), build WPF réussi. La mission « corrections critiques +
> Administration » est analysée mais **non encore implémentée**.

---

## Conventions à respecter sur tout le plan (non négociables)

- Argent : `decimal`, millimes (3 déc.), arrondi `AwayFromZero` — jamais de
  flottant, jamais `SUM()` SQLite pour l'argent.
- TVA stockée en fraction (0.190) ; ×100 uniquement à l'affichage/impression.
- Aucun texte « Atoo »/« Leo2 » dans le code app (CI bloquante) — grep de
  contrôle après chaque lot UI.
- Mot de passe jamais en clair ; audit des opérations sensibles.
- Rôles : Administrateur (Back-Office/Migration/Clôture), Caissier (vente).
- Thème global `Styles/PostekTheme.xaml`, contrôles tactiles réutilisables.
- Après chaque lot : `dotnet test` complet + ligne d'historique dans
  `context.md`.

---

## Phase 1 — Mission « Corrections critiques + Administration » (PRIORITÉ ACTIVE)

C'est la mission explicitement en attente dans `context.md`. Aucun patch
appliqué à ce jour — vérifications préalables déjà faites :
`BackOffice.xaml` contient encore « Importer depuis Leo2 », `OutilsViewModel`
affiche encore les identifiants initiaux, les ViewModels ne sont pas tous
abonnés à `DatabaseMaintenanceService.TablesVidees`.

### 1.1 Rebranding UI Atoo/Leo2
- Supprimer toutes les références UI `Atoo`/`Leo2` du dossier `desktop/`
  (textes visibles, titres, messages) ; conserver ces termes **uniquement**
  dans le code de migration isolé (`postek/migration/`).
- Contrôle : grep rebranding sur le dossier desktop → 0 occurrence.

### 1.2 Message neutre sans identifiants
- Remplacer tout affichage d'identifiants initiaux (`admin/admin`, `123456`)
  par un message neutre, sans mot de passe en clair (cible :
  `OutilsViewModel`, textes d'installation/RAZ).

### 1.3 Notification RAZ → invalidation des ViewModels
- `DatabaseMaintenanceService.TablesVidees` existe déjà et est déclenché par
  `ViderTablesMetier` (mais hors/après le bloc transactionnel et après
  réactivation des clés étrangères — garder ce comportement).
- Abonner les ViewModels **Produits, Clients, Vendeurs, Caisse** à
  l'événement : rechargement des listes, purge du ticket en cours côté caisse.
  - Abonnement via `Dispatcher` (thread UI) et **désabonnement dans
    `Dispose`** pour éviter fuites/notifications post-fermeture.
- Note : `CaisseViewModel.RechargerProduits()` et l'abonnement de
  `MainWindow` existent déjà (lot 2026-09-19) — généraliser aux autres VM.

### 1.4 Administration unifiée (fusion Gestion/Outils)
- Créer `AdministrationView` à **trois onglets** : Gestion, Outils, Paramètres.
- Créer `GestionUnifieeView` et `OutilsUnifieeView` (hébergées dans les
  onglets, réutilisant les vues/ViewModels existants sans dupliquer la
  logique métier).
- Raccorder le Back-Office : remplacer les deux accès séparés
  Gestion/Outils par l'entrée Administration unique.
- Adapter la fenêtre d'import (migration) à la nouvelle navigation.

### 1.5 Validation de la phase
- `dotnet test desktop/Postek.Caisse.Tests/Postec.Caisse.Tests.csproj` :
  114+ verts, 0 échec.
- Tests Python : 41/41 verts (inchangés).
- Grep rebranding : 0 hit UI sur `desktop/`.
- Tests manuels WPF : RAZ → les écrans Produits/Clients/Vendeurs/Caisse
  reflètent l'état vide sans redémarrage ; navigation Administration OK.

**Livrables** : `AdministrationView`, `GestionUnifieeView`,
`OutilsUnifieeView`, patch Back-Office + import, tests, grep rebranding.

---

## Phase 2 — Réactivation de l'écran de connexion (sécurité production)

- Repasser `ConnexionDesactiveeProvisoirement = false` dans `App.OnStartup`.
- Vérifier le parcours complet : login modal → Admin → `AccueilWindow` /
  Caissier → `MainWindow` → changement obligatoire de mot de passe
  (`DoitChangerMotDePasse=1`) → ouverture de caisse (fonds initial).
- Tests manuels à l'écran : ouvrir/fermer `LoginWindow` 5 fois (fuite
  mémoire), clavier tactile + physique, audit succès/échec.
- Recette du bouton Back-Office « Effacer l'historique des connexions »
  (branché mais non validé à l'écran, bloqué par le drapeau).

---

## Phase 3 — Écrans de Gestion : des stubs aux services réels

- **Fiche Produit** : déjà enrichie (Famille/TVA/Imprimante, jointures,
  routage `GrouperParImprimante`) — restent confirmation/audit complets et
  validation sur imprimantes physiques.
- Convertir les 9 vues de Gestion encore en stubs vers leurs services métier
  et schémas SQLite réels :
  Comptabilité, Historique des tickets, Ventilations/Tarifs, Claviers
  tactiles, Programmation avancée, Réglage date-heure (les ViewModels réels
  sont listés au TODO).
- Chaque conversion = CRUD SQLite + validation `decimal`/TVA fraction + audit
  + tests dédiés (voir Phase 4).
- Ordre suggéré : Historique tickets (lecture seule, simple) → Clôtures →
  Ventilations/Tarifs → Comptabilité → Claviers → Programmation avancée →
  Réglage date-heure.

---

## Phase 4 — Couverture de tests

### .NET (xUnit)
- Tests unitaires Clients/Vendeurs manquants ; objectif **≥ 15 tests**
  dédiés aux nouvelles vues (exigence utilisateur).
- Ajouter des tests ciblés sur `AdministrationView`/ViewModels unifiés
  (navigation onglets, commandes) — lot Phase 1.
- Nettoyage (optionnel) : avertissement xUnit2013 historique
  (`BaseDonneesTests.cs:272`).

### UIA (tests écran)
- Ajouter des scénarios UIA pour le **nouveau shell administrateur**
  (Accueil refondu, routage F1–F5) et le **clavier tactile LoginView**.
- Rejouer et valider **6/6** tests UIA Admin (l'échec historique concerne
  l'exposition `ContentControl` de `ParametresPeripheriquesView`).
- Rejouer les scénarios F2/mixte/clavier après la refonte de `MainWindow`
  (les noms de contrôles ont été conservés — à confirmer).

---

## Phase 5 — Migration des données Leo2

- Obtenir du client des **exports natifs complémentaires** : clients,
  fournisseurs, utilisateurs, mouvements, ventes, familles (les `.FIC`
  sont chiffrés ; seuls produits/familles provisoires ont été importés —
  76/78 produits, 2 rejets code-barres dupliqué).
- Rejouer la migration complète sur une copie réelle avec
  `xlrd`/`openpyxl` installés si XLS/XLSX présents.
- Remplacer les libellés « Famille N » provisoires par les libellés réels.
- Valider visuellement la base réelle migrée dans l'application
  (aucun utilisateur migré n'existe → prévoir la création de session).
- Rejouer `postek/tools/valider_migration.py` (références, orphelins).

---

## Phase 6 — Validation matérielle & terrain

- Vérifier l'affichage sur **1024×768** (fenêtre caisse sans bordure,
  accueil, login).
- Tester sur **imprimante thermique réelle** : ticket ESC/POS, rapport Z
  imprimé (le flux `imprimante.logique` est déjà validé en local).
- Confirmer le routage par imprimante (caisse/cuisine/bar) avec 2 produits
  affectés à des imprimantes distinctes.
- Remplacer la façade `SerialPortService` par l'intégration matérielle
  réelle (test de port, état tiroir) sur le protocole du poste client.
- Valider visuellement : fenêtre caisse sans bordure, raccourcis
  F2/F8/F9/F12, paiement multi-règlement, clôture Z.
- (Option accueil) Télécharger/créer les assets locaux plage/terminal —
  actuellement placeholder vectoriel.

---

## Phase 7 — CI & hygiène

- Envisager une **matrice Python** (3.12 + latest) une fois le noyau
  stabilisé ; lever/élaguer l'étape diagnostic CI devenue skippée (à garder
  sous le coude).
- Vérifier le job `tests-ecran` après les refontes UI (sélecteurs UIA).
- Ajouter au CI le contrôle grep rebranding des nouveaux fichiers UI si non
  couvert par la règle bloquante existante.
- Tenir `context.md` à jour après chaque lot (Historique récent + État
  actuel + TODO).

---

## Risques & points de vigilance

| Risque | Impact | Mitigation |
|---|---|---|
| Refontes UI récentes (Accueil/Caisse) vs tests UIA sélecteurs | Échec CI `tests-ecran` | Rejouer les scénarios après chaque refonte ; AutomationId stables |
| Abonnement `TablesVidees` non désabonné | Fuite / callback sur fenêtre fermée | `Dispose` idempotent systématique (modèle `LoginViewModel`) |
| Onglets Administration duplicant la logique Gestion/Outils | Régression double maintenance | Réutiliser ViewModels existants ; nouvelles vues = coquilles de composition |
| Base réelle sans utilisateur migré | Session d'ouverture impossible | Prévoir création admin/utilisateurs à la migration (PBKDF2) |
| Login désactivé oublie d'être réactivé | Faille de sécurité à la livraison | Phase 2 bloquante avant tout déploiement |

---

## Ordonnancement proposé

1. **Phase 1** (Administration + corrections critiques) — mission active, 1
   session de travail.
2. **Phase 2** (login réactivé) — immédiatement après, prérequis de recette
   écran des phases suivantes.
3. **Phase 4 (partie .NET Clients/Vendeurs)** — verrouille la qualité avant
   d'élargir.
4. **Phase 3** (stubs → réel) — par ordre de valeur métier.
5. **Phases 5–7** — dépendances externes (exports client, matériel
   physique) : lancer les demandes tôt, traiter en parallèle.

> **Critère de fin de cycle** : 114+ tests .NET verts, 41 Python verts,
> 6/6 UIA, grep rebranding vide, login réactivé et recette écran complète
> (caisse, paiement, Z, imprimante).

Mise à jour du plan + démarrage Phase 1
1. Mettre à jour plan.md
Supprimer la Phase 2 (login déjà actif)
Réordonner les phases (2 → 1, 3 → 2, etc.)
Fusionner les tests manuels login dans la Phase 1 (recette)
Ajouter une section sur les pages manquantes (gestion utilisateurs/mots de passe, config périphériques, audit)
Mettre à jour le critère de fin : "login actif et recetté" au lieu de "login réactivé"
2. Démarrer la Phase 1 (mission active)
Lot concret proposé :
#
Tâche
Fichiers
1.1
Rebranding UI (grep Atoo/Leo2 = 0)
BackOffice.xaml, OutilsViewModel.cs, vues concernées
1.2
Message neutre sans identifiants en clair
OutilsViewModel.cs
1.3
Abonnement TablesVidees pour Produits/Clients/Vendeurs
ViewModels concernés
1.4
Création AdministrationView (3 onglets)
Nouveau fichier XAML + ViewModel
1.5
Tests xUnit + grep rebranding
Tests dédiés
