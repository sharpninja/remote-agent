# Commit and push; write result to stdout and to commit-result.txt for verification.
# Usage: .\scripts\commit-and-sync.ps1 -Msg "Your commit message"
param(
    [Parameter(Mandatory = $true)]
    [string] $Msg
)
$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $RepoRoot
$OutFile = Join-Path $RepoRoot "commit-result.txt"
"=== git add -A ===" | Tee-Object -FilePath $OutFile
git add -A 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== git commit ===" | Tee-Object -FilePath $OutFile -Append
git commit -m $Msg 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== git push ===" | Tee-Object -FilePath $OutFile -Append
git push 2>&1 | Tee-Object -FilePath $OutFile -Append
"=== done ===" | Tee-Object -FilePath $OutFile -Append
Invoke-Item $OutFile
