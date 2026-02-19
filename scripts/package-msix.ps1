#Requires -Version 7.0
<#
.SYNOPSIS
  Build a combined MSIX package for the Remote Agent service and desktop management app.

.DESCRIPTION
  Publishes the service (net10.0, win-x64) and desktop app (net9.0, win-x64), assembles
  them into a single MSIX package, and optionally signs it.

  The resulting package installs both:
    - RemoteAgent.Service.exe, registered as a Windows service (RemoteAgentService) that
      starts automatically under LocalSystem via the MSIX windows.service extension.
    - RemoteAgent.Desktop.exe, visible in the Start menu as "Remote Agent Desktop".

  The Ollama plugin DLL is included in the service sub-directory when it has been built.

  Install layout inside the package:
    service\          service binaries + appsettings.json
    service\plugins\  Ollama plugin DLL (if built)
    desktop\          desktop app binaries
    Assets\           MSIX icon assets

  Requirements:
    - Windows SDK >= 10.0.19041  (makeappx.exe, signtool.exe)
    - .NET SDK 10.x (service + plugin) and 9.x (desktop; via src/RemoteAgent.Desktop/global.json)

  Signing:
    -DevCert        Create a temporary self-signed certificate (development / CI testing).
    -CertThumbprint Use an existing certificate from the current user's My store.
    Neither flag    Build an unsigned package (can be installed with Add-AppxPackage -AllowUnsigned).

  After building, install on the same machine:
    Add-AppxPackage -Path .\artifacts\remote-agent_<ver>_x64.msix

  To trust a self-signed dev cert on the target machine before installing:
    $cert = (Get-PfxCertificate -FilePath artifacts\remote-agent-dev.cer)
    Import-Certificate -Certificate $cert -CertStoreLocation Cert:\LocalMachine\Root

.PARAMETER Configuration
  Build configuration: Release (default) or Debug.

.PARAMETER Version
  Package version (e.g. 1.2.3). Defaults to GitVersion SemVer, then most recent git tag, then next-version from GitVersion.yml.

.PARAMETER Publisher
  MSIX Identity Publisher string, must match the signing certificate Subject exactly.
  Default: "CN=RemoteAgent Dev".

.PARAMETER CertThumbprint
  SHA1 thumbprint of an existing certificate in Cert:\CurrentUser\My to sign with.
  Mutually exclusive with -DevCert.

.PARAMETER DevCert
  Create (or reuse) a self-signed certificate and sign the package with it.
  The public certificate is exported to <OutDir>\remote-agent-dev.cer.

.PARAMETER SelfContained
  Publish self-contained, single-file packages (bundles .NET runtime; no runtime prereq on target).
  Default: $true. Pass -SelfContained $false for a framework-dependent build.

.PARAMETER Clean
  Run 'dotnet clean' on each project before publishing.

.PARAMETER Install
  After packaging, call install-remote-agent.ps1 to install the package and start the service.
  Requires the script to be run as Administrator.

.PARAMETER ServiceOnly
  Build and bundle only the service component; omit the desktop app.

.PARAMETER DesktopOnly
  Build and bundle only the desktop component; omit the service and Windows service extension.

.PARAMETER BumpMajor
  Increment the major component of next-version in GitVersion.yml before building
  (resets minor and patch to 0). Mutually exclusive with -BumpMinor and -BumpPatch.

.PARAMETER BumpMinor
  Increment the minor component of next-version in GitVersion.yml before building
  (resets patch to 0). Mutually exclusive with -BumpMajor and -BumpPatch.

.PARAMETER BumpPatch
  Increment the patch component of next-version in GitVersion.yml before building.
  Mutually exclusive with -BumpMajor and -BumpMinor.

.PARAMETER OutDir
  Output directory for the .msix file. Default: <repo-root>\artifacts\.

.EXAMPLE
  .\scripts\package-msix.ps1 -DevCert
  .\scripts\package-msix.ps1 -Configuration Release -DevCert -SelfContained
  .\scripts\package-msix.ps1 -CertThumbprint "ABC123DEF456..." -Version 2.1.0
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Version = "",

    [string] $Publisher = "CN=RemoteAgent Dev",

    [string] $CertThumbprint = "",

    [switch] $DevCert,

    [bool] $SelfContained = $true,

    [switch] $ServiceOnly,

    [switch] $DesktopOnly,

    [switch] $Clean,

    [switch] $Install,

    [switch] $Force,

    [switch] $BumpMajor,

    [switch] $BumpMinor,

    [switch] $BumpPatch,

    [string] $OutDir = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName

# ── Validate mutually exclusive flags ────────────────────────────────────────
if ($ServiceOnly -and $DesktopOnly) { Write-Error "-ServiceOnly and -DesktopOnly are mutually exclusive." }
if ($DevCert -and $CertThumbprint)  { Write-Error "-DevCert and -CertThumbprint are mutually exclusive." }
if (($BumpMajor.IsPresent + $BumpMinor.IsPresent + $BumpPatch.IsPresent) -gt 1) {
    Write-Error "-BumpMajor, -BumpMinor, and -BumpPatch are mutually exclusive."
}

# ── Version bump (modifies GitVersion.yml before the module reads it) ─────────
if ($BumpMajor -or $BumpMinor -or $BumpPatch) {
    $gvYml   = Join-Path $RepoRoot "GitVersion.yml"
    if (-not (Test-Path $gvYml)) { Write-Error "GitVersion.yml not found at $gvYml." }
    $current = (Get-Content $gvYml | Select-String '^\s*next-version:\s*(.+)').Matches[0].Groups[1].Value.Trim()
    $parts   = $current -split '\.'
    if ($parts.Count -lt 3 -or ($parts | Where-Object { $_ -notmatch '^\d+$' })) {
        Write-Error "Cannot parse next-version '$current' in GitVersion.yml."
    }
    [int]$maj = $parts[0]; [int]$min = $parts[1]; [int]$pat = $parts[2]
    if      ($BumpMajor) { $maj++; $min = 0; $pat = 0 }
    elseif  ($BumpMinor) { $min++; $pat = 0 }
    else                 { $pat++ }
    $newVer = "$maj.$min.$pat"
    (Get-Content $gvYml -Raw) -replace '(?m)^(next-version:\s*).*', "`${1}$newVer" |
        Set-Content $gvYml -Encoding UTF8
    Write-Host "[package-msix] bumped next-version: $current -> $newVer"
}

# ── Delegate to MsixTools module ──────────────────────────────────────────────
Import-Module (Join-Path $PSScriptRoot "MsixTools\MsixTools.psd1") -Force

$params = @{
    WorkspaceRoot = $RepoRoot
    ConfigPath    = Join-Path $RepoRoot "msix.yml"
    Clean         = $Clean
    Force         = $Force
    Install       = $Install
}

# Only forward params that were explicitly supplied (let YAML supply the rest).
if ($Configuration)  { $params['Configuration']  = $Configuration }
if ($Version)        { $params['Version']         = $Version }
if ($Publisher)      { $params['Publisher']       = $Publisher }
if ($CertThumbprint) { $params['CertThumbprint']  = $CertThumbprint }
if ($DevCert)        { $params['DevCert']         = $true }
if ($OutDir)         { $params['OutDir']          = $OutDir }
if ($ServiceOnly)    { $params['ExcludeDesktop']  = $true }
if ($DesktopOnly)    { $params['ExcludeService']  = $true }
if ($PSBoundParameters.ContainsKey('SelfContained')) { $params['SelfContained'] = $SelfContained }

New-MsixPackage @params

