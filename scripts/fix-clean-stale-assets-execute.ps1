# Set execute permission on clean-stale-assets.sh for CI (exit 126 fix).
# Run from repo root: pwsh -NoProfile -File scripts/fix-clean-stale-assets-execute.ps1

$ErrorActionPreference = "Stop"
# $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
# Set-Location $repoRoot

git update-index --chmod=+x scripts/clean-stale-assets.sh
git add scripts/clean-stale-assets.sh
git commit -m "Fix: set execute permission on clean-stale-assets.sh"
git push
Write-Host "Done: execute bit set, committed, and pushed."
