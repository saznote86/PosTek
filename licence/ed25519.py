#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ed25519.py — Signature Ed25519 pure Python, conforme RFC 8032.

Choix technique : la bibliothèque `cryptography` n'est pas installée dans
l'environnement cible des caisses (postes Windows offline, installation
sans privilèges). Une implémentation pure Python (~120 lignes) suffit :
la vérification de licence n'est exécutée qu'au démarrage et à
l'activation, jamais dans une boucle chaude.

Portée : sign / verify / secret_to_public uniquement (pas de X25519, pas
de dérivation de clés). Validée par les vecteurs de test officiels du
RFC 8032 dans tests/test_licence.py.
"""

from __future__ import annotations

import hashlib

# ---------------------------------------------------------------------------
# Courbe edwards25519 (RFC 8032 §5.1)
# ---------------------------------------------------------------------------
_P = 2**255 - 19                       # premier du corps
_L = 2**252 + 27742317777372353535851937790883648493  # ordre du groupe
_D = (-121665 * pow(121666, _P - 2, _P)) % _P      # paramètre d de la courbe
_I = pow(2, (_P - 1) // 4, _P)                     # sqrt(-1)


def _inv(x: int) -> int:
    """Inverse modulaire dans F_p (petit Fermat)."""
    return pow(x, _P - 2, _P)


def _recover_x(y: int, sign: int):
    """Récupère x à partir de y et du bit de signe (RFC 8032 §5.1.3)."""
    if y >= _P:
        return None
    x2 = (y * y - 1) * _inv(_D * y * y + 1) % _P
    if x2 == 0:
        return None if sign else 0
    x = pow(x2, (_P + 3) // 8, _P)
    if (x * x - x2) % _P != 0:
        x = x * _I % _P
    if (x * x - x2) % _P != 0:
        return None
    if x % 2 != sign:
        x = _P - x
    return x


_By = 4 * _inv(5) % _P
_Bx = _recover_x(_By, 0)
# Point de base B en coordonnées étendues (X, Y, Z, T) avec T = XY/Z.
_B = (_Bx, _By, 1, (_Bx * _By) % _P)
_IDENTITE = (0, 1, 1, 0)


def _point_add(p1, p2):
    """Addition de points en coordonnées étendues (RFC 8032 §5.1.4)."""
    x1, y1, z1, t1 = p1
    x2, y2, z2, t2 = p2
    a = (y1 - x1) * (y2 - x2) % _P
    b = (y1 + x1) * (y2 + x2) % _P
    c = 2 * t1 * t2 * _D % _P
    d = 2 * z1 * z2 % _P
    e, f, g, h = b - a, d - c, d + c, b + a
    return (e * f % _P, g * h % _P, f * g % _P, e * h % _P)


def _point_mul(s: int, point):
    """Multiplication scalaire par double-and-add (gauche→droite)."""
    q = _IDENTITE
    while s > 0:
        if s & 1:
            q = _point_add(q, point)
        point = _point_add(point, point)
        s >>= 1
    return q


def _point_equal(p1, p2) -> bool:
    """Égalité projective : X1·Z2 == X2·Z1 et Y1·Z2 == Y2·Z1."""
    if (p1[0] * p2[2] - p2[0] * p1[2]) % _P != 0:
        return False
    if (p1[1] * p2[2] - p2[1] * p1[2]) % _P != 0:
        return False
    return True


def _point_compress(point) -> bytes:
    """Sérialise un point sur 32 octets (y little-endian + bit de signe de x)."""
    _, _, z, _ = point
    zinv = _inv(z)
    x = point[0] * zinv % _P
    y = point[1] * zinv % _P
    return (y | ((x & 1) << 255)).to_bytes(32, "little")


def _point_decompress(data: bytes):
    """Dé-sérialise 32 octets en point, ou None si invalide."""
    if len(data) != 32:
        return None
    y = int.from_bytes(data, "little")
    sign = y >> 255
    y &= (1 << 255) - 1
    x = _recover_x(y, sign)
    if x is None:
        return None
    return (x, y, 1, (x * y) % _P)


# ---------------------------------------------------------------------------
# API signature (RFC 8032 §5.1.6 / §5.1.7)
# ---------------------------------------------------------------------------
def _secret_expand(secret: bytes):
    if len(secret) != 32:
        raise ValueError("clé privée Ed25519 : 32 octets attendus")
    h = hashlib.sha512(secret).digest()
    a = int.from_bytes(h[:32], "little")
    a &= (1 << 254) - 8
    a |= 1 << 254
    return a, h[32:]


def secret_to_public(secret: bytes) -> bytes:
    """Dérive la clé publique (32 octets) depuis la clé privée."""
    a, _ = _secret_expand(secret)
    return _point_compress(_point_mul(a, _B))


def sign(secret: bytes, message: bytes) -> bytes:
    """Signe `message` ; renvoie la signature de 64 octets (R || S)."""
    a, prefix = _secret_expand(secret)
    public = _point_compress(_point_mul(a, _B))
    r = int.from_bytes(hashlib.sha512(prefix + message).digest(), "little") % _L
    r_encoded = _point_compress(_point_mul(r, _B))
    h = int.from_bytes(
        hashlib.sha512(r_encoded + public + message).digest(), "little"
    ) % _L
    s = (r + h * a) % _L
    return r_encoded + s.to_bytes(32, "little")


def verify(public: bytes, message: bytes, signature: bytes) -> bool:
    """Vérifie une signature Ed25519. True si valide, False sinon."""
    if len(public) != 32 or len(signature) != 64:
        return False
    a_point = _point_decompress(public)
    if a_point is None:
        return False
    r_point = _point_decompress(signature[:32])
    if r_point is None:
        return False
    s = int.from_bytes(signature[32:], "little")
    if s >= _L:
        return False
    h = int.from_bytes(
        hashlib.sha512(signature[:32] + public + message).digest(), "little"
    ) % _L
    left = _point_mul(s, _B)
    right = _point_add(r_point, _point_mul(h, a_point))
    return _point_equal(left, right)
