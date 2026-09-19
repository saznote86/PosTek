# context.md — Mémoire centrale du projet POSTEK

> **Source de vérité unique.** Règles :
> 1. Lire ce fichier AVANT toute action — ne pas ré-analyser le code si l'info est ici.
> 2. Après chaque modification majeure : ajouter une ligne dans « Historique récent ».
> 3. Sur « fin de session » / « met à jour le contexte » : réécrire les sections
>    État actuel / Fait récemment / Bugs connus / Prochaines étapes (TODO).

---

## État actuel du projet

- **2026-09-19 — Mission corrections critiques + Administration LIVRÉE** : le
  bouton Back-Office « Importer depuis Leo2 » est renommé « Migration de
  données » (dernière référence Atoo/Leo2 du code app ; grep CI rebranding OK).
  Les ViewModels Produits/Clients/Vendeurs/Caisse sont abonnés à
  `DatabaseMaintenanceService.TablesVidees` (marshal Dispatcher, Dispose
  idempotent) et `OutilsViewModel.MessageFinPremiereInstallation` n'affiche
  aucun identifiant. `AdministrationView` (onglets Gestion/Outils/Paramètres)
  est routée depuis le Back-Office. La CI a été adaptée à la structure racine
  (desktop/, tools/, tests/ au lieu de postek/) et le .gitignore reconstruit
  (whitelist supprimée, données locales et backups .db hors dépôt). Tests :
  120/120 .NET, 42/42 Python.
- **2026-09-19 — Accueil POSTEC refondu** : `Views/AccueilView.xaml` adopte une
  carte centrale moderne avec en-tête poste/date/saint, statut licence, logo,
  actions colorées Caisse/Gestion/Outils/Licence/Quitter et tooltips.
  `AccueilViewModel` expose les commandes CaisseCommand à QuitterCommand et
  `AccueilWindow` route désormais F1 à F5. Tests : 114/114.
- **2026-09-19 — Caisse POSTEK refondue** : `MainWindow.xaml` adopte un shell
  sans bordure avec en-tête sombre, catalogue clair, recherche, familles,
  cartes articles, ticket sombre, résumé HT/TVA/timbre/total et actions Payer,
  Annuler, Mise en attente et Recherche Client. Les noms de contrôles attendus
  par `MainWindow.xaml.cs` et les flux paiement/ESC-POS/clôture Z sont conservés.
  Tests : 114/114.
- **2026-09-19 — Correction XAML caisse** : fermeture du style `ActionButton`
  corrigée après l’erreur MC3023 détectée par la compilation. Tests : relance
  en cours.
- **2026-09-19 — Tests accueil ajoutés** : `AccueilViewModelTests` couvre le
  format date/saint et le routage des cinq commandes correspondant aux
  raccourcis F1 à F5. Tests : 114/114.
- **2026-09-19 — Correction superposition boutons caisse** : ajout de deux
  lignes explicites dans la grille d’actions de `MainWindow.xaml`. Les boutons
  Payer/Annuler occupent la première ligne et Mise en attente/Recherche Client
  la seconde ; les handlers existants sont inchangés. Tests : 114/114.

### État actuel

- Les écrans accueil et caisse sont compilables via la solution de tests WPF.
- Le catalogue conserve recherche avec debounce 300 ms, filtre famille,
  sélection article, quantités, calcul HT/TVA fractionnelle, timbre fiscal et
  total TTC ; les services de paiement, impression ESC/POS, routage imprimante
  et clôture Z n’ont pas été déplacés.
- `ProductCard.xaml` reste le contrôle réutilisable existant et est alimenté
  par les articles enrichis (emoji/image/catalogue).

### Bugs connus / limites

- Les images externes plage/terminal n’ont pas été téléchargées : aucun dossier
  `Resources/` n’existait dans l’arbre initial et un placeholder vectoriel
  intégré est utilisé dans l’accueil, afin de ne pas dépendre d’une URL réseau
  non vérifiée.
- La compilation directe de `Postec.Caisse.csproj --no-restore` avait signalé
  `CS5001` dans une génération temporaire isolée ; la compilation réelle du
  projet, incluant le markup WPF, réussit durant `dotnet test`.
- Un avertissement xUnit historique (`BaseDonneesTests.cs:272`, règle
  xUnit2013) demeure sans impact : la suite est verte.

### Validation finale

- `dotnet test D:\postec\desktop\Postek.Caisse.Tests\Postec.Caisse.Tests.csproj --no-restore`
  : **114/114 réussis, 0 échec, 0 ignoré**.
- Tests manuels de rendu et parcours accueil → caisse à effectuer sur une
  session Windows avec affichage WPF ; les interactions métier existantes sont
  restées raccordées aux handlers de `MainWindow.xaml.cs`.

- **2026-09-19 — Catalogue produits enrichi** : ajout de la gestion des images
  produit (copie dans `images/produits`, aperçu, chemin SQLite, miniatures et
  carte tactile), recherche par code/désignation, filtre famille, compteurs et
  réinitialisation des filtres. Les fichiers principaux sont
  `Views/Controls/ProductCard.xaml`, `Converters/ImageConverters.cs`,
  `Views/ProduitsView.xaml` et `Services/ProduitsViewModel.cs`.
- **2026-09-19 — Gestion des familles livrée** : `FamillesView` permet la
  création, modification, ordre, compteur de produits et suppression protégée
  des familles. `BaseDonnees.SupprimerFamille` refuse une suppression si des
  articles sont encore rattachés.
- **2026-09-19 — Compatibilité ViewModel produits** : `ProduitsViewModel`
  expose les alias `Produits`, `FiltreRecherche`, `Recharger` et
  `ConfirmerEtSupprimer` pour les appelants historiques, sans retirer l'audit,
  les familles, les imprimantes ni le filtrage `ICollectionView`.
- **2026-09-19 — Menu Gestion restauré** : `MenuGestionView.xaml.cs` utilise
  de nouveau le ViewModel officiel `Services.MenuGestionViewModel`, avec
  constructeur sans paramètre et constructeur acceptant un
  `NavigationService` externe. Le routage des vues Produits, Familles, Clients,
  Vendeurs, Paramètres et Outils est conservé. Le retour fonctionne depuis
  `AccueilWindow` ou ferme la fenêtre modale ouverte par la caisse.
- **2026-09-19 — Commandes Menu Gestion complétées** : ajout et routage de
  `FamillesCommand`; le menu expose désormais les 16 commandes prévues sans
  déplacer la logique de fenêtres dans le ViewModel.
- **2026-09-19 — Fenêtre Menu Gestion validée** : `MainWindow.SurMenu` ouvre
  `MenuGestionView` dans une fenêtre `PosTec - Menu Gestion` centrée,
  `1100x700`, non redimensionnable. Build WPF réussi.
- **2026-09-19 — Mission Administration terminée** (historique) : la demande de
  fusion Gestion/Outils et des trois corrections critiques a été analysée puis
  implémentée dans la journée. À la livraison : bouton Back-Office renommé
  « Migration de données », message d'installation neutre, ViewModels
  abonnés à `DatabaseMaintenanceService.TablesVidees` et
  `AdministrationView` à trois onglets routée depuis le Back-Office.
  - **Priorité active — Mission corrections critiques + Administration :
    TERMINÉE et validée** (voir entrée correspondante en tête d'État actuel).
    Rappel des objectifs, tous couverts :
    1. supprimer toutes les références UI `Atoo`/`Leo2` du dossier desktop et
      conserver ces termes uniquement dans le code de migration isolé ;
    2. remplacer tout affichage d'identifiants initiaux par un message neutre
      sans mot de passe en clair ;
    3. notifier les ViewModels Produits, Clients, Vendeurs et Caisse après RAZ,
      avec abonnement Dispatcher et désabonnement dans `Dispose` ;
    4. remplacer l'accès séparé Gestion/Outils du Back-Office par
      `AdministrationView` avec onglets Gestion, Outils et Paramètres.
    Livrables prévus : `AdministrationView`, `GestionUnifieeView`,
    `OutilsUnifieeView`, raccordement Back-Office, adaptation de la fenêtre
    d'import, tests .NET/Python et contrôle grep de rebranding.
    **Validation finale : 120/120 tests .NET, 42/42 tests Python, grep
    rebranding CI sans match dans desktop/.**

- **2026-09-19 — Refonte POS en cours** : le XAML de `MainWindow` a été remplacé
  par une structure moderne sans bordure, en-tête sombre, catalogue à gauche et
  ticket sombre à droite. Les noms de contrôles utilisés par le code-behind ont
  été conservés pour préserver paiement, clôture, recherche, mise en attente,
  suppression et impression. Les raccourcis F2/F8/F9/F12 sont raccordés aux
  handlers existants. Tests après ce lot : build en cours.
- **2026-09-19 — Catalogue MVVM** : `Article` expose désormais emoji/prix de
  vente, `BaseDonnees` fournit `ChargerFamilles` et `ChargerArticlesParFamille`,
  et `CaisseViewModel` expose catalogue, familles, recherche avec debounce 300 ms,
  filtre famille et commandes de suppression/modification. `ProductCard` a été
  modernisée (emoji, prix millime, surface claire). Une erreur de callback timer
  a été corrigée ; validation complète en cours.
- **2026-09-19 — Tests caisse** : ajout de 6 tests ciblés couvrant chargement
  avec famille, filtre, recherche, ajout panier, totaux TVA/timbre et suppression.
  Le chargement de `MainWindow` initialise désormais le catalogue ViewModel et la
  recherche XAML est liée à `RechercheText`. Une correction de signature du test
  d’ajout panier et l’initialisation des familles de test sont appliquées avant
  la relance finale. Dernier résultat intermédiaire : 99/105, 6 échecs de
  préparation de fixtures (familles absentes), corrigés.
- **2026-09-19 — Validation finale** : build WPF réussi, suite .NET complète
  réussie à **105/105** ; `git diff --check` ne signale aucune erreur (hors
  avertissements Git de conversion LF/CRLF). Le warning xUnit préexistant dans
  `BaseDonneesTests.cs` subsiste.

### État actuel / Bugs connus / TODO

- État actuel : nouveau layout POS et carte produit compilent ; catalogue,
  familles, recherche debounce 300 ms, TVA/timbre/totaux et six tests ViewModel
  sont en place. Le routage logique par imprimante existant est conservé.
- Tests manuels WPF et test avec imprimantes physiques non exécutés dans cette
  session. L’impression effective par groupe caisse/cuisine/bar reste à
  confirmer sur périphériques réels.
- TODO : valider visuellement la fenêtre sans bordure, les raccourcis F2/F8/F9/F12,
  le paiement, la clôture Z et deux produits affectés à des imprimantes distinctes.
- **2026-09-19 — Refonte connexion démarrée** : ajout de `PasswordHelper`,
  attached property WPF destinée à synchroniser proprement `PasswordBox` et
  ViewModel sans logique métier dans la vue. Tests après ce lot : non exécutés.
- **2026-09-19 — ViewModel connexion** : remplacement de `LoginViewModel` par une
  implémentation observable et disposable, compatible avec les anciens tests,
  exposant utilisateurs, mot de passe, messages, commandes tactiles, validation,
  audit succès/échec et nettoyage idempotent. Tests après ce lot : non exécutés.
- **2026-09-19 — Vue connexion en refonte** : suppression de l’ancien XAML login
  afin de le remplacer par une vue modale sombre, tactile et sans animation
  fragile. Tests après ce lot : non exécutés. Le code-behind est en cours de
  remplacement par une version minimale et idempotente.
- **2026-09-19 — Cycle de vie LoginWindow** : nouveau code-behind minimal,
  `ShowDialog`-compatible, résultat de dialogue sur succès/annulation, focus
  au chargement et `Dispose` idempotent du ViewModel/base à la fermeture. Tests :
  compilation intermédiaire : 3 erreurs techniques corrigées avant nouvelle
  validation.
- **2026-09-19 — Routage connexion réactivé** : `App.xaml.cs` utilise désormais
  le nouveau `LoginWindow` modal et une instance neuve à chaque démarrage ; le
  routage Admin/Caissier et les étapes changement de mot de passe/ouverture de
  caisse existantes sont conservés. Tests après ce lot : build réussi.
- **2026-09-19 — Compatibilité historique corrigée** : le succès de connexion
  ré-enregistre maintenant `HistoriqueConnexions` avant l’audit détaillé, comme
  l’ancien ViewModel ; cela préserve la liste des utilisateurs récents. Tests
  après ce lot : 104/105 (régression identifiée puis corrigée).
- **2026-09-19 — Tests LoginViewModel** : ajout de 6 tests couvrant le chargement
  des utilisateurs, succès, échec, clavier tactile, effacement et annulation.
  Tests intermédiaires : 110/111 ; l’ordre de nettoyage du mot de passe effaçait
  le message d’erreur, correction appliquée avant la validation finale.
- **2026-09-19 — Validation finale login** : nouvelle `LoginWindow` sombre et
  tactile, `PasswordHelper`, ViewModel disposable, audit succès/échec, routage
  modal réactivé dans `App.xaml.cs` et six tests login ajoutés. Tests : **111/111**
  réussis ; build WPF réussi ; `git diff --check` sans erreur.

### État actuel / Bugs connus / TODO — connexion

- État actuel : une instance neuve de `LoginWindow` est créée à chaque démarrage,
  affichée par `ShowDialog()`, puis nettoyée avec un `Dispose` idempotent et un
  DataContext nul à la fermeture. Le mot de passe est synchronisé via attached
  property et n’est jamais persisté en clair.
- Le projet conserve `BaseDonnees.Authentifier` comme point d’authentification
  PBKDF2 et `AuditService` pour l’audit détaillé ; l’API existante des tests
  (`SeConnecter`, `NomUtilisateurSaisi`, `UtilisateursRecents`) reste compatible.
- Tests manuels restant à effectuer : ouvrir/fermer cinq fois la fenêtre,
  saisir via clavier tactile et physique, vérifier Admin → AccueilWindow et
  Caissier → MainWindow. Aucun test UI interactif n’a été exécuté ici.
- **2026-09-19 — Correction login demandée** : hauteur de `LoginWindow` portée à
  900 px, clavier et boutons d’action restructurés pour éviter le découpage,
  contraste du `ComboBox` corrigé. La création automatique de l’administrateur
  utilise désormais le mot de passe initial `123456` (toujours haché en PBKDF2) ;
  les appels explicites de maintenance conservent le mot de passe fourni.

- **POSTEK** : réécriture open-source de la caisse Atoo Leo2 (WinDev 14),
  conformité fiscale tunisienne (NACEF). Dépôt : `X:\ATOO_LEO2_NACEF`,
  tout le nouveau code vit dans `postek/` (branche `master`, aucun commit encore).
- **App desktop** : `postek/desktop/Postek.Caisse` (WPF, .NET 8, SQLite) +
  tests `Postek.Caisse.Tests` (xUnit).
- **Noyau Python** : `postek/fiscal` (TVA, journal SHA-256), `postek/licence`
  (Ed25519, empreinte matériel), `postek/migration` (Leo2 → POSTEK) — 41 tests unittest.
- **Tests .NET : 78/78 verts**. Les tests Python sont également **41/41 verts**.
  Le seul signal restant est un avertissement xUnit
  préexistant sur une assertion de taille de collection.
- **Outils et périphériques : raccordés**. Les vues utilisent désormais des
  ViewModels injectés, les modales Réindexation/Mots de passe sont fonctionnelles,
  et le Back-Office navigue vers Outils/Périphériques avec retour.
- **UIA du shell administrateur renforcée** : le conteneur de navigation et les
  vues injectées exposent maintenant des AutomationId/noms stables ; le service
  de navigation force layout et focus après injection.
- **Login UIA : pavé numérique ajouté** dans `LoginWindow` pour permettre la
  saisie tactile/UI Automation du mot de passe administrateur.
- **LoginWindow 3D refondue** : carte sombre en relief, gradients, ombres,
  labels dédoublonnés, pavé tactile complet 7-8-9/4-5-6/1-2-3/0-virgule-C,
  actions Annuler/Valider et apparition animée.
- **Historique de connexion ajouté** : table SQLite `HistoriqueConnexions`,
  `LoginViewModel` et ComboBox editable dans les vues de login ; les connexions
  réussies incrémentent le compteur sans jamais stocker de mot de passe.
- **Migration offline étendue** : détection locale des sources, lecture CSV/XLS(X),
  mappings clients/fournisseurs/utilisateurs/mouvements/ventes, tables cibles,
  backup/rollback et assistant WPF depuis le Back-Office.
- **Migration réelle testée** : `postek/ATOO_LEO2` est analysé et importé vers
  `postek/donnees/test_migration_reelle.db` sans modifier la production ; le
  scénario réel a produit 76 produits et 12 familles provisoires.
- **Git** : commit initial `91121dc` (47 fichiers, branche `master`) — le dépôt est
  scellé sur `postek/` + `start.bat` + CI via un .gitignore en whitelist ; toutes les
  données Leo2 (FIC/ndx/exe/BMP/HISTO…) restent hors versionnement.
- **Lancement** : `start.bat` à la racine (anti-double-lancement, compile si besoin,
  `--test-impression [imprimante]` en argument). L'app s'exécute actuellement (PID variable).

## Architecture (app desktop)

```
postek/desktop/Postek.Caisse/
├── App.xaml(.cs)          # thème global, login, ouverture de caisse, routage par rôle
├── MainWindow.xaml(.cs)   # caisse tactile, ticket, paiement, impression, raccourcis
├── FenetrePaiement.xaml(.cs)  # encaissement tactile multi-règlement
├── AccueilWindow.xaml(.cs)    # shell administrateur et navigation Accueil/Gestion/Outils
├── LoginWindow.xaml(.cs)      # authentification PBKDF2 et première installation
├── BackOffice.xaml(.cs)       # utilisateurs, rôles et journal d'audit
├── OuvertureCaisseWindow.xaml # fonds initial et ouverture de session
├── Views/                     # vues Leo2/Postek : Accueil, Gestion, Outils, Paramètres,
│                              # clôture, périphériques, paramètres caisse, modales
├── Styles/PostekTheme.xaml    # palette et styles tactiles centralisés
├── Services/                  # navigation, Saints du jour, système, imprimantes,
│                              # ports COM, maintenance SQLite, USB, clôture Z,
│                              # ViewModels Outils/Périphériques et modales
├── Brand.cs, themes/          # rebranding + thème JSON (postek.json)
└── Caisse/
  ├── CaisseService.cs   # ticket en cours : lignes, TVA (TTC×t/(1+t), AwayFromZero),
    │                      #   remises, multi-règlement (Regler/SupprimerReglement/EstSolde/
    │                      #   ResteAPayer/MonnaieARendre/DerniersReglements)
    ├── Reglements.cs      # enum ModeReglement (Especes, CB, Cheque, TicketRestaurant, Avoir)
    │                      #   + codes stables persistés (especes, cb, cheque, ticket_resto, avoir)
    ├── BaseDonnees.cs     # SQLite : ventes, clôtures, Utilisateurs, AuditLog,
    │                      #   SessionsCaisse, RapportsZ ; décimaux en TEXTE
    ├── AuthentificationService.cs # PBKDF2-SHA256, sessions et rôles Admin/Caissier
    ├── GestionCaisse.cs   # SessionCaisse, RapportZCaisse, ViewModel d'ouverture
    ├── CaisseViewModel.cs # propriétés observables + commandes tactiles
    ├── ClotureZ.cs        # RecapZ (avec ReglementsParMode), LigneZTva, LigneZMode
    ├── TicketEscPos.cs    # ESC/POS 80 mm, CP-863, pied multi-règlement + ligne RENDU
    └── ImprimanteBrute.cs # winspool RAW, repli fichier « imprimante.logique »
```

## Conventions non négociables

- Argent : `decimal`, millimes (3 déc.), arrondi `AwayFromZero` — **jamais de flottant**
  (SQLite SUM() interdit pour l'argent : agrégation côté C#).
- Taux TVA stockés en **fraction** (0.190) ; ×100 uniquement à l'affichage/impression.
- Ticket non soldé → `Encaisser()` lève ; sans règlement saisi = comptant implicite.
- Rendu monnaie = trop perçu **plafonné aux espèces reçues**.
- Rebranding : aucun texte « Atoo »/« Leo2 » dans le code app (CI bloquante).
- Sécurité : mot de passe jamais stocké en clair ; audit des connexions,
  déconnexions, annulations et opérations administrateur.
- Rôles : Administrateur accès Back-Office/Migration/Clôture ; Caissier accès vente.
- UI : thème global chargé depuis `Styles/PostekTheme.xaml`, contrôles tactiles
  réutilisables (`ProductCard`, `CartPanel`, `NumericKeypad`, `MenuCard`,
  `FinancialSummary`).

## Historique récent

- **2026-09-19 : Préparation de la fusion Administration et des corrections
  critiques.** L'analyse a confirmé que `DatabaseMaintenanceService` possède
  déjà l'événement statique `TablesVidees` et le déclenche après
  `ViderTablesMetier`, mais que les abonnements métier et le remplacement UI
  restent à implémenter. La création de `AdministrationView`,
  `GestionUnifieeView` et `OutilsUnifieeView` est également en attente. Aucun
  changement de code n'a été validé pour ce lot.
- **2026-09-19 : Mission reprise demandée par l'utilisateur.** Le périmètre
  confirmé comprend les textes Back-Office et import, la suppression de tout
  affichage `admin/admin`, l'invalidation globale après RAZ et la fusion des
  menus Gestion/Outils dans une fenêtre Administration à trois onglets. Avant
  implémentation, l'état vérifié reste inchangé :
  `DatabaseMaintenanceService.TablesVidees` existe déjà et est déclenché par
  `ViderTablesMetier`, mais les abonnements complets et les nouvelles vues ne
  sont pas encore livrés.
- **2026-09-19 : Menu Gestion et SurMenu revalidés.** Une version intermédiaire
  du code-behind avait remplacé le XAML et créé un ViewModel dupliqué dans le
  namespace `Views`; ces fichiers ont été restaurés avec le contrat réel
  `Services.MenuGestionViewModel` et le XAML des 16 boutons. Compilation finale
  : réussie sans erreur ni avertissement.

- **2026-09-19 : Images, filtres et familles produits livrés.** `Article`
  transporte `CheminImage`, les converters WPF sont enregistrés dans
  `App.xaml`, `ProductCard` affiche l'image ou l'emoji, et le catalogue admin
  fournit recherche, filtre famille, statistiques et gestion des familles.
  Build WPF réussi et tests ciblés produits : **14/14**.

- **2026-09-19 : `start.bat` adapté au dossier racine actuel du dépôt.** Le script
  utilisait un chemin en double `postek\...` alors que le workspace est déjà
  la racine du projet. Les chemins de compilation et d’exécution pointent désormais
  vers `desktop\Postek.Caisse` directement depuis ce dossier.

- **2026-09-19 : Bouton Back-Office « Effacer l'historique des connexions » ajouté. Tests : 86/86 .NET réussis.**
  Nouveau bouton (orange, confirmation MessageBox) sur la page Utilisateurs du
  Back-Office : appelle `BaseDonnees.EffacerHistoriqueConnexions()` (désactivation
  douce `EstActif=0`, comptes et mots de passe intacts), journalise
  « Effacement historique connexions » dans `AuditLog` puis recharge la vue.
  Test unitaire ajouté (`EffacerHistoriqueConnexions_DesactiveToutesLesEntrees`)
  : liste vide après effacement + aucune ligne `EstActif=1` en base. Seule la
  validation UI manuelle à l'écran reste à faire (login désactivé actuellement).
  Fichiers impactés : `BackOffice.xaml`, `BackOffice.xaml.cs`,
  `Postek.Caisse.Tests/AuthentificationTests.cs`.

- **2026-09-18 : Démarrage sans login vérifié à l'écran + crash Owner corrigé. Tests : 85/85 .NET réussis.** Lancement réel (`Postek.Caisse.exe`, observation fenêtres à T+3/8/15 s via le nouveau
  `postek/tools/attribuer_fenetres.py` : PID + process propriétaire) : aucun
  « POSTEK - Connexion » ; enchaînement direct Ouverture de caisse (fonds
  initial, session absente) → Accueil admin → Caisse via bouton. Fermeture
  propre par l'utilisateur, aucun événement Application/WER. Au passage, bug
  réel corrigé dans `App.OnStartup` : `ChangerMotDePasseModal { Owner = login }`
  levait « Impossible de définir la propriété Owner avec un objet Window qui a
  été fermé » dès qu'un compte `DoitChangerMotDePasse=1` se connectait
  (exactement le cas `admin/admin` après RAZ — deux crashes observés 22:54 et
  23:03 sur l'ancien binaire, journalisés dans `postek_demarrage.log`). Fix :
  ne plus fixer d'Owner (le LoginWindow est déjà fermé quand `ShowDialog()`
  retourne). NB : deux instances résiduelles sur anciens binaires circulaient
  pendant la vérification (fenêtres Accueil/Back-Office/Caisse + MessageBox
  « Confirmation 1/2 » de RAZ) — disparues, ne pas les confondre avec l'app fraîche.

- **2026-09-18 : Écran de connexion désactivé provisoirement. Tests : 85/85 .NET réussis.** `ConnexionDesactiveeProvisoirement` repassé à `true` dans `App.OnStartup` (session administrateur existante ou recréée, sans modifier `LoginWindow`) ; réactiver l'écran en repassant le drapeau à `false`. TODO production inchangé : réactiver avant livraison.

- **2026-09-18 : Prompt « Correction RAZ (admin par défaut) + Layout LoginWindow » exécuté et vérifié — code déjà conforme, aucun correctif requis. Tests : 85/85 .NET + 42/42 Python réussis.** Vérification point par point : `OutilsViewModel.RemiseAZ` vide uniquement `DatabaseMaintenanceService.TablesMetierParDefaut` (jamais `Utilisateurs`/`Parametres`/`Licence`/`AuditLog` ; la whitelist `TablesMetierAutorisees` refuse `Utilisateurs` et lance sans effacer), puis recrée/réinitialise l'admin via `CreerOuReinitialiserAdministrateur("admin", true)` (PBKDF2-SHA256 600 000 itérations, rôle `Administrateur`, `DoitChangerMotDePasse=1`) et journalise audit + `raz_log.txt`. Équivalent `CreerAdminParDefaut()` opérationnel : `GarantirAdministrateurParDefaut()` + `CreerOuReinitialiserAdministrateur()` dans `BaseDonnees` ; `LoginViewModel.SeConnecter` authentifie via `BaseDonnees.Authentifier` (PBKDF2). Layout `LoginWindow.xaml` conforme : fenêtre 500x750 `CenterScreen`/`NoResize`, clavier `UniformGrid` `Width/MaxWidth=360` centré, touches fixes `110x70` `FontSize=24` `Margin=5` (7-8-9/4-5-6/1-2-3/0-virgule-C), grille Annuler/Valider `360px` centrée avec `Annuler` 170x50 aligné gauche et `Valider` 170x50 aligné droite (marge gauche 20). Couverture RAZ assurée par `RazInstallationTests` : admin/admin accepté après RAZ, ancien mot de passe rejeté, `Parametres` conservés, table non autorisée refusée sans effacement, séquence `SessionsCaisse` réinitialisée. NB : test manuel écran bloqué tant que `ConnexionDesactiveeProvisoirement` reste à `true` (TODO déjà listé) ; la connexion admin/admin après RAZ est validée par les tests unitaires.

- **2026-09-18 : Bouton Caisse/F1 réparé — crash SQLite au clic. Tests : 85/85 .NET réussis.** Le clic (et la touche F1) ferment l'application : la RAZ laisse des familles par défaut (`DIVERS/BOISSONS/ALIMENTATION`) avec la table `article` vide, et `CreerDemo` ne réinsère ses familles de démo que si la table est vide → violation de clé étrangère dans `BaseDonnees.AjouterArticle` (`BOULANGERIE`/`SANDWICHS` absentes) → exception dans le constructeur `MainWindow`. Correctif : `CreerDemo` garantit l'existence des familles de démo via `AjouterFamilleSiAbsente` (INSERT OR IGNORE, sans écraser les familles existantes) avant de semer les articles. Vérifié à l'écran (UIA) : clic bouton et touche F1 ouvrent la caisse ; base démo auto-réparée (5 familles, 12 articles, 0 orphelin).

- **2026-09-18 : Bouton Caisse/F1 rendu robuste. Tests : 85/85 .NET réussis.** Le bouton utilise maintenant une commande explicite et la fenêtre possède un `KeyBinding` F1 ; les raccourcis F1/F2/F3 marquent l'événement comme traité pour éviter qu'un contrôle enfant l'absorbe. Avertissement restant : règle xUnit préexistante dans `BaseDonneesTests.cs`.

- **2026-09-18 : Écran de connexion désactivé provisoirement pour les vérifications locales. Tests : 85/85 .NET réussis.** `App.OnStartup` utilise temporairement une session administrateur existante ou recréée, sans modifier `LoginWindow`; le champ `ConnexionDesactiveeProvisoirement` permettra de réactiver l'écran en le repassant à `false`. TODO : réactiver avant livraison de production.

- **2026-09-18 : RAZ complète de l'historique de connexion. Tests : 85/85 .NET réussis.** `HistoriqueConnexions` est maintenant inclus dans `TablesMetierParDefaut`; `Utilisateurs`, `Parametres`, `Licence` et `AuditLog` restent conservés, puis `admin/admin` est réinitialisé avec changement obligatoire.

- **2026-09-18 : Layout LoginWindow aligné sur la structure proposée. Tests : 85/85 .NET réussis.** Le clavier numérique reste centré dans une zone fixe `360x300` avec touches `110x70`, sans marge verticale additionnelle ; la zone Annuler/Valider conserve ses contraintes `360px` et `170px` par bouton. Les handlers d'authentification sont conservés. Avertissement restant : règle xUnit préexistante dans `BaseDonneesTests.cs`.

- **2026-09-18 : RAZ réinitialise explicitement admin en admin/admin. Tests : 85/85 .NET réussis.** Après le vidage métier, le compte administrateur est conservé ou recréé avec le mot de passe par défaut `admin` et `DoitChangerMotDePasse=1`; l'ancien mot de passe n'est plus accepté après RAZ.
- **2026-09-18 : Layout LoginWindow contraint et première création sécurisée. Tests : 85/85 .NET réussis.** Fenêtre `500x750`, clavier centré `360x300` avec touches `110x70`, actions centrées et premier compte marqué pour changement de mot de passe obligatoire. TODO : validation visuelle manuelle recommandée sur l'écran réel.

- **2026-09-18 : Admin rendu visible et recréé au chargement de la connexion. Tests : 85/85 .NET réussis.** La liste de connexion inclut maintenant tous les utilisateurs actifs, même sans historique de connexion, tout en conservant les historiques orphelins des anciennes bases ; si `admin` manque mais que d'autres comptes existent, il est recréé avant le chargement de l'UI.

- **2026-09-18 : Compte admin garanti après RAZ. Tests : 84/84 .NET réussis.** La remise à zéro conserve le compte `admin` et son mot de passe existant ; si la base n'en contient aucun, elle recrée `admin/admin` avec changement obligatoire et écrit aussi l'opération dans `AuditLog`.

- **2026-09-18 : Session utilisateur préparée pour le changement obligatoire de mot de passe. Tests : validation en cours.** `UtilisateurSession` transporte maintenant le flag `DoitChangerMotDePasse`, nécessaire à la première installation sécurisée.
- **2026-09-18 : Schéma de configuration et comptes enrichi pour RAZ/installation. Tests : validation en cours.** Ajout additif de `Parametres`, `Licence`, de `Utilisateurs.DoitChangerMotDePasse`, ainsi que des APIs de création et de changement de mot de passe dans `BaseDonnees`.
- **2026-09-18 : Maintenance destructive réelle ajoutée. Tests : validation en cours.** `DatabaseMaintenanceService` crée les backups horodatés, vide les tables métier autorisées dans une transaction avec rollback, réinitialise les séquences et initialise paramètres/familles par défaut.
- **2026-09-18 : Initialisation du compte et de la session de caisse ajoutée. Tests : validation en cours.** `BaseDonnees` sait maintenant créer ou réinitialiser `admin`, activer `DoitChangerMotDePasse` et créer une première session fermée.
- **2026-09-18 : Workflows RAZ et première installation branchés dans Outils. Tests : validation en cours.** Double confirmation avec code `RAZ`, backup avant opération, vidage métier transactionnel, journal `raz_log.txt`, paramètres/familles par défaut et création de `admin/admin` avec changement de mot de passe obligatoire.
- **2026-09-18 : Dialogues RAZ et installation reliés à la vue Outils. Tests : compilation à vérifier.** Le code-behind propose maintenant la confirmation texte exacte `RAZ`, la confirmation d'installation et le feedback de fin d'opération.
- **2026-09-18 : RAZ tolérante aux tables optionnelles. Tests : compilation à vérifier.** `ViderTablesMetier` vérifie l'existence de chaque table whitelistée avant suppression, ce qui permet de traiter les anciennes bases sans schéma complet.
- **2026-09-18 : Tests unitaires RAZ/installation ajustés. Tests : filtre ciblé à relancer.** Les attentes distinguent maintenant la conservation d'une ligne de paramètres par la RAZ et les quatre paramètres créés par l'installation.
- **2026-09-18 : ViewModel de changement obligatoire ajouté. Tests : validation en cours.** Ajout de `ChangerMotDePasseViewModel` avec validation de longueur, confirmation, mise à jour PBKDF2 et audit.
- **2026-09-18 : Modale de changement obligatoire créée. Tests : compilation à vérifier.** `ChangerMotDePasseModal` permet de modifier le mot de passe avant l'ouverture de caisse et refuse l'annulation tant que le changement n'est pas validé par le flux de démarrage.
- **2026-09-18 : RAZ, première installation et changement obligatoire intégrés. Tests : 84/84 .NET réussis.** Backup horodaté, vidage métier transactionnel, journal `raz_log.txt`, paramètres/familles par défaut, admin `admin/admin` marqué `DoitChangerMotDePasse` et modale de changement au démarrage sont opérationnels. Avertissement restant : règle xUnit préexistante dans `BaseDonneesTests.cs`.

- **2026-09-18 : Écran de connexion affiné selon la capture de référence. Tests : 79/79 .NET réussis.** Dimensions de fenêtre légèrement augmentées, rendu texte net, carte centrale mieux équilibrée, champs plus lisibles et pavé tactile mieux espacé, sans modification du flux d'authentification. Fichier impacté : `postek/desktop/Postek.Caisse/LoginWindow.xaml`.

- **2026-09-18 : Commandes Outils alignées et horloge système activée. Tests : 79/79 .NET + 42/42 Python.** Ajout des alias de commandes attendus, de `ReparerIndexAsync` et ouverture de `timedate.cpl`; les services de maintenance restent injectés dans `OutilsViewModel`.
- **2026-09-18 : Menu Gestion nettoyé : 15 cases, 15 actions. Tests : 79/79 .NET + 42/42 Python.** Suppression de la case vide désactivée ; chaque bouton visible correspond maintenant à une commande ou au Retour.
- **2026-09-18 : Six vues de gestion supplémentaires créées et routées. Tests : validation en cours.** Ajout de vues navigables et de DataContexts pour Comptabilité, Historique des tickets, Ventilations/Tarifs, Claviers tactiles, Programmation avancée et Réglage date-heure, avec commandes d'action et Retour. `MenuGestionView` route maintenant ces destinations vers leurs vues.

- **2026-09-18 : Vues de gestion Produits, Clients et Vendeurs créées. Tests : validation en cours.** Ajout de `GestionStubViewModel` et de trois vues navigables avec DataGrid, commandes Nouveau/Modifier/Supprimer et Retour. Les vues servent de stubs fonctionnels en attendant les opérations métier complètes.

- **2026-09-19 : CRUD Produits réel livré. Tests : 4/4 ciblés verts ; compilation WPF réussie.** Création de `Services/ProduitsViewModel.cs` avec chargement SQLite de `article`, sélection, commandes Nouveau/Modifier/Supprimer/Retour, validation des montants `decimal` à 3 décimales et TVA en fraction. `Views/ProduitsView.xaml` est maintenant liée à `Articles`/`ArticleSelectionne`, `ArticleModale.xaml` permet la création et la modification, et la navigation depuis `MenuGestionView` injecte le ViewModel. Ajout de `ProduitsViewModelTests.cs` couvrant chargement, insertion, modification et suppression. La confirmation utilisateur avant suppression et l'audit restent à compléter avant production.
- **2026-09-19 : Audit et confirmation Produits ajoutés. Tests : 94/94 .NET après reconstruction.** Ajout de `Services/AuditService.cs`, audit des créations/modifications/suppressions avec détails code/nom/prix, confirmation tactile `YesNo/Warning` avant suppression et quatre tests ciblés supplémentaires, dont le refus sans confirmation.
- **2026-09-19 : Clients reliés à une modale CRUD réelle. Tests : 94/94 .NET verts.** Ajout de `ClientModale.xaml(.cs)` avec les champs métier, validation de la limite de crédit et branchement création/modification sur `ClientsViewModel`.
- **2026-09-19 : Vendeurs reliés à `Utilisateurs` et PBKDF2. Tests : 94/94 .NET verts.** Ajout de `VendeursViewModel`, `VendeurModale` et DataGrid métier ; création/modification hachent toujours le mot de passe via `AuthentificationService`, suppression désactive le compte et les opérations sont auditées.
- **2026-09-19 : Références WPF des écrans Clients/Vendeurs corrigées. Tests : build WPF réussi + 94/94 .NET verts.** Ajout des imports `Window`, `ComboBoxItem` et contrôles associés dans les code-behind des modales et vues.
- **2026-09-19 : Retour Clients/Vendeurs raccordé au NavigationService. Tests : validation finale en cours.** Les deux nouveaux ViewModels déclenchent désormais le retour vers le menu Gestion comme Produits.

- **2026-09-18 : Menus Gestion et Outils reliés aux commandes et services. Tests : 79/79 .NET.** Ajout de `MenuGestionViewModel`, navigation par destination, commandes des actions Outils, glyphes visibles des boutons, branchement de la clôture et injection des ViewModels dans les vues. Les destinations sans vue dédiée affichent désormais un retour explicite au lieu d'un clic silencieux. Fichiers impactés : `postek/desktop/Postek.Caisse/Services/NavigationService.cs`, `MenuGestionViewModel.cs`, `OutilsViewModel.cs`, `Views/MenuGestionView.xaml`, `Views/MenuGestionView.xaml.cs`, `Views/OutilsView.xaml`, `Views/OutilsView.xaml.cs`, `AccueilWindow.xaml.cs`.

- **2026-09-18 : Catalogue migré publié dans les tables de caisse. Tests : validation en cours.** Le moteur synchronise désormais `produit/famille` vers `article/famille_article`, avec conversion des codes TVA Leo2 en fractions POSTEK. Fichier impacté : `postek/migration/engine.py`.

- **2026-09-18 : Fenêtre Migration corrigée après capture UI. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.** Ajout de deux lignes Grid explicites pour séparer le dossier source et la base SQLite ; le sélecteur accepte désormais les fichiers source `.FIC/.XLS/.XLSX/.CSV/.xdd`. Fichiers impactés : `FenetreMigration.xaml`, `FenetreMigration.xaml.cs`.

- **2026-09-18 : LoginWindow compacte pour afficher les actions. Tests : validation en cours.** Dimensions `500x750`, marges/padding réduits, titre `36px`, touches `60x60`, actions `50px` et pied de page réduit. Fichier impacté : `postek/desktop/Postek.Caisse/LoginWindow.xaml`.
- **2026-09-18 : Pavé tactile du login sécurisé par handlers Click directs. Tests : validation en cours.** Fichiers impactés : `LoginWindow.xaml`, `LoginWindow.xaml.cs`. Les touches restent visuelles/UIA et alimentent directement le PasswordBox actif.
- **2026-09-18 : Layout LoginWindow validé avec authentification UIA. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.** La fenêtre compacte expose `Valider`, `Annuler` et toutes les touches ; le sixième échec reste limité à `ParametresPeripheriquesView`.
- **2026-09-18 : Boutons LoginWindow forcés visibles et identifiables UIA. Tests : validation en cours.** `Grid Margin=20`, carte `Padding=24`, boutons `BtnValider`/`BtnAnnuler` visibles avec `Panel.ZIndex=20`, et bouton de création conservé pour la première installation. Fichiers impactés : `LoginWindow.xaml`, `LoginWindow.xaml.cs`.
- **2026-09-18 : Boutons LoginWindow confirmés par UIA. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.** L’arbre UIA expose `Annuler`, `Valider` et les 12 touches du pavé ; l’échec restant concerne uniquement `ParametresPeripheriquesView`.

- **2026-09-18 : Ancien XAML LoginWindow retiré pour refonte 3D. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.** Fichier impacté : `postek/desktop/Postek.Caisse/LoginWindow.xaml`.
- **2026-09-18 : LoginWindow refondue en interface 3D tactile. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.** Fichiers impactés : `LoginWindow.xaml`, `LoginWindow.xaml.cs`. Suppression du label identifiant dupliqué, pavé 7-8-9/4-5-6/1-2-3/0-virgule-C, actions Annuler/Valider, gradients, ombres et apparition animée.

- **2026-09-18 : Analyseur du dossier réel ajouté. Tests : validation en cours.**
  Fichier impacté : `postek/tools/analyser_dossier_leo2.py`. Le rapport
  recensera les extensions, exports natifs, tables `.FIC`, sous-dossiers, taille
  et date de dernière modification.
- **2026-09-18 : Analyse réelle du dossier `postek/ATOO_LEO2`. Tests : 41/41 Python + 78/78 .NET + 0/6 UIA.**
  Rapport : 947 fichiers, 101 `.FIC`, 99 `.ndx`, 9 `.mmo`, 29 DLL, 1 export
  natif (`produits.XLS`), 1 `.xdd`, 23 sous-dossiers et 87 686 633 octets.
  Fichier généré : `postek/donnees/rapport_analyse.json`.
- **2026-09-18 : Normalisation du format brut `produits.XLS`. Tests : validation en cours.**
  `MoteurMigration` transforme maintenant `famille/libelle/prix/tva/cbarre` en
  lignes produit POSTEK avec codes synthétiques stables `P001...`.
  Fichier impacté : `postek/migration/engine.py`.
- **2026-09-18 : Familles provisoires générées depuis `produits.XLS`. Tests : validation en cours.**
  Les codes famille présents dans l’export réel sont maintenant créés avec un
  libellé temporaire `Famille <code>`, supprimant les références orphelines en
  attendant un export famille natif.
- **2026-09-18 : Migration réelle depuis `postek/ATOO_LEO2`. Tests : 78/78 .NET + 41/41 Python + 5/6 UIA.**
  Rapport d’analyse : 947 fichiers, 101 `.FIC`, 1 export natif `produits.XLS`.
  Migration de test : 76/78 produits importés, 12 familles provisoires, 2
  rejets de code-barres dupliqués, 0 référence orpheline. Fichiers générés :
  `postek/donnees/rapport_analyse.json`, `postek/donnees/rapport_migration.json`,
  `postek/donnees/validation_migration_reelle.json`,
  `postek/donnees/test_migration_reelle.db`.
- **2026-09-18 : Script de validation SQLite ajouté. Tests : validation en cours.**
  `postek/tools/valider_migration.py` compte les entités migrées et contrôle les
  références produit/client, les familles produit et les rôles utilisateur.

- **2026-09-18** : Correction de l’exposition UIA du contenu dynamique :
  AutomationId `MainContentControl`/`ParametresPeripheriquesView`, noms UIA
  des vues injectées, layout/focus forcés dans `NavigationService` et test
  Périphériques ciblant le ContentControl avant ses descendants. Fichiers
  impactés : `BackOffice.xaml`, `Views/ParametresPeripheriquesView.xaml`,
  `Views/OutilsView.xaml`, `Views/MenuGestionView.xaml`,
  `Services/NavigationService.cs`, `tools/test_admin_ui.py`. Tests : validation
  en cours.
- **2026-09-18** : Peer UIA personnalisé ajouté au conteneur de navigation du
  Back-Office pour exposer explicitement le UserControl injecté comme enfant
  UI Automation. Fichiers impactés : `Views/Controls/AutomationContentControl.cs`,
  `BackOffice.xaml`. Tests : validation en cours.
- **2026-09-18** : Correction du sélecteur Admin : `MainContentControl` est
  désormais recherché via `CurrentAutomationId`, et non comme nom UIA.
  Fichiers impactés : `tools/test_helpers.py`, `tools/test_admin_ui.py`.
  Tests : validation en cours.
- **2026-09-18** : Recherche de `MainContentControl` renforcée par
  `FindAll(TreeScope.Descendants, AutomationId)` UIA natif, avec repli sur les
  parcours d’arbre existants. Fichier impacté : `tools/test_helpers.py`.
  Tests : validation en cours.
- **2026-09-18** : Peer UIA personnalisé complété avec AutomationId/Name
  explicites et événement `StructureChanged` disponible après remplacement du
  contenu. Fichier impacté : `Views/Controls/AutomationContentControl.cs`.
  Tests : validation en cours.
- **2026-09-18** : `NavigationService` déclenche maintenant le rafraîchissement
  structurel du peer UIA après navigation et retour. Fichier impacté :
  `Services/NavigationService.cs`. Tests : validation en cours.
- **2026-09-18** : Correction d’une indentation introduite dans la boucle
  d’attente UIA du scénario Périphériques. Fichier impacté :
  `tools/test_admin_ui.py`. Tests : validation en cours.
- **2026-09-18** : Correction de l’indentation du sélecteur
  `MainContentControl` dans le scénario Périphériques. Fichier impacté :
  `tools/test_admin_ui.py`. Tests : validation en cours.

- **2026-09-18** : Extraction d’une façade de helpers UIA partagés pour les
  tests écran : recherche de contrôles, navigation, clavier tactile, attente
  des fenêtres et vérification de contenu. Fichier impacté :
  `postek/tools/test_helpers.py`. Tests : validation en cours.
- **2026-09-18** : `test_admin_ui.py` bascule sur les helpers partagés et
  accepte désormais le `ComboBox` editable du login ; le scénario Gestion est
  renommé `test_navigation_accueil_gestion`. Fichier impacté :
  `postek/tools/test_admin_ui.py`. Tests : validation en cours.
- **2026-09-18** : Guide écran ajusté pour documenter `test_helpers.py` comme
  source commune des helpers UIA. Fichier impacté :
  `postek/docs/GUIDE_TESTS_ECRAN.md`. Tests : validation syntaxique UIA réussie.
- **2026-09-18** : Correction du test Admin : fermeture des modales attendue
  sur leur `HWND` réel plutôt que sur un titre UIA potentiellement périmé.
  Fichiers impactés : `postek/tools/test_helpers.py`,
  `postek/tools/test_admin_ui.py`. Tests : validation locale en cours.
- **2026-09-18** : Activation UIA des onglets corrigée via
  `SelectionItemPattern` pour `MotsDePasseModal`, au lieu de supposer la
  présence d’`InvokePattern`. Fichier impacté : `postek/tools/test_helpers.py`.
  Tests : validation locale en cours.
- **2026-09-18** : Vérification de contenu UIA rendue tolérante aux erreurs COM
  transitoires pendant le rafraîchissement WPF des périphériques. Fichier
  impacté : `postek/tools/test_helpers.py`. Tests : validation locale en cours.
- **2026-09-18** : Vérification des textes des vues WPF complétée par un repli
  `RawViewWalker`, nécessaire pour les contenus injectés dans `ContentControl`.
  Fichier impacté : `postek/tools/test_helpers.py`. Tests : validation locale
  en cours.
- **2026-09-18** : Scénario Périphériques rendu plus robuste : attente des
  boutons réellement exposés par UIA avant de vérifier les libellés système,
  au lieu de dépendre uniquement du texte injecté dans `ContentControl`.
  Fichier impacté : `postek/tools/test_admin_ui.py`. Tests : validation locale
  en cours.
- **2026-09-18** : Recherche UIA textuelle durcie contre les exceptions COM
  levées pendant l’énumération WPF ; les boucles d’attente peuvent maintenant
  reprendre sur un arbre rafraîchi. Fichier impacté :
  `postek/tools/test_helpers.py`. Tests : validation locale en cours.
- **2026-09-18** : Ajout d’un diagnostic de fenêtres après le clic
  Périphériques afin de distinguer une fermeture réelle du Back-Office d’une
  indisponibilité temporaire de l’arbre UIA. Fichier impacté :
  `postek/tools/test_admin_ui.py`. Tests : diagnostic local en cours.
- **2026-09-18** : Ajout temporaire d’un inventaire des contrôles UIA exposés
  après navigation Périphériques pour aligner les sélecteurs sur l’arbre WPF
  réel. Fichier impacté : `postek/tools/test_admin_ui.py`. Tests : diagnostic
  local en cours.
- **2026-09-18** : Parcours UIA récursif rendu résilient aux erreurs COM sur
  `GetFirstChildElement` et `GetNextSiblingElement`, afin de préserver les
  contrôles déjà exposés pendant les rafraîchissements WPF. Fichier impacté :
  `postek/tools/test_helpers.py`. Tests : validation locale en cours.
- **2026-09-18** : Parcours récursif local de `test_admin_ui.py` aligné sur la
  reprise COM du module partagé. Fichier impacté :
  `postek/tools/test_admin_ui.py`. Tests : validation locale en cours.
- **2026-09-18** : Contrat UIA explicite ajouté à `ParametresPeripheriquesView`
  et à ses contrôles système/ports afin de stabiliser la découverte pendant le
  remplacement du `ContentControl`. Fichiers impactés :
  `Views/ParametresPeripheriquesView.xaml`, `test_admin_ui.py`. Tests : build et
  validation UIA locale en cours.
- **2026-09-18** : Point d’ancrage `HoteNavigation` du Back-Office nommé
  explicitement pour UIA ; le test Périphériques recherche désormais ce
  conteneur avant ses boutons. Fichiers impactés : `BackOffice.xaml`,
  `test_admin_ui.py`. Tests : build et validation UIA en cours.
- **2026-09-18** : Le helper partagé remplace désormais le `find_by_name`
  direct de `test_mix_ui.py` par une recherche UIA tolérante aux `COMError`.
  Fichier impacté : `postek/tools/test_helpers.py`. Tests : validation locale
  en cours.
- **2026-09-18** : Recherche UIA complétée par un parcours `RawViewWalker`
  pour les contrôles hébergés dynamiquement dans le `ContentControl` du
  Back-Office. Fichier impacté : `postek/tools/test_helpers.py`. Tests :
  validation locale en cours.
- **2026-09-18** : Rafraîchissement UIA stabilisé après navigation Back-Office :
  `HoteNavigation` force désormais le layout et reçoit le focus après injection
  d’une vue. Fichier impacté : `BackOffice.xaml.cs`. Tests : build et validation
  UIA en cours.
- **2026-09-18** : Le flux local Admin valide les étapes login, Gestion, Outils,
  Réindexation et Mots de passe (5/6). La vue Périphériques reste fonctionnelle
  côté application mais son contenu injecté n’est pas exposé par UIA dans la
  session locale ; le scénario est conservé en échec explicite pour diagnostic
  CI. Fichiers impactés : `test_admin_ui.py`, `test_helpers.py`,
  `BackOffice.xaml`, `BackOffice.xaml.cs`, `ParametresPeripheriquesView.xaml`.
  Tests : build .NET et syntaxe Python validés ; UIA local 5/6.

- **2026-09-18** : Extension du lecteur de migration avec détection
  d’encodage UTF-8/CP1252/Latin-1, lecture CSV/XLS(X), normalisation des
  colonnes et inventaire des exports locaux. Fichier impacté :
  `postek/migration/export_reader.py`. Tests : **41/41 tests Python verts**.
- **2026-09-18** : Ajout des mappings déclaratifs Fournisseurs, Utilisateurs,
  Mouvements et Ventes dans le schéma de migration. Fichier impacté :
  `postek/migration/leo2_schema.py`. Tests : **41/41 tests Python verts**.
- **2026-09-18** : Moteur de migration étendu avec tables Fournisseur,
  Mouvement et Vente, détection locale de dossier, inventaire XLS/CSV,
  orchestration multi-entités, alias de colonnes et restauration du backup sur
  erreur fatale. Fichier impacté : `postek/migration/engine.py`. Tests :
  **41/41 tests Python verts**.
- **2026-09-18** : Correction du routage des exports XLS/XLSX et du conflit de
  nom qui masquait le mapping métier des mouvements. Fichiers impactés :
  `postek/migration/export_reader.py`, `postek/migration/leo2_schema.py`.
  Tests : **41/41 tests Python verts**.
- **2026-09-18** : Ajout des méthodes d’import par entité et remontée explicite
  des mouvements rejetés lorsque leur produit est inconnu. Fichier impacté :
  `postek/migration/engine.py`. Tests : validation en cours.
- **2026-09-18** : Ajout de six tests Python offline couvrant détection de
  dossier, absence de source, inventaire/encodage, import client, rejet de
  mouvement incohérent et rollback. Fichiers impactés :
  `postek/tests/test_migration.py`, `postek/migration/leo2_schema.py`. Tests :
  validation en cours.
- **2026-09-18** : Correction de la détection automatique pour ne scanner que
  le dossier POSTEK fourni et ses descendants, sans faux positif provenant du
  dossier de travail courant. Fichier impacté : `postek/migration/engine.py`.
  Tests : **41/41 tests Python verts**.
- **2026-09-18** : Ajustement du test de détection pour vérifier le sous-dossier
  source retourné par le scanner. Fichier impacté : `postek/tests/test_migration.py`.
  Tests : validation en cours.
- **2026-09-18** : Peer UIA marqué explicitement comme élément de contrôle et
  de contenu pour permettre sa découverte par `TreeScope.Descendants`.
  Fichier impacté : `Views/Controls/AutomationContentControl.cs`. Tests :
  validation en cours.
- **2026-09-18** : Conteneur et vues injectées rendus focusables/tabulables afin
  que WPF les conserve dans l’arbre UIA après navigation dynamique. Fichiers
  impactés : `BackOffice.xaml`, `Views/ParametresPeripheriquesView.xaml`,
  `Views/OutilsView.xaml`, `Views/MenuGestionView.xaml`. Tests : validation en
  cours.
- **2026-09-18** : Peer enfant UIA explicite ajouté pour parcourir les contrôles
  visuels du `UserControl` injecté lorsque son peer WPF standard est nul.
  Fichier impacté : `Views/Controls/AutomationContentControl.cs`. Tests :
  validation en cours.
- **2026-09-18** : Retrait du parcours visuel récursif du peer UIA après
  fermeture du processus WPF pendant une requête UIA ; le peer conserve les
  AutomationId et le rafraîchissement structurel sans traversal risqué.
  Fichier impacté : `Views/Controls/AutomationContentControl.cs`. Tests :
  78/78 .NET + 41/41 Python + 5/6 UIA.
- **2026-09-18** : Import WPF manquant corrigé pour compiler les propriétés
  d’automatisation du peer personnalisé. Fichier impacté :
  `Views/Controls/AutomationContentControl.cs`. Tests : validation en cours.
- **2026-09-18** : Raccordement desktop de la migration : tables additives
  `Clients`, `Fournisseurs`, `Mouvements`, `Familles`, bouton administrateur
  « Importer depuis Leo2 » et détection automatique dans `FenetreMigration`.
  Fichiers impactés : `Caisse/BaseDonnees.cs`, `BackOffice.xaml`,
  `BackOffice.xaml.cs`, `FenetreMigration.xaml.cs`. Tests : **78/78 tests .NET verts**.
- **2026-09-18** : Guide opératoire de migration offline ajouté, avec priorité
  CSV/XLS(X), entités importées, backup/rollback, sécurité des mots de passe et
  repli guidé pour les `.FIC` chiffrés. Fichier impacté :
  `postek/docs/GUIDE_MIGRATION_LEO2.md`. Tests : **41/41 Python et 78/78 .NET verts**.

- **2026-09-18** : Ajout de l’historique local des connexions : migration SQLite
  `HistoriqueConnexions`, chargement des 10 utilisateurs actifs les plus récents,
  incrément du compteur après authentification réussie, ComboBox editable avec
  icône utilisateur et tests de persistance/sécurité. Fichiers impactés :
  `Caisse/BaseDonnees.cs`, `LoginViewModel.cs`, `LoginWindow.xaml`,
  `LoginWindow.xaml.cs`, `Views/LoginView.xaml`, `Views/LoginView.xaml.cs`,
  `AuthentificationTests.cs`. Tests : **77/77 tests .NET verts** ; contrôle UI
  manuel de trois connexions restant à exécuter.
- **2026-09-18** : Le ViewModel expose désormais le pavé numérique et
  pré-sélectionne automatiquement l’unique utilisateur historique. Fichiers
  impactés : `LoginViewModel.cs`, `LoginWindow.xaml.cs`. Tests : **77/77 tests
  .NET verts**.
- **2026-09-18** : La lecture de l’historique désactive automatiquement les
  entrées correspondant à un compte devenu inactif et un test d’intégration
  couvre trois connexions réussies avec deux utilisateurs. Fichiers impactés :
  `Caisse/BaseDonnees.cs`, `AuthentificationTests.cs`. Tests : validation en
  **78/78 tests .NET verts**.
- **2026-09-18** : Validation de l’identifiant restaurée avant la création du
  premier compte, afin d’éviter toute insertion d’un nom vide. Fichier impacté :
  `LoginWindow.xaml.cs`. Tests : **78/78 tests .NET verts**.

- **2026-09-18** : Ajout du `NumericKeypad` au `LoginWindow` et de sa commande
  de saisie pour préparer les tests UIA du shell administrateur. Fichiers impactés :
  `LoginWindow.xaml`, `LoginWindow.xaml.cs`. Tests : validation en cours.
- **2026-09-18** : Le pavé du login cible maintenant le `PasswordBox` actif,
  y compris la confirmation de première installation. Fichiers impactés :
  `LoginWindow.xaml`, `LoginWindow.xaml.cs`. Tests : validation en cours.
- **2026-09-18** : Import WPF ajouté pour compiler le suivi du champ de mot de
  passe actif. Fichier impacté : `LoginWindow.xaml.cs`. Tests : build en cours.
- **2026-09-18** : Préparation du shell administrateur UIA : rôle
  `Administrateur` affiché dans l'accueil, libellé `Répertoire` ajouté à la
  modale de réindexation et sélecteurs du test alignés sur les contrôles WPF.
  Fichiers impactés : `AccueilView.xaml`, `ReindexerModal.xaml`,
  `test_admin_ui.py`. Tests : validation en cours.
- **2026-09-18** : Guide écran créé avec la section **Tests Admin** : prérequis,
  variables d'environnement, six scénarios UIA et ordre CI. Fichier impacté :
  `postek/docs/GUIDE_TESTS_ECRAN.md`. Tests : build WPF réussi, 74/74 tests
  .NET verts, syntaxe Python UIA validée.
- **2026-09-18** : `test_admin_ui.py` ajouté au job `tests-ecran` après les
  tests F2/mixte/clavier, avec timeout CI global de 10 minutes. Fichier impacté :
  `.github/workflows/ci.yml`. Tests : validation locale ciblée en cours ; les
  74 tests .NET restent verts.
- **2026-09-18** : Le premier essai UIA a détecté le login mais pas le champ
  identifiant ; des `AutomationProperties.Name` explicites ont été ajoutés aux
  champs identifiant/mot de passe/confirmation. Fichier impacté :
  `LoginWindow.xaml`. Tests : nouveau passage UIA en cours.
- **2026-09-18** : Le dump UIA a confirmé que WPF expose le champ identifiant
  sous `TextBox` et non `Edit`; le sélecteur de `test_admin_ui.py` accepte
  désormais les deux classes. Fichier impacté : `test_admin_ui.py`. Tests :
  nouveau passage UIA en cours.
- **2026-09-18** : Le premier passage local a validé login, Gestion, Outils et
  l’ouverture de la modale ; l’assertion Réindexer attendait un ancien texte.
  Elle vérifie désormais le libellé réel `Répertoire`. Fichier impacté :
  `test_admin_ui.py`. Tests : passage UIA en cours.

- **2026-09-18** : Refonte Leo2/Postek étendue : design system centralisé,
  clavier tactile 4x3 avec animation et raccourcis physiques, vues Accueil,
  Gestion, Outils, Paramètres, Clôture, Périphériques, Paramètres Caisse,
  Réindexation et Mots de passe. Ajout de `SystemInfoService`,
  `PrinterService`, `SerialPortService`, `DatabaseMaintenanceService`,
  `UsbKeyService` et `ClotureService`. Suite .NET : **74 tests verts**.
- **2026-09-18** : Outils et périphériques raccordés en MVVM : ajout de
  `OutilsViewModel`, `ParametresPeripheriquesViewModel`, `ReindexerViewModel` et
  `MotsDePasseViewModel`. Réindexation asynchrone avec statut, gestion USB
  (lecteurs/écriture/effacement), RAZ avec double confirmation, informations
  système, ports COM, tiroir et découverte des imprimantes Windows. Navigation
  Back-Office vers les deux vues via `NavigationService`, retour fonctionnel.
- **2026-09-18** : Authentification et gestion humaine : PBKDF2-SHA256,
  rôles Admin/Caissier, tables `Utilisateurs`/`AuditLog`, sessions de caisse,
  écran d'ouverture avec fonds initial, Back-Office utilisateurs/audit et
  routage administrateur vers `AccueilWindow`.
- **2026-09-18** : Impression thermique 80 mm enrichie : identité société,
  matricule fiscal, sous-total, timbre, Code 128, modes de règlement et
  gestion d'erreur avec repli `imprimante.logique`.

- **2026-09-17** : **CI ENTIÈREMENT VERTE (run n°13) — tests-ecran OK au
  premier tour réel sur runner GitHub.** Enquête pour y arriver : (1) fix
  UnicodeEncodeError keygen (LC_ALL=C reproduit localement ; reconfigure +
  PYTHONUTF8) — insuffisant, l'hypothèse ASCII n'était pas la cause racine ;
  (2) job passe à Python 3.12 épinglé + étape if:failure() qui remonte le
  traceback dans les **annotations** du check-run (seul canal lisible sans
  auth) → cause trouvée : `test_migration_complete` 0≠2 sur PRODUITS —
  **_fichier_table ne gérait pas la casse mixte** (export « Produits.csv » ;
  fs Windows insensible masquait, Linux strict renvoyait 0 ligne) → balayage
  insensible à la casse dans export_reader (vrai bug de portabilité, aurait
  touché un export client réel sur serveur Linux). NB : pas de logs sans auth
  sur dépôt public non connecté ; Docker/WSL absents du poste (pas de
  reproduction Linux locale possible) — les annotations CI sont le canal de
  diagnostic retenu.
- **2026-09-17** : **Dépôt public + premier diagnostic CI réel**. Dépôt passé
  public (minutes Windows gratuites, API lisible) — suivi du run n°10 :
  rebranding ✅ mais **Noyau Python ❌** (exit 1 en ~1 s) alors que 33/33
  locaux verts. Reproduit localement : `LC_ALL=C` → UnicodeEncodeError sur
  les `print` accentués de `keygen.py` (« clé privée… À CONSERVER HORS
  LIGNE ») ; ubuntu-latest n'a pas de locale UTF-8 pour l'agent. Fix double :
  `sys.stdout/stderr.reconfigure(errors="replace")` dans `main()` (jamais
  planter sur l'affichage) + `PYTHONUTF8: "1"` en env global CI. Vérifié en
  conditions d'échec reproduites : 33/33 OK, exit 0. NB : le job tests-ecran
  (windows-latest) n'est pas concerné — le test clavier y écrivait déjà
  `reconfigure(encoding="utf-8", errors="replace")`. Docs supprimées du dépôt
  archivées sur disque dans `docs_archives/` (hors versionnement, whitelist
  .gitignore).
- **2026-09-17** : **Durcissement tests UIA pour le runner GitHub** (analyse
  risques avant premier tour CI) : détection fenêtres en boucle patiente
  (`attendre_fenetre_principale`, timeout `POSTEK_ATTENTE_APP` 30 s) au lieu
  d'un unique sondage à 2 s ; repli « fenêtres non visibles incluses » partout
  (session runner non interactive) ; helper `attendre_fermeture` (les asserts
  de fermeture sonnent en boucle, plus de one-shot) ; pas 3 du test F2
  converti à `attendre_fenetre` (le one-shot a réellement cassé en répétition
  locale à 2 s de warmup) ; CI : étapes diagnostic (`if: failure()` : liste
  processus + résolution écran) et artefact `diagnostic-tests-ecran`
  (imprimante.logique + postek_demo.db). Répétition générale locale Release
  verte après durcissement (4/4 + mixte + 3/3, warmup 2 s). Dépôt poussé :
  github.com/saznote86/PosTek (privé — API non authentifiée 404, suivi CI à
  faire côté web ou après passage public).
- **2026-09-17** : **Tests UIA intégrés à la CI** (job `tests-ecran` de
  `.github/workflows/ci.yml`, windows-latest, `needs: tests-dotnet`) : build
  Release → lancement app par PowerShell `Start-Process` (survit au pas, pas
  de -WindowStyle Hidden) → `test_f2_ui.py` + `test_mix_ui.py` +
  `test_clavier_ui.py` → arrêt `if: always()`. Répétition générale locale en
  Release OK (4/4 + mixte OK + 3/3, ticket mixte cb 1.000 + especes 1.000
  vérifié dans la base Release de l'app). Au passage : `test_clavier_ui.py` résout désormais la base
  démo **depuis le processus lancé** (fenêtre → PID → exe → db voisine) et non
  plus par heuristique Debug/Release — bug attrapé en répétition (le test
  relisait la base Debug pendant que l'app Release écrivait la sienne) ;
  surcharge `POSTEK_DEMO_DB` possible. Doc : § « Intégration continue » dans
  GUIDE_TESTS_ECRAN.md.
- **2026-09-17** : **Test UIA raccourcis clavier paiement 3/3 à l'écran**
  (`postek/tools/test_clavier_ui.py`, nouveau) : Échap referme la fenêtre et
  **rien en base** (comptage tickets avant/après, base lue en lecture seule
  `file:…?mode=ro` avec retries verrou) ; Entrée **ignorée** si non soldé
  (fenêtre reste ouverte) ; Entrée **soldée = valide** pour de vrai : +1 ticket
  en base, `reglement_ticket` = ("cb", "1.800"). Helpers partagés ajoutés à
  `test_mix_ui.py` (hwnd_fenetre_titre, champ_saisie, texte_total_principal) et
  `find_by_name` durci (retries sur COMError 0x80040201 pendant refresh WPF).
  Doc : `docs/GUIDE_TESTS_ECRAN.md` (pré-requis, scripts, dépannage) + pointeurs
  dans README et GUIDE_ENCAISSEMENT. Rejoué F2 (4/4) et clavier (3/3) dans la
  même session app : verts. Note infra de test : l'app lancée depuis bash meurt
  avec le shell — lancer par PowerShell `Start-Process` (pas de -WindowStyle
  Hidden, qui cache la fenêtre WPF).
- **2026-09-17** : **Première migration de données réelles** Leo2 → POSTEK :
  `postek/tools/extraire_produits_xls.py` convertit `produits.XLS` (export natif
  Leo2, 78 produits / 12 familles) → `export_leo2/tables/` (CSV + manifest SHA-256)
  → moteur Python → `postek/donnees/postek_donnees.db`. Résultat : **76/78 produits**
  (2 rejets = code-barres « 101 » dupliqué dans la source), 0 orphelin, TVA 59/14/2/1.
  Deux bugs de mapping attrapés : source `CODETVa` jamais matchée → corrigée (le
  CSV de test reproduisait la coquille) ; CODBAR vide → `trim` (NULL) sinon collision
  UNIQUE. Libellés famille provisoires (« Famille N ») : les .FIC sont chiffrés —
  attendre l'export famille natif du client. Dernier Z confirmé : n° 519 (17/04/2024).
  Données réelles hors dépôt (.gitignore : export_leo2/, postek/donnees/).
- **2026-09-17** : Ligne **RENDU dans le rapport Z** imprimé : `RecapZ.MonnaieRendue`
  dérivé (TotalRegle − TotalTtc, jamais stocké) → `DonneesRapportZ.MonnaieRendue` →
  ligne RENDU en gras avant les totaux, seulement si > 0. Vérifié à l'écran : Z n° 8
  (mixte 1,800 : CB 1,000 + espèces 1,000) imprime `ESPECES 1,000 / CB 1,000 /
  RENDU 0,200`. + 3 tests (RapportZ rendu/absent, RecapZ dérivé) → 63.
- **2026-09-17** : Raccourci **F2 = encaisser** (MainWindow.OnPreviewKeyDown).
  App rendu par le test UIA : **bug trouvé** — 1ʳᵉ version appelait OuvrirPaiement()
  direct et ouvrait le paiement sur ticket vide ; corrigé en routant via
  SurEncaisser (garde ticket vide). Vérifié à l'écran 4/4 (test_f2_ui.py) :
  vide ignoré / total 1,800 / F2 ouvre l'encaissement / Retour referme.
- **2026-09-17** : **Encaissement mixte vérifié à l'écran** (UI Automation,
  `postek/tools/test_mix_ui.py`, nécessite `pip install comtypes`) : ticket n° 13
  café 1,200 + croissant 0,600 = 1,800 TND → CB 1,000 + espèces 1,000, **RENDU 0,200**.
  Vérifié en base : `reglement_ticket` pos1 `cb` 1.000, pos2 `especes` 1.000 ; flux
  ticket dans `imprimante.logique` avec lignes CB/ESPECES/RENDU. Z n° 6 clôturée
  dans la foulée (1 ticket, 1,800) → **rapport Z imprimé automatiquement** vérifié
  dans le flux (volumétrie, TVA 19/13 %, ESPECES + CB, TOTAL TTC). Scénario UI :
  clics InvokePattern purs (pas de souris), fenêtre repérée par contenu, total
  vérifié (1,800) avant d'encaisser.
- **2026-09-17** : Test d'intégration **encaissement mixte espèces + CB avec rendu**
  (BaseDonneesTests, 60 tests) : cœur métier → persistance → ticket ESC/POS → récap Z.
  Vérifié aussi sur la vraie base démo (lecture seule) : 9 tickets, 2 Z clôturés,
  cohérence reçu−TTC=rendu OK partout — mais **aucun paiement mixte réel** encore
  (tout est mono-mode) : le clic à l'écran reste à faire.
  Bug doc corrigé au passage : `RecapZ.TotalRegle` = montants REÇUS (TTC + rendu),
  pas TTC — commentaire corrigé dans ClotureZ.cs.
- **2026-09-17** : **Commit initial git** `91121dc` — postek/ + start.bat + CI only ;
  .gitignore réécrit en **whitelist** (`/*` puis `!/postek/` etc.) : racine Leo2 exclue
  d'office (FIC/ndx/exe/BMP…), context.md non versionné (choix utilisateur).
- **2026-09-17** : Raccourcis clavier FenetrePaiement via `OnPreviewKeyDown` :
  Entrée = valider (si solde), Échap = annuler ; touche marquée traitée
  (pas de double-déclenchement si un bouton a le focus).
- **2026-09-17** : Impression **automatique du rapport Z** à la clôture :
  `TicketEscPos.GenererRapportZ` (DonneesRapportZ : volumétrie, récap TVA par
  taux, règlements par mode, totaux) + `MainWindow.ImprimerRapportZ` appelé après
  `CloturerZ` ; échec d'impression non bloquant (clôture déjà persistée).
  + 2 tests (TicketEscPosTests → 59).
- **2026-09-17** : Doc utilisateur `postek/docs/GUIDE_ENCAISSEMENT.md`
  (ticket → paiement multi-règlement → impression → clôture Z).
- **2026-09-17** : Multi-règlement complet (cœur + persistance + UI tactile + impression),
  tests impression (TicketEscPosTests 9, ImprimanteBruteTests 4), CI GitHub Actions
  (3 jobs : python, dotnet Release, garde rebranding), `.gitignore` complet.
- **2026-09-17** : Fix crash démarrage WPF — App.OnStartup injectait des `Color` ;
  remplacées par `SolidColorBrush` gelés + ajout `CouleurAlerte`/`CouleurSucces`.
- **2026-09-17** : Fix affichage taux TVA `TVA 0.19%` → `TVA 19%` (×100 à l'impression
  et dans le détail Z).
- **2026-09-17** : Fix pavé paiement — la touche `,` n'activait pas le mode décimal.
- **2026-09-17** : Création `start.bat` (racine) : anti-double-lancement, build si besoin,
  passe les arguments à l'exe. Testé : lance l'app, garde anti-doublon OK.
- **2026-09-17** : Session de test écran : 4 tickets encaissés, Z n°1 clôturée
  (19,450 TND = 13,500 espèces + 5,950 ticket resto, cohérence vérifiée en base).

## Historique récent

- 2026-09-19 : Correction RAZ catalogue engagée : événement `TablesVidees`, alias `ViderTablesAsync` et liste métier incluant `article`/`produit` ajoutés. Tests : validation en cours.
- 2026-09-19 : Invalidation du clavier tactile ajoutée : `CaisseViewModel.RechargerProduits()` et abonnement de `MainWindow` à `TablesVidees` pour reconstruire familles/grille et vider le ticket sans redémarrage. Tests : validation en cours.
- 2026-09-19 : Liste RAZ alignée sur les noms de tables autorisés du schéma (`SessionsCaisse`/`RapportsZ`), sans alias inexistants. Tests : 92/94 lors du diagnostic intermédiaire, correction en cours.
- 2026-09-19 : Test RAZ complété pour vérifier explicitement le vidage de `produit`; notification `TablesVidees` déplacée après réactivation des clés étrangères et hors bloc transactionnel. Tests : validation finale en cours.
- 2026-09-19 : Diagnostic Fiche Produit démarré. Le bouton Valider possède déjà son handler/DialogResult, mais la modale et le CRUD ne transportaient pas Famille/TVA/Imprimante. Migration additive ajoutée pour `article.imprimante_id`, `taux_tva` et `imprimante`, avec taux NACEF et imprimantes par défaut. Tests : validation en cours.
- 2026-09-19 : Modale Produit enrichie avec Famille, TVA fractionnaire et Imprimante active ; validation explicite du code/désignation/prix et transfert des valeurs dans `ArticleEdition`. Tests : validation en cours.
- 2026-09-19 : CRUD et DataGrid Produits enrichis par jointures Famille/Imprimante, colonnes Famille/TVA/Imprimante et audit des nouveaux champs. `Article` transporte désormais le routage imprimante et `CaisseService.GrouperParImprimante` fournit le regroupement métier. Tests : compilation WPF réussie, suite complète à exécuter.
- 2026-09-19 : Ajout de tests de persistance Famille/TVA/Imprimante, valeurs par défaut NACEF, jointure d’affichage et regroupement de routage. Tests : suite complète à exécuter.
- 2026-09-19 : Validation technique terminée pour la fiche Produit : build réussi et 99/99 tests réussis. Avertissement xUnit préexistant conservé ; test manuel WPF et validation sur imprimantes physiques restent à effectuer.
- 2026-09-19 : Nettoyage final du regroupement d’impression : clé entière `0` réservée au groupe sans imprimante, suppression de l’avertissement de nullabilité introduit par la correction. Tests : dernier build/test fonctionnel 99/99 ; recompilation finale requise après ce nettoyage mineur.

## Bugs connus

- Aucun bug bloquant connu.
- Lecture directe des `.FIC` chiffrés non disponible sans DLL WinDev locale ; le
  moteur détecte ces fichiers et l’assistant guide vers un export CSV/XLS.
- Le dossier réel ne contient pas d’exports clients, fournisseurs, utilisateurs,
  mouvements, ventes ou familles ; seules les données produit du XLS ont pu être
  importées automatiquement. Deux produits sont rejetés pour code-barres
  dupliqué dans la source.
- Le bouton Back-Office « Effacer l'historique des connexions » est branché
  (désactivation douce + audit) ; seule la validation manuelle à l'écran reste
  à faire (bloquée par `ConnexionDesactiveeProvisoirement = true`).
- `SerialPortService` expose actuellement un raccordement de façade : le test
  de port et l'état du tiroir produisent un statut métier, mais l'accès matériel
  réel devra être branché sur le protocole du poste client.
- La découverte des imprimantes Windows utilise le registre ; l'impression réelle
  continue de passer par le canal RAW ESC/POS validé.
- Avertissement xUnit préexistant dans `BaseDonneesTests.cs` (règle xUnit2013).
- Les neuf autres vues de Gestion restent à convertir des stubs vers leurs services métier et schémas SQLite réels.
- Les tests dédiés Clients/Vendeurs et les 15 tests minimum demandés restent à ajouter ; les boutons Clients/Vendeurs sont fonctionnels mais la couverture spécifique n'est pas encore livrée.

## Prochaines étapes (TODO)

- [ ] Finaliser la refonte visuelle de `MainWindow` (caisse) en conservant ses
  handlers de paiement, impression ESC/POS, clôture Z et routage métier.
- [ ] Ajouter/valider les assets locaux d’accueil et les tests spécifiques du
  nouveau shell (saint/date, raccourcis, panier, totaux et timbre).

- [x] Raccorder les vues Clôture/Périphériques/Outils aux commandes de
  navigation et de maintenance depuis le Back-Office.
- [x] Finaliser la clôture de session de caisse : fonds final, écart,
  `RapportsZ`, signature caissier, audit et impression du rapport Z.
- [ ] Remplacer la façade de `SerialPortService` par l'intégration matérielle
- [ ] Finaliser les ViewModels réels Comptabilité, Historique tickets, Clôtures, Ventilations/Tarifs, Claviers tactiles, Programmation avancée et Réglage date-heure.
- [ ] Ajouter les tests unitaires Clients/Vendeurs puis atteindre au moins 15 tests dédiés aux nouvelles vues.
  disponible sur le poste client.
- [ ] Ajouter des tests UIA pour le nouveau shell administrateur et le clavier
  tactile LoginView.
- [ ] Rejouer et valider 6/6 tests UIA Admin après correction de l’exposition
  `ContentControl`.
- [x] Ajouter le bouton Back-Office « Effacer l’historique des connexions ».
- [x] Ajouter l’assistant Back-Office de migration offline et le guide opératoire.
- [ ] Tester la migration complète sur une copie réelle du dossier source avec
  les dépendances `xlrd`/`openpyxl` installées si des XLS/XLSX sont présents.
- [x] Analyser et migrer la copie réelle `postek/ATOO_LEO2` vers une base de test.
- [ ] Obtenir des exports natifs complémentaires pour clients, fournisseurs,
  utilisateurs, mouvements, ventes et familles, puis rejouer la migration.
- [ ] Valider visuellement la base réelle dans l’application ; aucun utilisateur
  migré n’est disponible pour ouvrir une session automatiquement.
- [ ] Vérifier l'affichage sur une résolution 1024x768 et sur une imprimante
  thermique réelle.

- [x] Test écran des raccourcis clavier (Entrée/Échap) : fait, 3/3
      (`postek/tools/test_clavier_ui.py`), doc `docs/GUIDE_TESTS_ECRAN.md`.
- [x] Surveiller le premier tour vert du job CI `tests-ecran` sur GitHub :
      **fait — run n°13 vert (4/4 jobs)** ; les R1–R3 (attente active, repli
      hors WS_VISIBLE, artefacts diagnostic) ont tenu.
- [ ] Épingler `3.12` → envisager une matrice (3.12 + latest) quand le
      noyau sera stabilisé ; lever l'étape diagnostic CI devenue skippée
      (utile à conserver pour le prochain imprévu).
- [ ] Vérifier le rapport Z imprimé sur l'imprimante réelle (le flux dans
      `imprimante.logique` est déjà validé).
- [ ] Pousser vers un dépôt distant (GitHub) quand le CI devra tourner
      (4 commits locaux : `91121dc` initial, `39fdb84` encaissement,
      `0c7e7f9` F2 + RENDU rapport Z, `3d69ef5` migration XLS produits).

- **2026-09-19 — Rebranding et validation technique** : sources desktop et tests renommées vers Postec, thème postec.json, base runtime postec_demo.db, sauvegardes postec_backup_*; MainWindow refondue avec catalogue/panier et navigation Caisse/Gestion/Outils/Quitter, raccourcis F1/F2/F3/F8/F9/Échap, sans action F12. Build WPF validé; suite xUnit relancée (résultat final à confirmer).
