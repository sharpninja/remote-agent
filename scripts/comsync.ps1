# comsync: stage all modified, commit, push. Run from repo root. Output to comsync-result.txt
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)
$out = "comsync-result.txt"

git status --short 2>&1 | Out-File $out
$status = Get-Content $out -Raw
if (-not $status -or $status.Trim() -eq "") { "Nothing to commit." | Out-File $out; exit 0 }

git add -A
$msg = "Sync: desktop build .NET 9 global.json, testing strategy, capacity/prompt tests, scripts"
git commit -m $msg 2>&1 | Out-File $out -Append
git push 2>&1 | Out-File $out -Append
"---" | Out-File $out -Append
git log -1 --oneline 2>&1 | Out-File $out -Append
