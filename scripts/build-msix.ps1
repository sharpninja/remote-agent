#Requires -Version 7.0
<#
.SYNOPSIS
  Convenience script to build and optionally install the Remote Agent MSIX.

.DESCRIPTION
  Simplified wrapper around New-MsixPackage (scripts/MsixTools).
  For full control use package-msix.ps1.

.PARAMETER Configuration
  Build configuration: Release (default) or Debug.

.PARAMETER SelfContained
  Bundle the .NET runtime. Default: true.

.PARAMETER Install
  Install the MSIX and start the service after packaging. Requires Administrator.

.PARAMETER NoCert
  Skip signing (auto-signed on install by Install-MsixPackage).

.PARAMETER NoBuild
  Skip dotnet publish; repackage existing artifacts\publish-* output.

.PARAMETER Force
  Skip the AppxManifest review pause.

.EXAMPLE
  .\scripts\build-msix.ps1
  .\scripts\build-msix.ps1 -Install
  .\scripts\build-msix.ps1 -NoBuild -Force
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [bool]   $SelfContained = $true,
    [switch] $Install,
    [switch] $NoCert,
    [switch] $NoBuild,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Import-Module (Join-Path $PSScriptRoot 'MsixTools\MsixTools.psd1') -Force

$params = @{
    WorkspaceRoot = $RepoRoot
    ConfigPath    = Join-Path $RepoRoot 'msix.yml'
    Configuration = $Configuration
    SelfContained = $SelfContained
    NoBuild       = $NoBuild
    Force         = $Force
    Install       = $Install
}
if (-not $NoCert) { $params['DevCert'] = $true }

New-MsixPackage @params
