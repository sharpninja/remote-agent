#!/usr/bin/env bash
# Run from repo root in WSL: bash scripts/commit-and-push-wsl.sh
# Writes result to comsync-result.txt in repo root.
set -e
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
echo "Running from $REPO_ROOT"
cd "$REPO_ROOT"
OUT="$REPO_ROOT/comsync-result.txt"
{
  git status --short
  git add -A
  git commit -m "Sync: desktop build .NET 9 global.json, testing strategy, capacity/prompt tests, scripts" || true
  git push
  echo "---"
  git log -1 --oneline
} 2>&1 | tee "$OUT"
