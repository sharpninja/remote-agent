#!/usr/bin/env bash
# "Sync all" workflow: commit, sync with remote, monitor GitHub Actions run,
# and on success update the local Docker container.
# Usage: ./scripts/sync-all.sh [commit_message]
#   commit_message  Optional. Default: "sync"

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

COMMIT_MSG="${1:-sync}"

echo "=== 1. Commit ==="
if git status --porcelain | grep -q .; then
  git add -A
  git commit -m "$COMMIT_MSG"
else
  echo "Nothing to commit."
fi

echo "=== 2. Sync (pull --rebase, push) ==="
BRANCH="$(git branch --show-current)"
git pull --rebase origin "$BRANCH"
git push origin "$BRANCH"

echo "=== 3. Monitor pipeline and update container on success ==="
exec "$REPO_ROOT/scripts/watch-and-update-container.sh"
