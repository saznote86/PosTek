# SYSTÈME DE LICENCE POSTEK — Spécification
Version 1.0 — 17/09/2026

---

## 1. Rupture avec l'existant

| Atoo Leo2 (existant) | POSTEK (nouveau) |
|---|---|
| Numéro de série HDD (`hdd.ini → HDD=1028`) | Empreinte CPU + carte mère (WMI) |
| `licence.FIC` / `licence.NDX` chiffrés | Jeton JSON **signé Ed25519** |
| `info_lic_leo2` (3 nombres en clair) | Fichier `*.posteklic` vérifiable sans secret |
| Vérification fermée dans le binaire | Vérification par **clé publique embarquée** |
| Mono-poste rigide | **Multi-postes** (`postes: N`) + révocation possible |

> La clé privée Ed25519 ne quitte jamais l'éditeur. L'application embarque
> `public_key.pub` uniquement : elle peut vérifier des milliers de licences
> sans détenir aucun secret (modèle standard des distributions signées).

## 2. Jeton de licence

```json
{
  "payload": {
    "format": "POSTEK-LIC-1",
    "societe": "Boulangerie El Manar",
    "postes": 3,
    "empreinte": "K7Q29MTX3ABPCDEF0123",
    "emis_le": "2026-09-01",
    "expiration": "2027-09-01",
    "options": {"hotel": true, "pesage": false}
  },
  "signature_b64": "..."
}
```

- Signature **Ed25519** sur le payload JSON canonique (clés triées, séparateurs compacts).
- Empreinte = SHA-256(CPU `ProcessorId` + carte mère `SerialNumber|Manufacturer`),
  formatée `POSTEK1-XXXX-XXXX-XXXX-XXXX` (Base32 sans caractères ambigus).
- Champs : `societe`, `postes` (nombre de postes simultanés licenciés),
  `empreinte` (poste lié — ou empreinte du serveur en mode multi-postes),
  `expiration` (optionnelle), `options` (modules activés).

## 3. Canaux d'activation

### 3.1 En ligne (défaut)
1. POSTEK envoie à l'API éditeur : `empreinte + code_installation`.
2. L'API répond avec le jeton signé correspondant à l'abonnement du client.
3. POSTEK vérifie la signature (clé publique locale), enregistre le jeton et
   la date de dernier contact (`dernier_contact_ok`).
4. Revalidation périodique silencieuse (cible : 24 h, tentative transparente).

### 3.2 Hors ligne (clé USB / e-mail)
1. Le client transmet son empreinte (affichée à l'écran d'activation).
2. L'éditeur fabrique `*.posteklic` avec `keygen.py offline ...`.
3. Le client importe le fichier : vérification locale complète, aucune
   connexion requise. Fenêtre de grâce de 7 jours avant revalidation.
4. Une revalidation en ligne ultérieure (même différée) prolonge la fenêtre.

## 4. Règles de décision (implémentées dans `token.etat_licence`)
1. Signature Ed25519 invalide → refus.
2. Champ obligatoire manquant (`societe`, `postes`, `empreinte`) → refus.
3. Empreinte du poste ≠ empreinte du jeton → refus (« autre poste »).
4. Date du jour > `expiration` → refus (« licence expirée »).
5. Absence de validation réussie depuis > 7 jours (alors qu'une validation
   a déjà eu lieu) → refus temporaire, réessai réseau possible.
6. Sinon → **licence valide**.

Les refus temporaires (5) n'effacent jamais les données : POSTEK démarre en
mode lecture seule tant que la licence n'est pas rétablie — une caisse ne
bloque jamais une journée de ventes sur un problème réseau.

## 5. Multi-postes
- Chaque poste possède sa propre empreinte et son propre jeton (n° de série
  de licence distincts, même `societe`).
- Le serveur de licences comptabilise les activations actives par client et
  refuse au-delà de `postes`. Réinstallation = désactivation préalable
  (appel API) ou validation manuelle éditeur.
- Extension future : jeton « flottant » pour HFSQL Client/Serveur.

## 6. Livrables code (`postek/licence/`)

| Fichier | Rôle |
|---|---|
| `ed25519.py` | Signature Ed25519 pure Python (RFC 8032, vecteurs officiels en test) |
| `fingerprint.py` | Empreinte matérielle CPU + carte mère (WMI + repli) |
| `token.py` | Création/vérification jetons, `etat_licence`, grâce offline |
| `keygen.py` | CLI éditeur : `generer` (paire de clés), `offline` (licence), `empreinte` |

## 7. Tests (`postek/tests/test_licence.py`)
- 4 vecteurs officiels RFC 8032 (signature publique de référence).
- Signatures falsifiées (payload altéré, autre clé, champ manquant).
- Règles d'état : expiration, autre poste, grâce 7 jours.
- Keygen bout en bout dans un dossier temporaire.
