#Requires -Version 7.0
<#
.SYNOPSIS
  Build a combined MSIX package for the Remote Agent service and desktop management app.

.DESCRIPTION
  Wrapper around New-MsixPackage (scripts/MsixTools). Reads all project and build
  configuration from msix.yml in the repository root.

.PARAMETER Configuration
  Build configuration: Release (default) or Debug.

.PARAMETER Version
  Package version (e.g. 1.2.3). Auto-detected if omitted.

.PARAMETER Publisher
  MSIX Identity Publisher string. Default: read from msix.yml.

.PARAMETER CertThumbprint
  SHA1 thumbprint of a certificate in Cert:\CurrentUser\My. Mutually exclusive with -DevCert.

.PARAMETER DevCert
  Create/reuse a self-signed dev certificate. Mutually exclusive with -CertThumbprint.

.PARAMETER SelfContained
  Publish self-contained single-file. Default: true.

.PARAMETER ServiceOnly
  Omit the desktop app from the package.

.PARAMETER DesktopOnly
  Omit the service and windows.service extension from the package.

.PARAMETER Clean
  Delete bin/ and obj/ before publishing. Mutually exclusive with -NoBuild.

.PARAMETER NoBuild
  Skip dotnet publish; repackage existing artifacts\publish-* output. Mutually exclusive with -Clean.

.PARAMETER BumpMajor
  Increment major version in GitVersion.yml before building. Mutually exclusive with -BumpMinor/-BumpPatch.

.PARAMETER BumpMinor
  Increment minor version in GitVersion.yml before building. Mutually exclusive with -BumpMajor/-BumpPatch.

.PARAMETER BumpPatch
  Increment patch version in GitVersion.yml before building. Mutually exclusive with -BumpMajor/-BumpMinor.

.PARAMETER Install
  Install the MSIX and start the service after packaging. Requires Administrator.

.PARAMETER Force
  Skip the AppxManifest review pause.

.PARAMETER OutDir
  Output directory. Default: <repo-root>\artifacts.

.EXAMPLE
  .\scripts\package-msix.ps1 -DevCert -Force -Install
  .\scripts\package-msix.ps1 -BumpPatch -Clean -Force -Install
  .\scripts\package-msix.ps1 -NoBuild -Force
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $Version = '',
    [string] $Publisher = '',
    [string] $CertThumbprint = '',
    [switch] $DevCert,
    [bool]   $SelfContained = $true,
    [switch] $ServiceOnly,
    [switch] $DesktopOnly,
    [switch] $Clean,
    [switch] $NoBuild,
    [switch] $BumpMajor,
    [switch] $BumpMinor,
    [switch] $BumpPatch,
    [switch] $Install,
    [switch] $Force,
    [string] $OutDir = ''
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Import-Module (Join-Path $PSScriptRoot 'MsixTools\MsixTools.psd1') -Force

$params = @{
    WorkspaceRoot = $RepoRoot
    ConfigPath    = Join-Path $RepoRoot 'msix.yml'
    Configuration = $Configuration
    SelfContained = $SelfContained
    Clean         = $Clean
    NoBuild       = $NoBuild
    BumpMajor     = $BumpMajor
    BumpMinor     = $BumpMinor
    BumpPatch     = $BumpPatch
    Force         = $Force
    Install       = $Install
}
if ($Version)        { $params['Version']        = $Version }
if ($Publisher)      { $params['Publisher']      = $Publisher }
if ($CertThumbprint) { $params['CertThumbprint'] = $CertThumbprint }
if ($DevCert)        { $params['DevCert']        = $true }
if ($OutDir)         { $params['OutDir']         = $OutDir }
if ($ServiceOnly)    { $params['ExcludeDesktop'] = $true }
if ($DesktopOnly)    { $params['ExcludeService'] = $true }

New-MsixPackage @params
