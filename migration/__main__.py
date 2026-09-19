#!/usr/bin/env python3
"""Point d'entrée JSON pour le wizard de migration WPF."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from .assistant import AssistantMigration


def main() -> int:
    parser = argparse.ArgumentParser(description="Migration Leo2 vers POSTEK")
    parser.add_argument("--exports", required=True, type=Path)
    parser.add_argument("--base", required=True, type=Path)
    args = parser.parse_args()
    result = AssistantMigration(args.exports, args.base).executer()
    print(json.dumps(result, ensure_ascii=False))
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
