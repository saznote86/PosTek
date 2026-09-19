#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Assistant de migration graphico-guidée Leo2 → POSTEK.

Le but est de donner une interface de progression en étapes, sans dépendre
d'un framework UI. Le moteur `MoteurMigration` reste la vraie source de vérité,
pendant que l'assistant fournit un retour lisible en étapes pour un wizard.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

from .engine import MoteurMigration, ecrire_rapport


class AssistantMigration:
    """Encapsule une migration avec un flux d'étapes clonable pour UI."""

    def __init__(self, dossier_exports: str | Path, base_sqlite: str | Path):
        self.dossier_exports = Path(dossier_exports)
        self.base_sqlite = Path(base_sqlite)

    def executer(self) -> dict[str, Any]:
        moteur = MoteurMigration(self.dossier_exports, self.base_sqlite)
        steps: list[dict[str, Any]] = []

        analyse = moteur.analyser()
        steps.append({
            "etape": "analyse",
            "ok": analyse.ok,
            "details": analyse.anomalies,
        })

        if not analyse.ok or not analyse.tables:
            return {
                "ok": False,
                "etapes": steps,
                "message": "Aucun export valide détecté pour la migration.",
            }

        rap = moteur.executer()
        steps.append({
            "etape": "sauvegarde",
            "ok": True,
            "details": [rap.backup] if rap.backup else ["pas de sauvegarde nécessaire"],
        })
        steps.append({
            "etape": "import",
            "ok": not any(len(t.erreurs) for t in rap.tables),
            "details": [f"{t.table}: {t.lignes_importees} lignes" for t in rap.tables],
        })
        steps.append({
            "etape": "validation",
            "ok": not rap.anomalies,
            "details": rap.anomalies,
        })

        rapport = self.base_sqlite.with_name("rapport_migration.json")
        ecrire_rapport(rap, rapport)
        steps.append({
            "etape": "rapport",
            "ok": rap.ok,
            "details": [rapport.as_posix()],
        })

        return {
            "ok": rap.ok,
            "etapes": steps,
            "rapport": str(rapport),
            "backup": rap.backup,
            "anomalies": rap.anomalies,
            "avertissements": rap.avertissements,
        }
