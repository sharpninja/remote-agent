#!/usr/bin/env bash
# bump-minor-version.sh
# Increments the minor version in GitVersion.yml and resets the patch to 0.
# Usage: ./scripts/bump-minor-version.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GV_FILE="$REPO_ROOT/GitVersion.yml"

# Extract current next-version value (e.g. "0.1.0")
current=$(grep -E '^next-version:' "$GV_FILE" | sed 's/next-version:[[:space:]]*//')

IFS='.' read -r major minor patch <<< "$current"

if [[ -z "$major" || -z "$minor" || -z "$patch" ]]; then
    echo "error: could not parse next-version '$current' from $GV_FILE" >&2
    exit 1
fi

new_version="$major.$((minor + 1)).0"

sed -i "s/^next-version:.*/next-version: $new_version/" "$GV_FILE"

echo "Bumped next-version: $current -> $new_version"
