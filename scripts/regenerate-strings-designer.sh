#!/usr/bin/env bash
# Regenerates src/InstallerClean.Core/Resources/Strings.Designer.cs from
# Strings.resx. Run after adding, removing, or renaming a key in the
# .resx so the typed accessor surface stays in sync.
#
# The file is built by scripts/strings-designer.mjs, which
# check-designer-parity.mjs also builds it with to compare against the
# committed copy, so what this writes is exactly what the check expects.
set -euo pipefail

ROOT=$(cd "$(dirname "$0")/.." && pwd)
cd "$ROOT"
exec node scripts/strings-designer.mjs
