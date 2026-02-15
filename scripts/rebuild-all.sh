#!/usr/bin/env bash
# Clean and build the full solution (all projects, all target frameworks).
# Do NOT use -f net10.0-android at solution level (causes NETSDK1005 for non-Android projects).
# Usage: ./scripts/rebuild-all.sh [Debug|Release]

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

CONFIG="${1:-Release}"
SOLUTION="RemoteAgent.slnx"

echo "=== Clean ($CONFIG) ==="
dotnet clean "$SOLUTION" -c "$CONFIG" -nologo -v m

echo "=== Build ($CONFIG) ==="
dotnet build "$SOLUTION" -c "$CONFIG" -nologo -v m

echo "=== Rebuild complete ==="
