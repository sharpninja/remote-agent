#Requires -Version 7.0
<#
.SYNOPSIS
  Build the Remote Agent MSIX package and optionally install it.

.DESCRIPTION
  Convenience wrapper around scripts/package-msix.ps1.
  Publishes the service (net10.0) and desktop app (net9.0), packs a combined
  MSIX, signs it with a self-signed dev certificate, and — when -Install is
  specified — installs the package and starts the Windows service.

  Equivalent to:
    .\scripts\package-msix.ps1 -Configuration <Config> -DevCert [-SelfContained]
    .\scripts\install-remote-agent.ps1 -CertPath artifacts\remote-agent-dev.cer   (if -Install)

.PARAMETER Configuration
  Build configuration: Release (default) or Debug.

.PARAMETER SelfContained
  Bundle the .NET runtime inside the package (no runtime prereq on the target
  machine). Results in a larger MSIX. Default: false.

.PARAMETER Install
  After building, install the MSIX on this machine and start the service.
  Requires the script to be run as Administrator.

.PARAMETER NoCert
  Skip self-signed certificate creation and produce an unsigned package.
  The package can still be installed locally with Add-AppxPackage -AllowUnsigned.

.EXAMPLE
  # Build a signed dev package (Release):
  .\scripts\build-msix.ps1

  # Build Debug, bundle runtime:
  .\scripts\build-msix.ps1 -Configuration Debug -SelfContained

  # Build and install on this machine:
  .\scripts\build-msix.ps1 -Install

  # Build unsigned (no cert):
  .\scripts\build-msix.ps1 -NoCert
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [switch] $SelfContained,

    [switch] $Install,

    [switch] $NoCert
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RepoRoot  = (Get-Item $ScriptDir).Parent.FullName
$ArtifactsDir = Join-Path $RepoRoot "artifacts"

# ── Validate ──────────────────────────────────────────────────────────────────
if ($Install -and -not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "-Install requires the script to be run as Administrator. Re-run from an elevated prompt."
}

# ── Build the MSIX ────────────────────────────────────────────────────────────
Write-Host "[build-msix] configuration : $Configuration"
Write-Host "[build-msix] self-contained: $SelfContained"
Write-Host "[build-msix] sign (dev cert): $(-not $NoCert)"
Write-Host ""

$packageParams = @{
    Configuration = $Configuration
    OutDir        = $ArtifactsDir
}
if ($SelfContained) { $packageParams["SelfContained"] = $true }
if (-not $NoCert)   { $packageParams["DevCert"]       = $true }

& "$ScriptDir\package-msix.ps1" @packageParams
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Locate the generated .msix file and display its full path.
$msixFile = Get-ChildItem -Path $ArtifactsDir -Filter "*.msix" -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $msixFile) {
    Write-Error "[build-msix] MSIX package not found in $ArtifactsDir after packaging."
}
$MsixPath = $msixFile.FullName

# ── Install (optional) ────────────────────────────────────────────────────────
if ($Install) {
    Write-Host ""
    Write-Host "[build-msix] installing package and starting service..."

    $installParams = @{}
    $cerPath = Join-Path $ArtifactsDir "remote-agent-dev.cer"
    if (-not $NoCert -and (Test-Path $cerPath)) {
        $installParams["CertPath"] = $cerPath
    }

    & "$ScriptDir\install-remote-agent.ps1" @installParams
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ""
Write-Host "[build-msix] done.  MSIX: $MsixPath"
