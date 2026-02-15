# Commit and push; write result to stdout and to commit-result.txt for verification.
$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $RepoRoot
$OutFile = Join-Path $RepoRoot "commit-result.txt"
$msg = "FR-11.1 multiple sessions; build fixes; rebuild-all script; fix connection state UI thread"
"=== git add -A ===" | Tee-Object -FilePath $OutFile
git add -A 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== git commit ===" | Tee-Object -FilePath $OutFile -Append
git commit -m $msg 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== git push ===" | Tee-Object -FilePath $OutFile -Append
git push 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== done ===" | Tee-Object -FilePath $OutFile -Append
