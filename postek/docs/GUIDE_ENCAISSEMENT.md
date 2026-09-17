# Encaisser avec POSTEK — du ticket au rapport Z

Parcours complet d'encaissement sur la caisse tactile POSTEK (application
desktop .NET/WPF). Lancement : `start.bat` à la racine du projet.

---

## 1. Constituer le ticket

- Sur l'écran principal, touchez les articles du clavier : chaque touche
  ajoute une ligne au ticket en cours.
- **Vider** annule le ticket en cours et repart de zéro.
- Le total TTC et le détail TVA (HT / TVA) s'affichent en permanence
  en bas d'écran ; ils sont recalculés à chaque modification.

> La TVA est incluse dans les prix affichés (TTC). Le découpage HT/TVA
> se fait par taux (19 %, 13 %, 7 %, 0 %) au moment de l'encaissement.

## 2. Ouvrir l'encaissement

Touchez **Encaisser** : la fenêtre de paiement s'ouvre sur le ticket en cours.

- À gauche : le montant **à payer**, le **reste à payer** après chaque
  règlement saisi, et la liste des règlements déjà saisis (supprimables
  un à un avec ✕).
- À droite : le pavé numérique et les modes de règlement.

Raccourcis clavier (utile avec un clavier branché) :

| Touche  | Action                |
|---------|-----------------------|
| `Entrée`| Valider l'encaissement|
| `Échap` | Annuler et revenir    |

## 3. Saisir les règlements (multi-paiement)

Un ticket peut être réglé avec **plusieurs modes** : saisissez un montant
au pavé puis touchez un mode. Règles appliquées :

- **Espèces** : peuvent dépasser le total — la différence devient le
  **rendu monnaie**, plafonné aux espèces réellement reçues.
- **CB, chèque, ticket resto, avoir** : plafonnés au reste à payer
  (pas de rendu monnaie sur ces modes).
- Toucher un mode **sans rien saisir** règle exactement le reste à payer
  (confort tactile).
- La touche `,` active la saisie des millimes (3 décimales) ; `C` efface
  la saisie en cours ; `⌫` efface le dernier chiffre.

Le bouton **Valider l'encaissement** ne s'active que lorsque le ticket
est soldé. Tant qu'il ne l'est pas :

- **← Retour** (ou `Échap`, ou la fermeture de la fenêtre) annule tous
  les règlements saisis — le ticket reste intact, rien n'a été compté.

## 4. Validation et impression du ticket

À la validation :

1. Le ticket est **enregistré** dans la base locale (lignes, récap TVA,
   règlements) — avec son numéro définitif.
2. Le flux **ESC/POS 80 mm** est envoyé à l'imprimante configurée
   (impression en mode « texte brut »). Le ticket comporte :
   - en-tête de la marque, numéro, date/heure, caisse et caissier ;
   - le détail des articles (quantité × prix unitaire, total) ;
   - le **récapitulatif TVA par taux** (exigence fiscale) ;
   - le détail des **règlements** et la ligne **RENDU** s'il y a lieu ;
   - le TOTAL et les mentions légales (MF, TVA, RC…).
3. Le compteur avance : le ticket suivant portera le numéro suivant.

> Si aucune imprimante n'est configurée, le flux est mis de côté
> (fichier `imprimante.logique`) et l'encaissement n'est **pas** annulé :
> l'écran signale simplement que le ticket est en attente d'impression.
> De même, une panne d'imprimante n'annule jamais un encaissement validé.

## 5. Clôture Z (fin de journée)

Touchez **Clôture Z** sur l'écran principal :

1. Un récapitulatif s'affiche : nombre de tickets, total TTC, détail HT /
   TVA par taux, totaux par mode de règlement.
2. **Confirmer** rattache tous les tickets de la période à la clôture
   (numéro Z incrémenté) et **imprime automatiquement le rapport Z**
   sur l'imprimante : volumétrie, récap TVA par taux, règlements par
   mode, totaux HT/TVA/TTC.
3. Un nouveau Z courant repart de zéro pour la journée suivante.

> Une clôture sans ticket est refusée. Un souci d'impression du rapport
> n'annule pas la clôture (déjà persistée) : un message le signale.

---

## Bon à savoir

- Les montants sont gérés en **décimal exact** (millimes) : pas
  d'arrondi flottant, l'affiché = le comptabilisé.
- Raccourci de test : `start.bat --test-impression [nom-imprimante]`
  envoie une page de test à l'imprimante indiquée.
