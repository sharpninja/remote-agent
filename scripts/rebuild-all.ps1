#Requires -Version 7.0
<#
.SYNOPSIS
  Clean and build the full solution (all projects, all target frameworks).
.DESCRIPTION
  Do NOT use -f net10.0-android at solution level (causes NETSDK1005 for non-Android projects).
  Uses Release by default; pass Debug for Debug configuration.
.PARAMETER Config
  Build configuration: Release (default) or Debug.
.EXAMPLE
  .\scripts\rebuild-all.ps1
  .\scripts\rebuild-all.ps1 -Config Debug
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Config = "Release"
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $RepoRoot

$Solution = "RemoteAgent.slnx"

Write-Host "=== Clean ($Config) ==="
dotnet clean $Solution -c $Config -nologo -v m

Write-Host "=== Build ($Config) ==="
dotnet build $Solution -c $Config -nologo -v m

Write-Host "=== Rebuild complete ==="
