#!/usr/bin/env bash
# "Sync all" workflow: commit, sync with remote, monitor GitHub Actions run,
# and on success update the local Docker container.
# On non-main branches (e.g. develop): always push directly.
# On main: push directly unless --pr (or USE_PR=1) to use a pull request.
# Usage: ./scripts/sync-all.sh [commit_message]
#        ./scripts/sync-all.sh --pr [commit_message]   (main only)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

USE_PR="${USE_PR:-0}"
COMMIT_MSG="sync"
for arg in "$@"; do
  if [ "$arg" = "--pr" ]; then
    USE_PR=1
  else
    COMMIT_MSG="$arg"
  fi
done

echo "=== 1. Commit ==="
if git status --porcelain | grep -q .; then
  git add -A
  git commit -m "$COMMIT_MSG"
else
  echo "Nothing to commit."
fi

BRANCH="$(git branch --show-current)"

# On non-main branches (e.g. develop), always push directly. PR flow only on main when --pr.
if [ "$BRANCH" = "main" ] && [ "$USE_PR" = "1" ]; then
  echo "=== 2. Sync via PR (branch → push → PR → merge) ==="
  SYNC_BRANCH="sync/$(date +%Y%m%d-%H%M%S)"
  git checkout -b "$SYNC_BRANCH"
  git push -u origin "$SYNC_BRANCH"
  PR_URL="$(gh pr create --base main --fill --title "Sync: $COMMIT_MSG")"
  echo "Created PR: $PR_URL"
  if gh pr merge "$PR_URL" --squash 2>/dev/null || gh pr merge "$PR_URL" --merge 2>/dev/null; then
    echo "Merged."
  else
    echo "Merge failed (e.g. required checks or deployment). Merge from the GitHub UI when ready, then run: $REPO_ROOT/scripts/watch-and-update-container.sh"
    git checkout main
    git branch -D "$SYNC_BRANCH" 2>/dev/null || true
    exit 0
  fi
  git checkout main
  git pull origin main
  git branch -D "$SYNC_BRANCH" 2>/dev/null || true
else
  echo "=== 2. Sync (pull --rebase, push) ==="
  git pull --rebase origin "$BRANCH"
  git push origin "$BRANCH"
fi

echo "=== 3. Monitor pipeline and update container on success ==="
exec "$REPO_ROOT/scripts/watch-and-update-container.sh"
