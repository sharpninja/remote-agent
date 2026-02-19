#Requires -Version 7.0
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install (or uninstall) the Remote Agent MSIX package and manage the Windows service.

.DESCRIPTION
  Wrapper around the MsixTools module's Install-MsixPackage / Uninstall-MsixPackage functions.
  Package, service, and path configuration is read from msix.yml in the repository root.

  Install flow:
    1. Optionally trust the signing certificate (-CertPath).
    2. Auto-sign unsigned packages with a self-signed dev cert if needed.
    3. Stop RemoteAgentService and close RemoteAgent.Desktop before updating.
    4. Remove any existing version, then fresh-install (avoids HRESULT 0x80073CFB).
    5. Wait for the service to register in the SCM and start it.

.PARAMETER MsixPath
  Path to the .msix file. Defaults to the most recent remote-agent_*.msix in artifacts\.

.PARAMETER CertPath
  Path to a .cer file to trust in Cert:\LocalMachine\Root before installing.

.PARAMETER TimeoutSeconds
  Seconds to wait for RemoteAgentService to register in the SCM after install. Default: 30.

.PARAMETER Uninstall
  Stop the service and remove the installed package instead of installing.

.EXAMPLE
  # Build then install:
  .\scripts\package-msix.ps1 -DevCert -Force -Install

  # Install a specific package:
  .\scripts\install-remote-agent.ps1 -MsixPath artifacts\remote-agent_0.1.0_x64.msix

  # Uninstall:
  .\scripts\install-remote-agent.ps1 -Uninstall
#>

[CmdletBinding()]
param(
    [string] $MsixPath = "",
    [string] $CertPath = "",
    [int]    $TimeoutSeconds = 30,
    [switch] $Uninstall
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Import-Module (Join-Path $PSScriptRoot "MsixTools\MsixTools.psd1") -Force

$configPath = Join-Path $RepoRoot "msix.yml"

if ($Uninstall) {
    Uninstall-MsixPackage -ConfigPath $configPath
    exit 0
}

$params = @{
    ConfigPath     = $configPath
    TimeoutSeconds = $TimeoutSeconds
}
if ($MsixPath) { $params['MsixPath'] = $MsixPath }
if ($CertPath) { $params['CertPath'] = $CertPath }

Install-MsixPackage @params
