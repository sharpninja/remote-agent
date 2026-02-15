#!/usr/bin/env bash
# Delete outdated GHCR container versions, keeping the N most recent.
# Requires: gh (GitHub CLI) logged in, with read:packages and delete:packages.
# Usage: ./ghcr-prune-versions.sh [package_name] [keep_count]
#        GH_OWNER=myorg ./ghcr-prune-versions.sh [package_name] [keep_count]  # org-owned package
#   package_name  Default: remote-agent/service (ghcr.io/OWNER/remote-agent/service)
#   keep_count    Default: 2

set -euo pipefail

PACKAGE_NAME="${1:-remote-agent/service}"
KEEP="${2:-2}"

# URL-encode the package name (slash -> %2F)
PACKAGE_ENCODED="${PACKAGE_NAME//\//%2F}"

if [ -n "${GH_OWNER:-}" ]; then
  API_PREFIX="orgs/${GH_OWNER}"
else
  API_PREFIX="user"
fi

echo "Package: $PACKAGE_NAME (keeping $KEEP most recent versions)"
echo "Fetching versions from GHCR (${API_PREFIX})..."

VERSIONS_JSON=$(gh api "${API_PREFIX}/packages/container/${PACKAGE_ENCODED}/versions" --paginate 2>/dev/null) || {
  echo "Error: Failed to list versions. Check 'gh auth status' and package name." >&2
  echo "For org packages set GH_OWNER=yourorg" >&2
  exit 1
}

# Extract id and created_at, sort by created_at descending, then take IDs
# jq: sort by created_at (newest first), skip first KEEP, then output .id for delete
TO_DELETE=$(echo "$VERSIONS_JSON" | jq -r --argjson keep "$KEEP" '
  if type == "array" then . else [.] end
  | sort_by(.created_at) | reverse
  | .[$keep:]
  | .[].id
')

if [ -z "$TO_DELETE" ]; then
  echo "Nothing to delete (≤ $KEEP versions)."
  exit 0
fi

COUNT=$(echo "$TO_DELETE" | wc -l)
echo "Deleting $COUNT outdated version(s)..."

for ID in $TO_DELETE; do
  echo "  Deleting version id: $ID"
  gh api -X DELETE "${API_PREFIX}/packages/container/${PACKAGE_ENCODED}/versions/${ID}" || true
done

echo "Done."
