#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "[assets] scanning for stale Windows-generated project.assets.json files"

while IFS= read -r -d '' assets_file; do
  if grep -Eq 'C:\\\\Program Files \(x86\)\\\\Microsoft Visual Studio\\\\Shared\\\\NuGetPackages|[A-Za-z]:\\\\Users\\\\|[A-Za-z]:\\\\github\\\\' "$assets_file"; then
    echo "[assets] removing stale file: $assets_file"
    rm -f "$assets_file"
  fi
done < <(find "$REPO_ROOT/src" "$REPO_ROOT/tests" -type f -path '*/obj/project.assets.json' -print0 2>/dev/null)

echo "[assets] stale asset cleanup complete"
