# Dictionnaire de données — Atoo Leo2 (analyse MINICASH.xdd)

> Généré automatiquement par `tools/parse_xdd.py` — 101 tables, 1115 rubriques. GenNum=162.

> ⚠️ Les codes de types non vérifiés sont affichés bruts (`code N`) ; le décodage complet sera confirmé en phase 2 par lectures réelles.

## ADDIMAT_ENVOI

(physique : `ADDIMAT_ENVOI`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDADDIMAT_ENVOI` | entier auto (clé) | 4 | clé unique |
| `DH_ENVOI` | code 34 | 8 | clé avec doublons |
| `TRAME` | texte | 100 |  |
| `NUM_VENDEUR` | code 9 | 2 |  |
| `NUM_PRODUIT` | code 26 | 4 |  |
| `QTE` | code 3 | 2 |  |
| `Status` | code 36 | 1 | clé avec doublons |

## ADDIMAT_RECEPTION

(physique : `ADDIMAT_RECEPTION`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDADDIMAT_RECEPTION` | entier auto (clé) | 4 | clé unique |
| `DH_ARRIVEE` | code 34 | 8 | clé avec doublons |
| `TRAME` | texte | 100 |  |
| `NUM_VENDEUR` | code 9 | 2 |  |
| `NUM_PRODUIT` | code 26 | 4 |  |
| `NUM_TABLEAU` | code 9 | 2 |  |
| `QTE` | code 3 | 2 |  |
| `Status` | code 36 | 1 | clé avec doublons |

## Bases_j

(abr. `BA`, physique : `bases_j`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `BACLEUNIK` | entier auto (clé) | 4 | clé unique |
| `type_base` | texte | 20 | clé avec doublons |
| `cle_base` | code 5 | 4 | clé avec doublons |
| `libelle` | texte | 35 |  |
| `ba_valeur` | code 17 | 10 |  |

## CACLI

(physique : `CACLI`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `C0CLEUNIK` | code 5 | 4 | clé unique |
| `CaTotal` | code 17 | 10 | clé avec doublons |
| `Solde` | code 17 | 10 | clé avec doublons |
| `Date_PCde` | code 14 | 8 | clé avec doublons |
| `Date_DCde` | code 14 | 8 | clé avec doublons |
| `Date_DReg` | code 14 | 8 | clé avec doublons |
| `Fidelite` | code 5 | 4 | clé avec doublons |
| `Monetaire1` | code 17 | 10 | clé avec doublons |
| `Monetaire2` | code 17 | 10 |  |
| `Numerique1` | code 5 | 4 | clé avec doublons |
| `Numerique2` | code 5 | 4 |  |
| `Texte1` | texte | 50 | clé avec doublons |
| `Texte2` | texte | 50 |  |

## CartePayeeHisto

(abr. `CH`, physique : `CartePayeeHisto`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDCartePayeeHisto` | entier auto (clé) | 4 | clé unique |
| `NO_CARTE` | texte | 25 | clé avec doublons |
| `Date_OP` | code 14 | 8 | clé avec doublons |
| `Heure_OP` | code 11 | 4 | clé avec doublons |
| `Type_OP` | code 5 | 4 | clé avec doublons |
| `No_Piece` | code 5 | 4 |  |
| `No_Caisse` | code 5 | 4 |  |
| `no_vendeur` | code 5 | 4 |  |
| `Montant_OP` | code 17 | 10 |  |

## CartePayée

(abr. `CA`, physique : `CartePayée`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDCartePayée` | entier auto (clé) | 4 | clé unique |
| `NO_CARTE` | texte | 25 | clé unique |
| `NO_CLIENT` | code 5 | 4 | clé avec doublons |
| `NOM` | texte | 30 | clé avec doublons |
| `PRENOM` | texte | 30 |  |
| `MOT_PASSE` | texte | 8 |  |
| `REGLE_MDP` | code 5 | 4 |  |
| `LIMITE_MDP` | code 17 | 10 |  |
| `PREM_DATE` | code 14 | 8 | clé avec doublons |
| `EXPIRE_DATE` | code 14 | 8 | clé avec doublons |
| `DERN_OP` | code 5 | 4 |  |
| `DERN_OP_DATE` | code 14 | 8 | clé avec doublons |
| `DERN_OP_MONTANT` | code 17 | 10 |  |
| `DEBIT_MAXI` | code 17 | 10 |  |
| `DEBIT_MAXI_OP` | code 17 | 10 |  |
| `CHSUP01` | texte | 20 |  |
| `CHSUP02` | code 5 | 4 |  |
| `CHSUP03` | code 17 | 10 |  |
| `Solde` | code 17 | 10 | clé avec doublons |

## CdeCli

(abr. `C2`, physique : `CdeCli`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `NumCommande` | entier auto (clé) | 4 | clé unique |
| `DateCommande` | code 14 | 8 | clé avec doublons |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `NO_COMPTE` | texte | 10 | clé avec doublons |
| `NOM` | texte | 40 | clé avec doublons |
| `ADR1` | texte | 30 |  |
| `ADR2` | texte | 30 |  |
| `COD_POST` | texte | 5 | clé avec doublons |
| `VILLE` | texte | 30 | clé avec doublons |
| `PcRemise` | code 5 | 4 | clé avec doublons |
| `TotalRemise` | code 17 | 10 | clé avec doublons |
| `TotalHT` | code 17 | 10 |  |
| `TotalTVA` | code 17 | 10 |  |
| `TotalTTC` | code 17 | 10 | clé avec doublons |
| `DateLivPrev` | code 14 | 8 | clé avec doublons |
| `DateLivraison` | code 14 | 8 | clé avec doublons |
| `IDModeReglement` | code 5 | 4 | clé avec doublons |
| `Observations` | code 29 | 8 |  |
| `EtatCommande` | code 5 | 4 | clé avec doublons |
| `Reg_Accompte` | code 5 | 4 | clé avec doublons |
| `Total_Accompte` | code 17 | 10 | clé avec doublons |
| `Origine` | code 5 | 4 | clé avec doublons |

## CdeFou

(abr. `C1`, physique : `CdeFou`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `NumCommande` | entier auto (clé) | 4 | clé unique |
| `DateCommande` | code 14 | 8 | clé avec doublons |
| `NumFournisseur` | code 5 | 4 | clé avec doublons |
| `TotalHT` | code 17 | 10 |  |
| `TotalTVA` | code 17 | 10 |  |
| `TotalTTC` | code 17 | 10 | clé avec doublons |
| `EtatCommande` | code 5 | 4 | clé avec doublons |
| `Observations` | code 29 | 8 |  |

## Clavier

(abr. `CL`, physique : `clavier`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `CLCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `tchong` | texte | 20 | clé unique |
| `cltype` | texte | 1 |  |
| `clvaleur` | texte | 25 | clé avec doublons |
| `raccourci_clavier` | texte | 3 | clé avec doublons |

## CLIENTCOMPL

(abr. `CC`, physique : `CLIENTCOMPL`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDCLIENTCOMPL` | entier auto (clé) | 4 | clé unique |
| `NO_CLIENT` | code 5 | 4 | clé avec doublons |
| `TITRE` | texte | 6 |  |
| `Nationnalité` | texte | 4 |  |
| `Pays` | texte | 40 |  |
| `Langue` | texte | 4 |  |
| `GROUPE_CLI` | code 5 | 4 |  |
| `INFO_CB` | texte | 50 |  |
| `GSM` | texte | 20 | clé avec doublons |
| `Fax` | texte | 20 |  |
| `WEB` | texte | 40 |  |
| `CHSUP01` | texte | 20 |  |
| `CHSUP02` | code 5 | 4 |  |
| `CHSUP03` | code 17 | 10 |  |
| `commentaire` | texte | 50 |  |

## CLIENTS

(abr. `C0`, physique : `CLIENTS`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `C0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `NO_COMPTE` | texte | 10 | clé avec doublons |
| `NOM` | texte | 30 | clé avec doublons |
| `PRENOM` | texte | 30 |  |
| `SEXE` | code 12 | 1 |  |
| `ADR1` | texte | 30 |  |
| `ADR2` | texte | 30 |  |
| `COD_POST` | texte | 5 | clé avec doublons |
| `VILLE` | texte | 30 |  |
| `TELEPHONE` | texte | 20 | clé avec doublons |
| `MAIL` | texte | 35 |  |
| `DATE_NAIS` | code 14 | 8 | clé avec doublons |
| `DATE_FETE` | code 14 | 8 |  |
| `LIBRE1` | texte | 300 |  |
| `LIBRE2` | texte | 300 |  |
| `LIBRE3` | texte | 300 |  |
| `LIBRE4` | texte | 300 |  |
| `LIBRE5` | texte | 1000 |  |
| `MEMO` | code 29 | 8 |  |
| `HISTO` | texte | 100 |  |
| `TARIF` | code 3 | 2 |  |
| `MEMO_TICKET` | texte | 500 |  |
| `CHSUP1` | texte | 10 |  |
| `CHSUP2` | code 5 | 4 |  |
| `CHSUP3` | texte | 20 |  |

## CODPOST

(abr. `CO`, physique : `CODPOST`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `codep` | texte | 21 | clé avec doublons |
| `nomville` | texte | 35 |  |

## Comptes

(abr. `CP`, physique : `comptes`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `CPCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `nom_cpt` | texte | 25 |  |
| `code_cpt` | texte | 10 | clé unique |

## Doseur_j

(abr. `D0`, physique : `doseur_j`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `D0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `heure` | texte | 6 | clé avec doublons |
| `terminal` | code 36 | 1 | clé avec doublons |
| `doseur` | code 36 | 1 | clé avec doublons |
| `num_histo` | code 5 | 4 | clé avec doublons |
| `cleprod` | code 5 | 4 | clé avec doublons |
| `num_caisse` | code 36 | 1 | clé avec doublons |

## Facture

(abr. `FC`, physique : `Facture`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `NumFacture` | entier auto (clé) | 4 | clé unique |
| `DateFacture` | code 14 | 8 | clé avec doublons |
| `NumCommande` | code 5 | 4 | clé avec doublons |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `NO_COMPTE` | texte | 10 | clé avec doublons |
| `NOM` | texte | 40 | clé avec doublons |
| `ADR1` | texte | 30 |  |
| `ADR2` | texte | 30 |  |
| `COD_POST` | texte | 5 | clé avec doublons |
| `VILLE` | texte | 30 | clé avec doublons |
| `PcRemise` | code 5 | 4 |  |
| `TotalRemise` | code 17 | 10 | clé avec doublons |
| `TotalHT` | code 17 | 10 |  |
| `TotalTVA` | code 17 | 10 |  |
| `TotalTTC` | code 17 | 10 | clé avec doublons |
| `Observations` | code 29 | 8 |  |
| `vendeur` | code 25 | 8 | clé avec doublons |
| `EtatFacture` | code 5 | 4 | clé avec doublons |
| `Reg_Accompte` | code 5 | 4 | clé avec doublons |
| `Total_Accompte` | code 17 | 10 |  |
| `IDReglement` | code 5 | 4 | clé avec doublons |
| `Origine` | code 5 | 4 | clé avec doublons |

## Famille

(abr. `FA`, physique : `famille`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `FACLEUNIK` | entier auto (clé) | 4 | clé unique |
| `NOM` | texte | 30 | clé avec doublons |
| `touche` | texte | 5 | clé avec doublons |
| `IDGROUPE` | code 5 | 4 | clé avec doublons |

## FICHETEC

(abr. `FI`, physique : `FICHETEC`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `FICLEUNIK` | entier auto (clé) | 4 | clé unique |
| `CLEPROD_FINI` | code 5 | 4 | clé avec doublons |
| `NOLIG` | code 3 | 2 | clé avec doublons |
| `cleprod` | code 5 | 4 | clé avec doublons |
| `QTE_CONV` | code 7 | 8 |  |
| `PRIX_HA` | code 17 | 10 |  |
| `FFTEC1` | code 5 | 4 |  |
| `FFTEC2` | texte | 20 |  |

## FormData

(physique : `FormData`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDFormData` | entier auto (clé) | 4 | clé unique |
| `FD_NoCaisse` | code 5 | 4 | clé avec doublons |
| `FD_NoVendeur` | code 5 | 4 | clé avec doublons |
| `FD_DateHeure` | code 34 | 8 | clé avec doublons |
| `FD_IDFormulaire` | texte | 50 | clé avec doublons |
| `FD_NumTicket` | code 5 | 4 |  |
| `FD_Etat` | texte | 3 | clé avec doublons |
| `FD_NoClient` | code 5 | 4 |  |
| `FD_DataBrut` | code 29 | 8 |  |
| `FD_PRCLEUNIK` | code 26 | 4 |  |
| `FD_QTE` | code 6 | 4 |  |

## FormDataNew

(physique : `FormDataNew`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDFormData` | entier auto (clé) | 4 | clé unique |
| `FD_NoCaisse` | code 5 | 4 | clé avec doublons |
| `FD_NoVendeur` | code 5 | 4 | clé avec doublons |
| `FD_DateHeure` | code 34 | 8 | clé avec doublons |
| `FD_IDFormulaire` | texte | 50 | clé avec doublons |
| `FD_NumTicket` | code 5 | 4 |  |
| `FD_Etat` | texte | 3 | clé avec doublons |
| `FD_NoClient` | code 5 | 4 |  |
| `FD_DataBrut` | code 29 | 8 |  |
| `FD_PRCLEUNIK` | code 26 | 4 |  |
| `FD_QTE` | code 6 | 4 |  |
| `FD_GROUPE` | code 26 | 4 |  |
| `FD_PRIX` | code 17 | 10 |  |
| `FD_LIBRE_TXT1` | texte | 25 |  |
| `FD_LIBRE_TXT2` | texte | 25 |  |
| `FD_LIBRE_MON1` | code 17 | 10 |  |
| `FD_LIBRE_MON2` | code 17 | 10 |  |
| `FD_FACLEUNIK` | code 26 | 4 |  |

## FOURNISSEURS

(abr. `FO`, physique : `FOURNISSEURS`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `NumFournisseur` | entier auto (clé) | 4 | clé unique |
| `Societe` | texte | 40 | clé avec doublons |
| `Civilite` | texte | 5 |  |
| `Nom` | texte | 40 | clé avec doublons |
| `Adresse1` | texte | 40 |  |
| `Adresse2` | texte | 40 |  |
| `CodePostal` | texte | 5 | clé avec doublons |
| `Ville` | texte | 40 |  |
| `Pays` | texte | 40 |  |
| `Telephone` | texte | 20 |  |
| `GSM` | texte | 20 | clé avec doublons |
| `Fax` | texte | 20 |  |
| `EMail` | texte | 40 |  |
| `Observations` | code 29 | 8 |  |

## GenAnnonces

(physique : `GenAnnonces`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDGenAnnonces` | entier auto (clé) | 4 | clé unique |
| `AN_DATE_CREATION` | code 34 | 8 |  |
| `AN_DATE_VALIDITE` | code 14 | 8 | clé avec doublons |
| `AN_NOM_CONTACT_1` | texte | 50 |  |
| `AN_NOM_CONTACT_2` | texte | 50 |  |
| `AN_TELEPHONE` | texte | 20 |  |
| `AN_MAIL` | texte | 125 |  |
| `AN_HORAIRES` | texte | 50 |  |
| `AN_TEXTE` | code 29 | 8 |  |
| `AN_IMAGE` | texte | 250 |  |
| `AN_DIVERS_1` | texte | 50 |  |
| `AN_DIVERS_2` | texte | 50 |  |
| `AN_NUM_CLIENT` | code 27 | 8 |  |

## GenImpression

(physique : `GenImpression`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDGenImpression` | entier auto (clé) | 4 | clé unique |
| `IMP_GEN` | code 37 | 1 | clé avec doublons |
| `IMP_Libelle` | texte | 50 | clé avec doublons |
| `IMP_Data` | code 29 | 8 |  |
| `IMP_LAST_MODIF` | code 34 | 8 |  |
| `IMP_CREATION` | code 34 | 8 |  |
| `IMP_Type` | texte | 8 | clé avec doublons |

## GLORY_TRANSACTION

(physique : `GLORY_TRANSACTION`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDGLORY_TRANSACTION` | entier auto (clé) | 4 | clé unique |
| `DATE_HEURE` | code 34 | 8 | clé avec doublons |
| `num_caisse` | code 9 | 2 | clé avec doublons |
| `no_vendeur` | code 5 | 4 | clé avec doublons |
| `montant` | code 17 | 10 |  |
| `Detail` | texte | 100 |  |

## GLORYTRACE

(physique : `GLORYTRACE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDGLOYTRACE` | entier auto (clé) | 4 | clé unique |
| `ID_USER` | code 26 | 4 | clé avec doublons |
| `GL_DATEHEURE` | code 34 | 8 | clé avec doublons |
| `GL_OPERATION` | texte | 50 | clé avec doublons |
| `GL_TYPE` | texte | 50 |  |
| `GL_NOMUSER` | texte | 50 |  |
| `GL_DETAIL_1` | code 29 | 8 |  |
| `GL_DETAIL_2` | texte | 50 |  |
| `GL_XML` | code 29 | 8 |  |

## GROUPE

(physique : `GROUPE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDGROUPE` | entier auto (clé) | 4 | clé unique |
| `GRNOM` | texte | 30 |  |

## Histo_j

(abr. `HI`, physique : `histo_j`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `HICLEUNIK` | entier auto (clé) | 4 | clé unique |
| `heure` | texte | 6 | clé avec doublons |
| `duree` | code 11 | 4 |  |
| `NUMERO` | code 5 | 4 | clé avec doublons |
| `cleprod` | code 5 | 4 | clé avec doublons |
| `prixu_ttc` | code 17 | 10 |  |
| `qte` | code 7 | 8 |  |
| `tva` | code 5 | 4 |  |
| `vendeur` | code 5 | 4 | clé avec doublons |
| `ventillation` | code 5 | 4 | clé avec doublons |
| `offert` | code 36 | 1 | clé avec doublons |
| `offert_perso` | code 36 | 1 | clé avec doublons |
| `num_caisse` | code 36 | 1 | clé avec doublons |
| `code_cpt` | texte | 10 | clé avec doublons |
| `no_table` | texte | 6 |  |
| `nb_couv` | code 3 | 2 |  |
| `remise` | code 17 | 10 |  |

## HISTOCLI

(abr. `H0`, physique : `HISTOCLI`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `H0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `CLCLEUNIK` | code 5 | 4 | clé avec doublons |
| `NUMERO` | code 5 | 4 | clé avec doublons |
| `DATE_TICKET` | texte | 20 | clé avec doublons |
| `NOREGL` | code 5 | 4 |  |
| `TOTALREG` | code 17 | 10 |  |
| `LETTRAGE` | texte | 3 | clé avec doublons |

## HTL_EVENT

(abr. `HT`, physique : `HTL_EVENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `HR_CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `HR_DATE_DEB` | code 14 | 8 | clé avec doublons |
| `HR_MA_DEB` | code 36 | 1 |  |
| `HR_DATE_FIN` | code 14 | 8 | clé avec doublons |
| `HR_MA_FIN` | code 36 | 1 |  |
| `HR_DATE_RESA` | code 14 | 8 |  |
| `HR_MONTANT_ACCOMPTE` | code 17 | 10 |  |
| `HR_COULEUR` | code 25 | 8 |  |
| `HR_REMARQUE` | code 29 | 8 |  |
| `HR_NOMBRE` | code 9 | 2 |  |
| `HR_NB_CAT1` | code 9 | 2 |  |
| `HR_NB_CAT2` | code 9 | 2 |  |
| `HR_NB_CAT3` | code 9 | 2 |  |
| `HR_NB_CAT4` | code 9 | 2 |  |
| `HR_ACCOMPTE` | code 37 | 1 |  |
| `HR_DIVERS` | code 29 | 8 |  |
| `HR_GROUPE` | code 9 | 2 | clé avec doublons |
| `HR_PAYE` | code 37 | 1 | clé avec doublons |
| `HR_ARRIVEE` | code 37 | 1 | clé avec doublons |
| `HR_OPTION` | code 37 | 1 |  |
| `HR_LIMITE_OPTION` | code 14 | 8 |  |
| `HR_LIB_CLIENT` | texte | 40 |  |
| `HR_CARTE_BLEU` | texte | 30 |  |
| `HR_NUM_FACTURE` | code 25 | 8 |  |
| `HC_CLEUNIK` | code 5 | 4 | clé avec doublons |
| `HB_CLEUNIK` | code 5 | 4 | clé avec doublons |
| `OptimCleComp_1` | code None |  | clé avec doublons |
| `HR_MODE_REGL` | code 5 | 4 |  |
| `HR_NO_TABLE` | code 5 | 4 | clé avec doublons |
| `HR_DIVERS_1` | texte | 40 |  |
| `HR_DIVERS_2` | code 5 | 4 |  |
| `HR_CLE_PERSO` | texte | 8 | clé avec doublons |

## HTLARTICLE

(abr. `AR`, physique : `HTLARTICLE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `ARCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `AR_CODE` | texte | 5 |  |
| `AR_LIBELLE` | texte | 40 |  |
| `AR_PRIX_TTC` | code 17 | 10 |  |
| `AR_QTE` | code 6 | 4 |  |
| `AR_REMISE` | code 17 | 10 |  |
| `AR_TVA` | code 6 | 4 |  |
| `AR_FAMILLE` | code 9 | 2 |  |
| `AR_DATE` | code 14 | 8 |  |
| `AR_HEURE` | code 11 | 4 |  |
| `AR_PAYE` | code 37 | 1 |  |
| `AR_USER_1` | texte | 20 |  |
| `AR_USER_2` | code 17 | 10 |  |
| `AR_USER_3` | code 17 | 10 |  |
| `HR_CLEUNIK` | code 5 | 4 | clé avec doublons |
| `AR_LIB_TARIF` | texte | 20 |  |
| `AR_COD_CPT` | texte | 12 |  |
| `AR_COD_VEND` | code 5 | 4 |  |
| `AR_TRAITE` | code 5 | 4 |  |
| `AR_COD_TARIF` | code 5 | 4 |  |
| `AR_COD_MENU` | texte | 20 |  |
| `AR_TRIFAM` | code 5 | 4 |  |
| `AR_REMISE_GES` | code 5 | 4 |  |
| `AR_NO_CHAMBRE_LEO` | texte | 10 |  |

## HTLCHAMBRE

(abr. `AP`, physique : `HTLCHAMBRE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `HB_CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `HB_LIBELLE` | texte | 40 | clé avec doublons |
| `HB_GROUPE` | code 9 | 2 | clé avec doublons |
| `HB_ARTICLES` | code 29 | 8 |  |
| `HB_DETAIL` | code 29 | 8 |  |
| `HB_PRIX_1` | code 17 | 10 |  |
| `HB_PRIX_2` | code 17 | 10 |  |
| `HB_PRIX_3` | code 17 | 10 |  |
| `HB_PRIX_4` | code 17 | 10 |  |
| `HB_PRIX_5` | code 17 | 10 |  |
| `HB_PRIX_6` | code 17 | 10 |  |
| `HB_CODE_1` | code 9 | 2 | clé avec doublons |
| `HB_CODE_2` | code 9 | 2 |  |
| `HB_LIT` | texte | 1 |  |
| `HB_DOUCHE` | texte | 1 |  |
| `HB_VUE` | texte | 3 |  |
| `HB_PICTO_1` | texte | 1 |  |
| `HB_PICTO_2` | texte | 1 |  |
| `HB_PICTO_3` | texte | 1 |  |
| `HB_USER_1` | texte | 30 |  |
| `HB_USER_2` | code 17 | 10 |  |
| `HB_USER_3` | code 14 | 8 |  |

## HTLRESA

(abr. `PE`, physique : `HTLRESA`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `HR_CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `HR_DATE_DEB` | code 14 | 8 | clé avec doublons |
| `HR_MA_DEB` | code 36 | 1 |  |
| `HR_DATE_FIN` | code 14 | 8 | clé avec doublons |
| `HR_MA_FIN` | code 36 | 1 |  |
| `HR_DATE_RESA` | code 14 | 8 |  |
| `HR_MONTANT_ACCOMPTE` | code 17 | 10 |  |
| `HR_COULEUR` | code 25 | 8 |  |
| `HR_REMARQUE` | code 29 | 8 |  |
| `HR_NOMBRE` | code 9 | 2 |  |
| `HR_NB_CAT1` | code 9 | 2 |  |
| `HR_NB_CAT2` | code 9 | 2 |  |
| `HR_NB_CAT3` | code 9 | 2 |  |
| `HR_NB_CAT4` | code 9 | 2 |  |
| `HR_ACCOMPTE` | code 37 | 1 |  |
| `HR_DIVERS` | code 29 | 8 |  |
| `HR_GROUPE` | code 9 | 2 | clé avec doublons |
| `HR_PAYE` | code 37 | 1 | clé avec doublons |
| `HR_ARRIVEE` | code 37 | 1 | clé avec doublons |
| `HR_OPTION` | code 37 | 1 |  |
| `HR_LIMITE_OPTION` | code 14 | 8 |  |
| `HR_LIB_CLIENT` | texte | 40 |  |
| `HR_CARTE_BLEU` | texte | 30 |  |
| `HR_NUM_FACTURE` | code 25 | 8 |  |
| `HC_CLEUNIK` | code 5 | 4 | clé avec doublons |
| `HB_CLEUNIK` | code 5 | 4 | clé avec doublons |
| `OptimCleComp_1` | code None |  | clé avec doublons |
| `HR_MODE_REGL` | code 5 | 4 |  |
| `HR_NO_TABLE` | code 5 | 4 | clé avec doublons |
| `HR_DIVERS_1` | texte | 40 |  |
| `HR_DIVERS_2` | code 5 | 4 |  |
| `HR_CLE_PERSO` | texte | 8 | clé avec doublons |

## HTLTRACE

(physique : `HTLTRACE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDHTLTRACE` | entier auto (clé) | 4 | clé unique |
| `ID_USER` | code 26 | 4 | clé avec doublons |
| `HT_DATEHEURE` | code 34 | 8 | clé avec doublons |
| `HT_OPERATION` | texte | 50 | clé avec doublons |
| `HT_TYPE` | texte | 50 |  |
| `HT_NOMUSER` | texte | 50 |  |
| `HT_DETAIL_1` | code 29 | 8 |  |
| `HT_DETAIL_2` | texte | 50 |  |
| `HT_NUM_RESA` | code 27 | 8 | clé avec doublons |

## LEOTRACE

(abr. `LT`, physique : `LEOTRACE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLEOTRACE` | entier auto (clé) | 4 | clé unique |
| `LT_DATEHEURE` | code 34 | 8 | clé avec doublons |
| `LT_NOCAISSE` | code 5 | 4 | clé avec doublons |
| `LT_VENDEUR` | code 5 | 4 | clé avec doublons |
| `LT_EVENT` | code 5 | 4 | clé avec doublons |
| `LT_DUREE` | code 5 | 4 | clé avec doublons |
| `LT_INFOTXT` | texte | 20 | clé avec doublons |
| `LT_DETAIL_1` | code 29 | 8 |  |
| `LT_DETAIL_2` | code 29 | 8 |  |

## Licence

(abr. `LI`, physique : `licence`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `LICLEUNIK` | entier auto (clé) | 4 | clé unique |
| `nom_lic` | texte | 35 |  |
| `nom_ticket` | texte | 35 |  |
| `nbr_poste` | code 36 | 1 |  |
| `num_disque` | texte | 500 |  |
| `date_valide` | code 14 | 8 |  |
| `Type_Licence` | code 5 | 4 |  |
| `LISUP1` | texte | 10 |  |

## LienFamilleMenu

(physique : `LienFamilleMenu`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLienFamilleMenu` | entier auto (clé) | 4 | clé unique |
| `Menu_Choix` | texte | 50 | clé avec doublons |
| `CodeFamille` | code 5 | 4 | clé avec doublons |
| `TypeTriAuto` | code 5 | 4 |  |
| `ChSup1` | texte | 50 |  |
| `chsup2` | code 5 | 4 |  |

## LigneCdeCli

(abr. `L2`, physique : `LigneCdeCli`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLigneCde` | entier auto (clé) | 4 | clé unique |
| `NumCommande` | code 5 | 4 | clé avec doublons |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `Code_barre` | texte | 25 | clé avec doublons |
| `LibProd` | texte | 40 | clé avec doublons |
| `Quantite` | code 7 | 8 | clé avec doublons |
| `PrixVente` | code 17 | 10 | clé avec doublons |
| `Remise` | code 7 | 8 | clé avec doublons |
| `TauxTVA` | code 7 | 8 | clé avec doublons |
| `Qte_Livree` | code 7 | 8 | clé avec doublons |
| `PrixTTC` | code 17 | 10 | clé avec doublons |

## LigneCdeFou

(abr. `L1`, physique : `LigneCdeFou`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLigneCde` | entier auto (clé) | 4 | clé unique |
| `NumCommande` | code 5 | 4 | clé avec doublons |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `LibProd` | texte | 40 | clé avec doublons |
| `Quantite` | code 7 | 8 | clé avec doublons |
| `Remise` | code 7 | 8 | clé avec doublons |
| `TauxTVA` | code 7 | 8 | clé avec doublons |
| `Qte_Livree` | code 7 | 8 | clé avec doublons |
| `prix_achat` | code 17 | 10 | clé avec doublons |
| `Soldee` | code 37 | 1 | clé avec doublons |
| `ref_fourn` | texte | 50 | clé avec doublons |

## LigneFac

(abr. `L3`, physique : `LigneFac`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLigneFac` | entier auto (clé) | 4 | clé unique |
| `NumFacture` | code 5 | 4 | clé avec doublons |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `Reference` | texte | 20 | clé avec doublons |
| `LibProd` | texte | 40 | clé avec doublons |
| `Quantite` | code 7 | 8 | clé avec doublons |
| `PrixVente` | code 17 | 10 | clé avec doublons |
| `Remise` | code 7 | 8 | clé avec doublons |
| `TauxTVA` | code 7 | 8 | clé avec doublons |
| `IDLigneCde` | code 5 | 4 | clé avec doublons |
| `ventillation` | code 25 | 8 | clé avec doublons |
| `NumCommande` | code 5 | 4 | clé avec doublons |
| `PrixTTC` | code 17 | 10 | clé avec doublons |

## LigneReg

(abr. `LG`, physique : `LigneReg`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDLigneReg` | entier auto (clé) | 4 | clé unique |
| `IDReglement` | code 5 | 4 | clé avec doublons |
| `Code_reglement` | code 5 | 4 | clé avec doublons |
| `Montant_reglement` | code 17 | 10 | clé avec doublons |

## ListeProduction

(physique : `ListeProduction`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDListeProduction` | entier auto (clé) | 4 | clé unique |
| `ID_ECRAN_PREPA` | texte | 50 | clé avec doublons |
| `DH_ARRIVEE` | code 34 | 8 | clé avec doublons |
| `no_table` | texte | 6 |  |
| `nb_couv` | code 3 | 2 |  |
| `vendeur` | code 25 | 8 | clé avec doublons |
| `DH_FIN` | code 34 | 8 |  |
| `Status` | code 36 | 1 | clé avec doublons |
| `NumProd` | code 27 | 8 | clé avec doublons |
| `Priorite` | code 36 | 1 | clé avec doublons |
| `PosX` | code 3 | 2 |  |
| `PosY` | code 3 | 2 |  |
| `Altitude` | code 9 | 2 |  |
| `Zoom` | code 36 | 1 |  |
| `Detail_Ticket` | code 29 | 8 |  |
| `Etat` | texte | 50 |  |
| `Divers1` | texte | 50 |  |
| `Divers2` | texte | 50 |  |
| `Numerique1` | code 5 | 4 |  |
| `Numerique2` | code 5 | 4 |  |

## MENU

(abr. `M1`, physique : `MENU`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `M1CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `CLE_MENU` | texte | 20 | clé unique |
| `SUPPL_MENU` | code 17 | 10 |  |
| `TYPE_MENU` | code 36 | 1 |  |
| `cleprod_menu` | code 5 | 4 | clé avec doublons |
| `MFONCT1` | code 5 | 4 |  |
| `MFONCT2` | texte | 10 |  |

## Messages

(abr. `M0`, physique : `messages`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `M0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `libelle` | texte | 35 | clé avec doublons |
| `msg_lib` | texte | 1000 |  |
| `TYPE_LIB` | code 36 | 1 |  |

## Mouvements

(abr. `MV`, physique : `Mouvements`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `MVTCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `LibProd` | texte | 40 | clé avec doublons |
| `date_mvt` | code 14 | 8 | clé avec doublons |
| `entree` | code 37 | 1 | clé avec doublons |
| `type_mvt` | code 36 | 1 | clé avec doublons |
| `Quantite` | code 7 | 8 | clé avec doublons |
| `montant` | code 17 | 10 | clé avec doublons |
| `ref_mvt` | texte | 40 | clé avec doublons |
| `Observations` | code 29 | 8 |  |

## Numeroz

(abr. `NU`, physique : `numeroz`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `NUCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `num_caisse` | code 36 | 1 | clé unique |
| `numero_ticket` | code 5 | 4 |  |

## Param

(abr. `PA`, physique : `param`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `cle` | texte | 25 | clé unique |
| `obs` | texte | 90 |  |
| `complement` | texte | 20 |  |
| `valeur` | texte | 35 |  |

## PesageTrace

(physique : `PesageTrace`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDPesageTrace` | entier auto (clé) | 4 | clé unique |
| `DATE_HEURE` | code 34 | 8 | clé avec doublons |
| `SENS` | texte | 1 | clé avec doublons |
| `TRAME` | texte | 100 |  |
| `PROTOCOLE` | texte | 15 |  |
| `PARAMS` | texte | 15 |  |
| `DIVERS1` | texte | 50 |  |
| `CAISSE` | code 5 | 4 |  |
| `VENDEUR` | code 5 | 4 |  |
| `VERSION` | texte | 30 |  |

## POINTVENTE

(abr. `PV`, physique : `POINTVENTE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDPOINTVENTE` | entier auto (clé) | 4 | clé unique |
| `NOM_PDV` | texte | 50 | clé avec doublons |
| `ACCES_PDV` | texte | 50 | clé avec doublons |
| `USER_PDV` | texte | 50 |  |
| `MDP_PDV` | texte | 50 |  |
| `DOSSIER_PDV` | texte | 50 | clé avec doublons |
| `NUM_TICKET` | code 5 | 4 | clé avec doublons |
| `SEL_TRANSFERT` | code 37 | 1 | clé avec doublons |

## PORTABLE

(abr. `PO`, physique : `PORTABLE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDPORTABLE` | entier auto (clé) | 4 | clé unique |
| `PO_NOSERIE` | texte | 50 | clé unique |
| `PO_NODEVICE` | code 5 | 4 | clé unique |
| `PO_NOM` | texte | 50 | clé avec doublons |
| `PO_TYPE` | code 5 | 4 | clé avec doublons |
| `PO_CHECKRES` | code 5 | 4 | clé avec doublons |
| `PO_CHECKLIST` | code 5 | 4 | clé avec doublons |
| `PO_IMPR1` | texte | 50 | clé avec doublons |
| `PO_IMPR2` | texte | 50 | clé avec doublons |
| `PO_VENTIL` | code 5 | 4 | clé avec doublons |
| `PO_VENDEUR` | code 5 | 4 | clé avec doublons |
| `PO_CLAVIER` | code 5 | 4 | clé avec doublons |
| `PO_CHOIXPROD` | code 5 | 4 | clé avec doublons |
| `PO_SALLE` | texte | 50 |  |
| `PO_NUM1` | code 5 | 4 | clé avec doublons |
| `PO_NUM2` | code 5 | 4 | clé avec doublons |
| `PO_TXT1` | texte | 50 |  |
| `PO_TXT2` | texte | 50 |  |

## Prenoms

(abr. `P0`, physique : `prenoms`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `P0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `prprenom` | texte | 30 | clé avec doublons |
| `prdatefete` | code 14 | 8 | clé avec doublons |

## PRODFOU

(abr. `PF`, physique : `PRODFOU`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDPRODFOU` | entier auto (clé) | 4 | clé unique |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `NumFournisseur` | code 5 | 4 | clé avec doublons |
| `ref_fourn` | texte | 50 | clé avec doublons |
| `prix_achat` | code 17 | 10 | clé avec doublons |
| `Code_barre` | texte | 25 | clé avec doublons |

## Produits

(abr. `PR`, physique : `produits`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `PRCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `nom_long` | texte | 40 | clé avec doublons |
| `nom_court` | texte | 25 | clé avec doublons |
| `prix_base` | code 17 | 10 |  |
| `code_tva` | texte | 1 | clé avec doublons |
| `TTC_HT` | code 36 | 1 |  |
| `code_compta` | texte | 20 | clé avec doublons |
| `dessin` | texte | 30 |  |
| `fond` | texte | 25 |  |
| `touche` | texte | 5 | clé avec doublons |
| `Code_barre` | texte | 25 | clé avec doublons |
| `FACLEUNIK` | code 5 | 4 | clé avec doublons |
| `imp_prod` | texte | 70 |  |
| `msg_prod` | texte | 30 |  |
| `code_fourn` | code 26 | 4 |  |
| `ref_fourn` | texte | 50 |  |
| `prix_achat` | code 17 | 10 |  |
| `condit` | code 26 | 4 |  |
| `Code_unigest` | texte | 2 | clé avec doublons |
| `Type_produit` | texte | 50 | clé avec doublons |
| `Prod_negatif` | code 37 | 1 |  |
| `pmp` | code 7 | 8 |  |
| `PRSUP1` | texte | 10 |  |
| `PRSUP2` | code 5 | 4 |  |

## PRODVAR

(physique : `PRODVAR`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDPRODVAR` | entier auto (clé) | 4 | clé unique |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `Code_barre` | texte | 25 | clé avec doublons |
| `CLE_VARI` | code 5 | 4 |  |
| `qt_stock` | code 7 | 8 |  |

## QTEPROD

(abr. `QT`, physique : `QTEPROD`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `QTCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `QTE_STOCK` | code 7 | 8 |  |
| `ALERTE` | code 5 | 4 |  |
| `DATE_INV` | code 14 | 8 |  |
| `QTE_INV` | code 7 | 8 |  |

## Reglem_j

(abr. `RE`, physique : `reglem_j`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `RECLEUNIK` | entier auto (clé) | 4 | clé unique |
| `num_histo` | code 5 | 4 | clé avec doublons |
| `vendeur` | code 5 | 4 | clé avec doublons |
| `cle_reglem` | code 36 | 1 | clé avec doublons |
| `montant` | code 17 | 10 |  |

## Reglement

(abr. `RG`, physique : `Reglement`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDReglement` | entier auto (clé) | 4 | clé unique |
| `montant` | code 17 | 10 | clé avec doublons |
| `date_reglement` | code 14 | 8 | clé avec doublons |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |

## ReleveNotesClient

(physique : `ReleveNotesClient`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDReleveNotesClient` | entier auto (clé) | 4 | clé unique |
| `NO_CLIENT` | code 5 | 4 | clé avec doublons |
| `DATE_deb` | code 14 | 8 | clé avec doublons |
| `DATE_fin` | code 14 | 8 | clé avec doublons |
| `No_releve` | code 5 | 4 |  |
| `Solde_memo` | code 17 | 10 |  |
| `chsup_1` | code 5 | 4 |  |
| `chsup_2` | code 5 | 4 |  |
| `CLe_comp` | texte | 50 | clé unique |

## RESA

(abr. `R0`, physique : `RESA`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `R0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `CLECOMP` | texte | 20 | clé avec doublons |
| `NBCOUV` | code 3 | 2 |  |
| `NOM` | texte | 30 | clé avec doublons |
| `TEL` | texte | 20 | clé avec doublons |
| `CLCLEUNIK` | code 5 | 4 | clé avec doublons |
| `obs` | texte | 90 |  |
| `V0CLEUNIK` | code 5 | 4 |  |

## SEB_ABO_ENT

(physique : `SEB_ABO_ENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_ABO` | code 24 | 8 | clé unique |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `SA_FORFAIT_SOLDE` | code 37 | 1 | clé avec doublons |
| `SA_DATE_CREATION` | code 14 | 8 |  |
| `SA_DATE_VALIDITE` | code 14 | 8 |  |
| `SA_PRIX_FORFAIT` | code 17 | 10 |  |
| `SA_PRIX_REEL` | code 17 | 10 |  |
| `SA_OBSERVATION` | code 29 | 8 |  |
| `SA_LIBRE_N1` | code 25 | 8 |  |
| `SA_LIBRE_N2` | code 25 | 8 |  |
| `SA_LIBRE_M1` | code 17 | 10 |  |
| `SA_LIBRE_M2` | code 17 | 10 |  |
| `SA_LIBRE_T1` | texte | 30 |  |
| `SA_LIBRE_T2` | texte | 30 |  |
| `SA_DETAIL_ABO` | code 29 | 8 |  |
| `SA_ABO_MAITRE` | code 27 | 8 | clé avec doublons |

## SEB_ABO_LIG

(physique : `SEB_ABO_LIG`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_ABO` | code 25 | 8 | clé avec doublons |
| `IDSEB_ABO_LIG` | code 24 | 8 | clé unique |
| `PR_CLEUNIK` | code 27 | 8 | clé avec doublons |
| `SAL_QTE` | code 5 | 4 |  |
| `SAL_QTE_USED` | code 5 | 4 |  |
| `SAL_SOLDE` | code 37 | 1 | clé avec doublons |
| `SAL_DETAIL_LIGNE` | code 29 | 8 |  |
| `SAL_TYPE_LIGNE` | texte | 1 | clé avec doublons |
| `SAL_CLE_PLAGE` | code 5 | 4 |  |
| `SAL_DATE_VALIDITE` | code 14 | 8 |  |
| `SAL_ETAT` | code 36 | 1 | clé avec doublons |

## SEB_AFFICHAGE

(physique : `SEB_AFFICHAGE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `AF_CLE` | entier auto (clé) | 4 | clé unique |
| `AF_LIBELLE` | texte | 40 |  |
| `AF_PS_CLE` | code 27 | 8 | clé avec doublons |
| `AF_POSITION` | code 36 | 1 |  |
| `AF_GA_CLE` | code 27 | 8 | clé avec doublons |

## SEB_ARTICLE

(abr. `S1`, physique : `SEB_ARTICLE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `ARCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `AR_CODE` | texte | 5 | clé avec doublons |
| `AR_LIBELLE` | texte | 40 |  |
| `AR_PRIX_TTC` | code 17 | 10 |  |
| `AR_QTE` | code 6 | 4 |  |
| `AR_REMISE` | code 17 | 10 |  |
| `AR_TVA` | code 6 | 4 |  |
| `AR_FAMILLE` | code 9 | 2 |  |
| `AR_DATE` | code 14 | 8 |  |
| `AR_HEURE` | code 11 | 4 |  |
| `AR_PAYE` | code 37 | 1 | clé avec doublons |
| `AR_USER_1` | texte | 20 |  |
| `AR_USER_2` | code 17 | 10 |  |
| `AR_USER_3` | code 17 | 10 |  |
| `AR_LIB_TARIF` | texte | 20 |  |
| `AR_COD_CPT` | texte | 12 |  |
| `AR_COD_VEND` | code 5 | 4 |  |
| `AR_TRAITE` | code 5 | 4 |  |
| `AR_COD_TARIF` | code 5 | 4 |  |
| `AR_COD_MENU` | texte | 20 |  |
| `AR_TRIFAM` | code 5 | 4 |  |
| `AR_REMISE_GES` | code 5 | 4 |  |
| `AR_NUM_TABLE_LEO` | texte | 30 | clé avec doublons |
| `IDSEB_PARTICIPANT` | code 5 | 4 | clé avec doublons |
| `AR_NUM_FORFAIT` | code 25 | 8 | clé avec doublons |
| `AR_NUM_FORFAIT_LIG` | code 26 | 4 | clé avec doublons |

## SEB_ARTICLE_RDV

(abr. `S2`, physique : `SEB_ARTICLE_RDV`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `ARCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `AR_CODE` | texte | 5 | clé avec doublons |
| `AR_LIBELLE` | texte | 40 |  |
| `AR_PRIX_TTC` | code 17 | 10 |  |
| `AR_QTE` | code 6 | 4 |  |
| `AR_REMISE` | code 17 | 10 |  |
| `AR_TVA` | code 6 | 4 |  |
| `AR_FAMILLE` | code 9 | 2 |  |
| `AR_DATE` | code 14 | 8 |  |
| `AR_HEURE` | code 11 | 4 |  |
| `AR_PAYE` | code 37 | 1 | clé avec doublons |
| `AR_USER_1` | texte | 20 |  |
| `AR_USER_2` | code 17 | 10 |  |
| `AR_USER_3` | code 17 | 10 |  |
| `AR_LIB_TARIF` | texte | 20 |  |
| `AR_COD_CPT` | texte | 12 |  |
| `AR_COD_VEND` | code 5 | 4 |  |
| `AR_TRAITE` | code 5 | 4 |  |
| `AR_COD_TARIF` | code 5 | 4 |  |
| `AR_COD_MENU` | texte | 20 |  |
| `AR_TRIFAM` | code 5 | 4 |  |
| `AR_REMISE_GES` | code 5 | 4 |  |
| `IDSEB_RDV` | code 27 | 8 | clé avec doublons |
| `AR_NUM_FORFAIT` | code 25 | 8 | clé avec doublons |
| `AR_NUM_PLAGE` | code 26 | 4 | clé avec doublons |

## SEB_COMMENTAIRE_POSTE

(physique : `SEB_COMMENTAIRE_POSTE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_COMMENTAIRE_POSTE` | entier auto (clé) | 4 | clé unique |
| `IDPoste` | code 26 | 4 | clé avec doublons |
| `DateJour` | code 14 | 8 | clé avec doublons |
| `commentaire` | texte | 50 |  |
| `Divers1` | texte | 50 |  |
| `DateModif` | code 34 | 8 |  |
| `ModifiePar` | texte | 30 |  |

## SEB_COMPETENCE

(physique : `SEB_COMPETENCE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `CM_PS_CLE` | code 27 | 8 | clé avec doublons |
| `CM_PRCLEUNIK` | code 27 | 8 | clé avec doublons |
| `CM_NIVEAU` | code 36 | 1 |  |
| `CM_VALIDITE` | code 14 | 8 |  |
| `CM_FORMATION` | code 14 | 8 |  |

## SEB_CT_ENT

(physique : `SEB_CT_ENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_CT_ENT` | code 24 | 8 | clé unique |
| `CTE_NUM_PROD` | code 27 | 8 | clé avec doublons |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `CTE_LIB_CLIENT` | texte | 50 |  |
| `CTE_CREDIT_TEMPS` | code 35 | 8 |  |
| `CTE_CREDIT_UTILISE` | code 35 | 8 |  |
| `CTE_DATE_VALIDITE` | code 14 | 8 | clé avec doublons |
| `CTE_ETAT` | code 36 | 1 | clé avec doublons |
| `CTE_TXT_1` | texte | 25 |  |
| `CTE_NUM_CARTE_EXT` | texte | 50 | clé avec doublons |
| `CTE_DATE_CREATION` | code 34 | 8 |  |
| `CTE_SOLDE` | code 37 | 1 | clé avec doublons |
| `CTE_DETAIL` | code 29 | 8 |  |

## SEB_CT_LIG

(physique : `SEB_CT_LIG`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_CT_LIG` | code 24 | 8 | clé unique |
| `IDSEB_CT_ENT` | code 25 | 8 | clé avec doublons |
| `CTL_DATEHEURE` | code 34 | 8 |  |
| `CTL_DUREE` | code 35 | 8 |  |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `CTL_DIVERS_TXT_1` | texte | 25 |  |
| `CTL_DIVERS_TXT_2` | texte | 25 |  |
| `CTL_NUM_1` | code 7 | 8 |  |
| `CTL_MON_1` | code 17 | 10 |  |
| `CTL_DATE_1` | code 34 | 8 |  |
| `CTL_NUM_PLAGE` | code 27 | 8 |  |

## SEB_ECHEANCIER

(physique : `SEB_ECHEANCIER`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_PAIEMENT` | entier auto (clé) | 4 | clé unique |
| `PA_C0CLEUNIK` | code 27 | 8 | clé avec doublons |
| `PA_TYPEREG` | code 9 | 2 |  |
| `PA_MONTANT` | code 17 | 10 |  |
| `PA_DATE_RECEPTION` | code 14 | 8 |  |
| `PA_MONTANT_UTILISE` | code 17 | 10 |  |
| `PA_ETAT` | code 36 | 1 | clé avec doublons |
| `PA_DATE_ENCAISSEMENT` | code 14 | 8 | clé avec doublons |
| `PA_POUR_QUOI` | texte | 1 |  |
| `PA_CLE_LIE` | code 27 | 8 | clé avec doublons |

## SEB_FORFAIT_ENT

(physique : `SEB_FORFAIT_ENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_FORFAIT` | code 25 | 8 | clé unique |
| `C0CLEUNIK` | code 5 | 4 | clé avec doublons |
| `SF_FORFAIT_SOLDE` | code 37 | 1 | clé avec doublons |
| `SF_DATE_CREATION` | code 14 | 8 |  |
| `SF_DATE_VALIDITE` | code 14 | 8 |  |
| `SF_PRIX_FORFAIT` | code 17 | 10 |  |
| `SF_PRIX_REEL` | code 17 | 10 |  |
| `SF_OBSERVATION` | code 29 | 8 |  |
| `SF_LIBRE_N1` | code 25 | 8 |  |
| `SF_LIBRE_N2` | code 25 | 8 |  |
| `SF_LIBRE_M1` | code 17 | 10 |  |
| `SF_LIBRE_M2` | code 17 | 10 |  |
| `SF_LIBRE_T1` | texte | 30 |  |
| `SF_LIBRE_T2` | texte | 30 |  |
| `SF_DETAIL_FORFAIT` | code 29 | 8 |  |

## SEB_FORFAIT_LIG

(physique : `SEB_FORFAIT_LIG`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_FORFAIT_LIG` | code 24 | 8 | clé unique |
| `IDSEB_FORFAIT` | code 25 | 8 | clé avec doublons |
| `PR_CLEUNIK` | code 27 | 8 | clé avec doublons |
| `SFL_QTE` | code 5 | 4 |  |
| `SFL_QTE_USED` | code 5 | 4 |  |
| `SFL_SOLDE` | code 37 | 1 | clé avec doublons |
| `SFL_DETAIL_LIGNE` | code 29 | 8 |  |
| `SFL_TYPE_LIGNE` | texte | 1 | clé avec doublons |
| `SFL_CLE_PLAGE` | code 26 | 4 |  |

## SEB_GRP_AFFICHAGE

(physique : `SEB_GRP_AFFICHAGE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `GA_CLE` | entier auto (clé) | 4 | clé unique |
| `GA_LIBELLE` | texte | 30 |  |
| `GA_VENTIL` | code 27 | 8 |  |
| `GA_FAMILLE` | code 27 | 8 |  |
| `GA_NB_COL` | code 36 | 1 |  |
| `GA_TYPE` | texte | 1 |  |

## SEB_HST_ENT

(physique : `SEB_HST_ENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_HST_ENT` | entier auto (clé) | 4 | clé unique |
| `HST_VERSION` | texte | 50 |  |
| `HST_DATEHEURE` | code 34 | 8 | clé avec doublons |
| `HST_TYPE` | texte | 8 |  |
| `HST_TEXTE_COURT` | texte | 150 |  |
| `HST_TEXTE_LONG` | code 29 | 8 |  |
| `HST_MODULE` | texte | 10 |  |

## SEB_PAIEMENT

(physique : `SEB_PAIEMENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_PAIEMENT` | entier auto (clé) | 4 | clé unique |
| `PA_CLERDV` | code 26 | 4 | clé avec doublons |
| `PA_TYPEREG` | code 9 | 2 |  |
| `PA_MONTANT` | code 17 | 10 |  |
| `PA_DATE_RECEPTION` | code 14 | 8 |  |
| `PA_MONTANT_UTILISE` | code 17 | 10 |  |
| `PA_ETAT` | code 36 | 1 | clé avec doublons |

## SEB_PAIEMENT_GRP

(physique : `SEB_PAIEMENT_GRP`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_PAIEMENT` | entier auto (clé) | 4 | clé unique |
| `PA_CLE_PARTICIPANT` | code 27 | 8 | clé avec doublons |
| `PA_TYPEREG` | code 9 | 2 |  |
| `PA_MONTANT` | code 17 | 10 |  |
| `PA_DATE_RECEPTION` | code 14 | 8 |  |
| `PA_MONTANT_UTILISE` | code 17 | 10 |  |
| `PA_ETAT` | code 36 | 1 | clé avec doublons |
| `PA_DATE_ENCAISSEMENT` | code 14 | 8 | clé avec doublons |

## SEB_PARTICIPANT

(physique : `SEB_PARTICIPANT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_PARTICIPANT` | entier auto (clé) | 4 | clé unique |
| `SP_DATE_RESA` | code 14 | 8 |  |
| `SP_HEURE_RESA` | code 11 | 4 |  |
| `SP_LIB_PARTICIPANT` | texte | 60 | clé avec doublons |
| `SP_NUM_CLIENT_LEO` | code 27 | 8 | clé avec doublons |
| `SP_NOMBRE` | code 5 | 4 |  |
| `SP_DATE_OPTION` | code 14 | 8 |  |
| `SP_NUM_TABLE_LEO` | code 25 | 8 | clé avec doublons |
| `SP_TOTAL_CONSO` | code 17 | 10 |  |
| `SP_ACCOMPTE` | code 17 | 10 |  |
| `SP_DATE_ACOMPTE` | code 14 | 8 |  |
| `SP_COMMENTAIRE` | code 29 | 8 |  |
| `SP_NUM_BILLET` | texte | 20 | clé avec doublons |
| `SP_REG_BILLET` | texte | 50 |  |
| `SP_LIBRE_T1` | texte | 30 |  |
| `SP_LIBRE_T2` | texte | 30 |  |
| `SP_LIBRE_N1` | code 7 | 8 |  |
| `SP_LIBRE_N2` | code 7 | 8 |  |
| `SP_LIBRE_D1` | code 14 | 8 |  |
| `SP_LIBRE_D2` | code 14 | 8 |  |
| `SP_DATE_CONTRAT` | code 14 | 8 |  |
| `SP_DATE_BILLET` | code 14 | 8 |  |
| `SP_DATE_MODIF` | code 14 | 8 |  |
| `PL_CLE` | code 25 | 8 | clé avec doublons |

## SEB_PLAGE

(abr. `S0`, physique : `SEB_PLAGE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `PL_CLE` | code 24 | 8 | clé unique |
| `PL_POSTE` | code 27 | 8 | clé avec doublons |
| `PL_JOUR` | code 14 | 8 | clé avec doublons |
| `PL_Libelle` | texte | 40 |  |
| `PL_HEUREDEB` | code 34 | 8 | clé avec doublons |
| `PL_HEUREFIN` | code 34 | 8 | clé avec doublons |
| `PL_DUREE` | code 35 | 8 | clé avec doublons |
| `PL_COULEUR_C` | code 25 | 8 |  |
| `PL_COULEUR_F` | code 25 | 8 |  |
| `PL_COULEUR_T` | code 25 | 8 |  |
| `PL_FOND` | texte | 50 |  |
| `PL_ICONE` | texte | 50 |  |
| `PL_COMMENTAIRE` | code 29 | 8 |  |
| `PL_CREATION` | texte | 12 |  |
| `PL_MODIF` | texte | 12 |  |
| `PL_CREATEUR` | code 25 | 8 |  |
| `PL_CLIENT` | code 25 | 8 | clé avec doublons |
| `PL_ALARME` | code 36 | 1 | clé avec doublons |
| `PL_ALARME_TIME` | code 11 | 4 |  |
| `PL_CATEGORIE` | code 26 | 4 |  |
| `PL_NB_PARTICIPANT` | code 5 | 4 |  |
| `PL_NB_PARTICIPANT_MAX` | code 5 | 4 |  |
| `PL_DIV_C1` | texte | 40 |  |
| `PL_DIV_C2` | texte | 40 |  |
| `PL_DIV_N1` | code 25 | 8 |  |
| `PL_DIV_N2` | code 27 | 8 |  |
| `PL_A_CONFIRMER` | code 37 | 1 |  |
| `PL_CONFIRMEE` | code 37 | 1 |  |
| `PL_POSTE_JOUR` | code None |  | clé avec doublons |
| `PL_RDV_CLE` | code 27 | 8 | clé avec doublons |
| `PL_CLE_PRODUIT` | code 27 | 8 | clé avec doublons |
| `PL_POSTE_BIS` | code 27 | 8 | clé avec doublons |
| `PL_POSTE_TER` | code 27 | 8 | clé avec doublons |
| `PL_ETAT` | code 36 | 1 | clé avec doublons |
| `PL_TEXTE_ALERTE` | code 29 | 8 |  |
| `PL_PRIX` | code 17 | 10 |  |
| `PL_NUM_LIGNE_FORFAIT` | code 27 | 8 |  |
| `PL_NUM_ABONNEMENT` | code 27 | 8 |  |

## SEB_POSTE

(physique : `SEB_POSTE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `PS_CLE` | code 24 | 8 | clé unique |
| `PS_NOM` | texte | 40 |  |
| `PS_COMMENTAIRE` | code 29 | 8 |  |
| `PS_TYPE` | texte | 1 | clé avec doublons |
| `PS_VDR_LEO` | code 27 | 8 | clé avec doublons |
| `PS_ETAT` | code 36 | 1 | clé avec doublons |
| `PS_TEINTE` | code 26 | 4 |  |
| `PS_LOGIN` | texte | 50 |  |
| `PS_MDP` | texte | 25 |  |

## SEB_POSTE_COMBINE

(physique : `SEB_POSTE_COMBINE`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSEB_POSTE_COMBINE` | entier auto (clé) | 4 | clé unique |
| `PC_CLE_MAITRE` | code 27 | 8 | clé avec doublons |
| `PC_CLE_COMBINE` | code 27 | 8 | clé avec doublons |
| `PC_DATE_1` | code 14 | 8 |  |
| `PC_TXT_1` | texte | 50 |  |
| `PC_NUM_1` | code 27 | 8 |  |
| `PC_MON_1` | code 17 | 10 |  |

## SEB_PRODUIT

(physique : `SEB_PRODUIT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `PR_CLEUNIK` | code 27 | 8 | clé unique |
| `PR_DUREE` | code 35 | 8 |  |
| `PR_PAUSE` | code 35 | 8 |  |
| `PR_TYPE` | texte | 1 | clé avec doublons |
| `PR_USAGE` | texte | 1 | clé avec doublons |
| `PR_QTE_MAX` | code 5 | 4 |  |
| `PR_COUT` | code 17 | 10 |  |
| `PR_LIBRE_N1` | code 5 | 4 |  |
| `PR_LIBRE_N2` | code 5 | 4 |  |
| `PR_LIBRE_M1` | code 17 | 10 |  |
| `PR_LIBRE_M2` | code 17 | 10 |  |
| `PR_LIBRE_T1` | texte | 30 |  |
| `PR_LIBRE_T2` | texte | 30 |  |
| `PR_LIBRE_D1` | code 14 | 8 |  |
| `PR_LIBRE_D2` | code 14 | 8 |  |
| `PR_ETAT` | code 36 | 1 | clé avec doublons |
| `PR_CONSOMME` | code 29 | 8 |  |
| `PR_DATE_DEB` | code 14 | 8 |  |
| `PR_DATE_FIN` | code 14 | 8 |  |
| `PR_FORFAIT` | code 29 | 8 |  |

## SEB_RDV

(physique : `SEB_RDV`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `GR_CLE` | code 24 | 8 | clé unique |
| `GR_LIB_1` | texte | 50 |  |
| `GR_LIB_2` | texte | 50 |  |
| `GR_TOTAL` | code 17 | 10 |  |
| `GR_SOLDE` | code 37 | 1 |  |
| `GR_ARCHIVE` | code 37 | 1 |  |
| `GR_TABLE_LEO` | code 27 | 8 | clé avec doublons |
| `GR_CLIENT_LEO` | code 27 | 8 | clé avec doublons |
| `GR_DIV_C1` | texte | 50 |  |
| `GR_DIV_C2` | texte | 50 |  |
| `GR_DIV_N1` | code 25 | 8 |  |
| `GR_DIV_N2` | code 25 | 8 |  |
| `GR_DIV_R1` | code 7 | 8 |  |
| `GR_DIV_R2` | code 7 | 8 |  |
| `GR_DIV_CLE` | texte | 20 | clé avec doublons |
| `GR_DATE` | code 14 | 8 | clé avec doublons |
| `GR_TYPE` | texte | 1 | clé avec doublons |
| `GR_TEXTE_ALERTE` | code 29 | 8 |  |
| `GR_ETAT` | code 36 | 1 | clé avec doublons |
| `GR_NUM_FORFAIT` | code 25 | 8 |  |

## Secu_mdp

(abr. `SE`, physique : `secu_mdp`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `SECLEUNIK` | entier auto (clé) | 4 | clé unique |
| `niveau_mdp` | code 36 | 1 | clé unique |
| `mot_passe` | code 5 | 4 |  |

## Sel_Règlement

(abr. `SR`, physique : `Sel_Règlement`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSel_Règlement` | entier auto (clé) | 4 | clé unique |
| `date_reglement` | code 14 | 8 | clé avec doublons |
| `heure` | texte | 6 | clé avec doublons |
| `NO_Regl` | code 5 | 4 | clé avec doublons |
| `Lib_Regl` | texte | 20 |  |
| `Montant_Regl` | code 17 | 10 |  |
| `NUM_TICKET` | code 5 | 4 | clé avec doublons |
| `no_vendeur` | code 5 | 4 | clé avec doublons |
| `service` | code 5 | 4 | clé avec doublons |
| `NO_CLIENT` | code 5 | 4 | clé avec doublons |
| `nom_client` | texte | 50 |  |
| `No_Caisse` | code 5 | 4 | clé avec doublons |

## Sel_Tickets

(abr. `ST`, physique : `Sel_Tickets`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSel_Tickets` | entier auto (clé) | 4 | clé unique |
| `DATE` | code 14 | 8 | clé avec doublons |
| `heure` | texte | 6 | clé avec doublons |
| `No_Caisse` | code 5 | 4 | clé avec doublons |
| `no_vendeur` | code 5 | 4 | clé avec doublons |
| `nom_vendeur` | texte | 50 |  |
| `no_table` | texte | 6 | clé avec doublons |
| `NUM_TICKET` | code 5 | 4 | clé avec doublons |
| `nb_couv` | code 3 | 2 |  |
| `cleprod` | code 25 | 8 | clé avec doublons |
| `clefam` | code 5 | 4 | clé avec doublons |
| `nom_prod` | texte | 50 |  |
| `qte` | code 7 | 8 |  |
| `prixu_ttc` | code 17 | 10 |  |
| `montant` | code 17 | 10 |  |
| `nom_fam` | texte | 50 |  |
| `no_ventil` | code 5 | 4 | clé avec doublons |
| `no_tva` | code 5 | 4 | clé avec doublons |
| `tx_tva` | code 17 | 10 |  |
| `code_compta` | texte | 20 | clé avec doublons |
| `remise` | code 17 | 10 |  |
| `duree` | code 11 | 4 |  |
| `offert` | code 9 | 2 | clé avec doublons |
| `offert_perso` | code 9 | 2 | clé avec doublons |
| `service` | code 5 | 4 | clé avec doublons |
| `NO_CLIENT` | code 5 | 4 | clé avec doublons |
| `nom_client` | texte | 50 |  |
| `no_groupe` | code 5 | 4 | clé avec doublons |

## SousGroupe

(physique : `SousGroupe`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDSousGroupe` | entier auto (clé) | 4 | clé unique |
| `no_sous_groupe` | code 5 | 4 | clé avec doublons |
| `cleprod` | code 5 | 4 |  |
| `NOM` | texte | 50 |  |
| `chsup_1` | code 5 | 4 |  |
| `chsup_2` | texte | 20 |  |

## Tables_Valides

(physique : `Tables_Valides`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTables_Valides` | entier auto (clé) | 4 | clé unique |
| `notable` | texte | 50 | clé unique |
| `INFO_LIB` | texte | 50 |  |
| `TLibre1` | code 5 | 4 |  |
| `TLibre2` | texte | 50 |  |
| `TheureDep` | code 11 | 4 |  |
| `TheureDern` | code 11 | 4 |  |
| `Tstatus` | code 5 | 4 |  |
| `Trésea` | texte | 50 |  |

## TARIF

(abr. `TA`, physique : `TARIF`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `TACLEUNIK` | entier auto (clé) | 4 | clé unique |
| `PRIXU` | code 17 | 10 |  |
| `remise` | code 17 | 10 |  |
| `cleprod_fam` | texte | 20 | clé unique |

## TarifPromo_ent

(physique : `TarifPromo_ent`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTarifPromo_ent` | entier auto (clé) | 4 | clé unique |
| `Nom` | texte | 50 | clé avec doublons |
| `TPE_DATE_DEB` | code 14 | 8 | clé avec doublons |
| `TPE_DATE_FIN` | code 14 | 8 | clé avec doublons |
| `TPE_OBSERVATION` | code 29 | 8 |  |

## TarifPromo_Lig

(physique : `TarifPromo_Lig`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTarifPromo_Lig` | code 24 | 8 | clé unique |
| `IDTarifPromo_ent` | code 5 | 4 | clé avec doublons |
| `PRCLEUNIK` | code 5 | 4 | clé avec doublons |
| `TP_DATE_DEB` | code 14 | 8 | clé avec doublons |
| `TP_DATE_FIN` | code 14 | 8 | clé avec doublons |
| `TP_HEURE_DEB` | code 11 | 4 |  |
| `TP_HEURE_FIN` | code 11 | 4 |  |
| `TP_LUNDI` | code 37 | 1 |  |
| `TP_MARDI` | code 37 | 1 |  |
| `TP_MERCREDI` | code 37 | 1 |  |
| `TP_JEUDI` | code 37 | 1 |  |
| `TP_VENDREDI` | code 37 | 1 |  |
| `TP_SAMEDI` | code 37 | 1 |  |
| `TP_DIMANCHE` | code 37 | 1 |  |
| `TP_PRIX` | code 17 | 10 |  |
| `TP_PRIORITE` | code 36 | 1 | clé avec doublons |
| `TP_COULEUR` | code 27 | 8 |  |
| `TP_REGLE` | texte | 50 |  |
| `TP_QTE_MIN` | code 7 | 8 |  |
| `TP_QTE_MAX` | code 7 | 8 |  |
| `TP_DATE_CREATION` | code 34 | 8 |  |
| `TP_DATE_MODIF` | code 34 | 8 |  |
| `TP_VERSION` | texte | 10 |  |
| `TP_TYPE` | texte | 1 |  |
| `TP_DIVERS_1` | texte | 25 |  |
| `TP_DIVERS_2` | texte | 25 |  |

## TBASES

(abr. `TB`, physique : `TABLES BASES`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `TBCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `type_base` | texte | 20 | clé avec doublons |
| `cle_base` | code 5 | 4 | clé avec doublons |
| `libelle` | texte | 35 |  |
| `ba_valeur` | code 17 | 10 |  |

## TCData

(physique : `TCData`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTCData` | entier auto (clé) | 4 | clé unique |
| `NumProdPrincipal` | code 26 | 4 | clé avec doublons |
| `NumProdAffilie` | code 26 | 4 | clé avec doublons |
| `CodeX` | code 9 | 2 |  |
| `CodeY` | code 9 | 2 |  |
| `CodeZ` | code 9 | 2 |  |
| `DiversT1` | texte | 50 |  |
| `DiversT2` | texte | 50 |  |
| `DiversN1` | code 5 | 4 |  |
| `DiversN2` | code 5 | 4 |  |
| `DiversM1` | code 17 | 10 |  |

## TCVariante_Ent

(physique : `TCVariante_Ent`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTCVariante` | entier auto (clé) | 4 | clé unique |
| `Nom` | texte | 30 |  |
| `Axe` | texte | 1 |  |
| `DiversT1` | texte | 50 |  |
| `DiversN1` | texte | 50 |  |
| `DiversM1` | code 17 | 10 |  |
| `Nombre` | code 9 | 2 |  |

## TCVariante_Lig

(physique : `TCVariante_Lig`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDTCVariante_Lig` | entier auto (clé) | 4 | clé unique |
| `IDTCVariante` | code 5 | 4 | clé avec doublons |
| `Libelle` | texte | 35 |  |
| `CouleurFond` | code 26 | 4 |  |
| `DiversT1` | texte | 50 |  |
| `DiversN1` | texte | 50 |  |
| `DiversM1` | code 17 | 10 |  |
| `Ordre` | code 9 | 2 |  |

## THISTO

(abr. `TH`, physique : `TABLES HISTO`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `THCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `heure` | texte | 6 | clé avec doublons |
| `duree` | code 11 | 4 |  |
| `NUMERO` | code 5 | 4 | clé avec doublons |
| `cleprod` | code 5 | 4 | clé avec doublons |
| `prixu_ttc` | code 17 | 10 |  |
| `qte` | code 7 | 8 |  |
| `tva` | code 5 | 4 |  |
| `vendeur` | code 5 | 4 | clé avec doublons |
| `ventillation` | code 5 | 4 | clé avec doublons |
| `offert` | code 36 | 1 | clé avec doublons |
| `offert_perso` | code 36 | 1 | clé avec doublons |
| `num_caisse` | code 36 | 1 | clé avec doublons |
| `code_cpt` | texte | 10 | clé avec doublons |
| `no_table` | texte | 6 |  |
| `nb_couv` | code 3 | 2 |  |
| `remise` | code 17 | 10 |  |

## Touches

(abr. `T0`, physique : `touches`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `T0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `intitule` | texte | 20 | clé avec doublons |
| `infos` | texte | 80 |  |
| `Niveau_acces` | code 36 | 1 |  |
| `image_tch` | texte | 22 |  |
| `touche_clavier` | texte | 20 | clé avec doublons |
| `NO_ORDRE` | code 5 | 4 |  |

## TREGLEM

(abr. `TR`, physique : `TABLE REGLEMENT`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `TRCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `num_histo` | code 5 | 4 | clé avec doublons |
| `vendeur` | code 5 | 4 | clé avec doublons |
| `cle_reglem` | code 36 | 1 | clé avec doublons |
| `montant` | code 17 | 10 |  |

## TVA

(abr. `TV`, physique : `TVA`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `TVCLEUNIK` | entier auto (clé) | 4 | clé unique |
| `NOM_TVA` | texte | 20 |  |
| `TAUX_TVA` | texte | 20 |  |

## TVERROUS

(abr. `V1`, physique : `VERROUS`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `cle` | texte | 25 | clé unique |
| `valeur` | texte | 35 |  |

## Unigest

(physique : `Unigest`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `code_unigest` | texte | 2 | clé unique |
| `lib_unigest` | texte | 50 |  |

## VARIANTES

(physique : `VARIANTES`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDVARIANTES` | entier auto (clé) | 4 | clé unique |
| `LIB_VARI` | texte | 30 |  |

## VENDEUR

(abr. `V0`, physique : `VENDEUR`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `V0CLEUNIK` | entier auto (clé) | 4 | clé unique |
| `V0NOM` | texte | 20 | clé avec doublons |
| `V0PRENOM` | texte | 20 |  |
| `V0BADGE` | texte | 30 | clé avec doublons |
| `V0TOUCHE` | code 5 | 4 | clé avec doublons |
| `V0COM` | code 17 | 10 |  |
| `V0MANAGER` | code 36 | 1 |  |
| `V0TIROIR` | texte | 20 |  |
| `V0TARIF` | code 5 | 4 |  |
| `V0CLAVIER` | texte | 30 |  |
| `V0PROD` | code 5 | 4 |  |
| `MDP_POCKET` | texte | 8 |  |
| `V0LIBRE1` | texte | 10 |  |

## Ventil

(abr. `VE`, physique : `ventilation`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `VECLEUNIK` | entier auto (clé) | 4 | clé unique |
| `VENOM` | texte | 20 |  |
| `CLEVEND` | code 5 | 4 | clé avec doublons |
| `CLETVA` | code 5 | 4 | clé avec doublons |
| `CLECPT` | code 5 | 4 | clé avec doublons |
| `CLETARIF` | code 5 | 4 | clé avec doublons |
| `COULEUR` | code 5 | 4 |  |
| `touche` | texte | 5 | clé avec doublons |
| `HDEB` | code 11 | 4 |  |
| `HFIN` | code 11 | 4 |  |
| `vVELIBRE1` | texte | 10 |  |
| `VELIBRE2` | code 5 | 4 |  |

## Winid_Trans

(physique : `Winid_Trans`)

| Rubrique | Type | Taille | Clé |
|---|---|---|---|
| `IDWinid_Trans` | entier auto (clé) | 4 | clé unique |
| `WT_Id` | code 27 | 8 | clé unique |
| `WT_Date` | code 34 | 8 | clé avec doublons |
| `WT_Montant` | code 17 | 10 |  |
| `WT_NumTicket` | code 25 | 8 |  |
| `WT_Annulee` | code 37 | 1 |  |
| `WT_NoCaisse` | code 5 | 4 | clé avec doublons |
| `WT_Operateur` | texte | 50 |  |
