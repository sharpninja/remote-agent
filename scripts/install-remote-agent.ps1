#Requires -Version 7.0
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install the Remote Agent MSIX package and immediately start the Windows service.

.DESCRIPTION
  1. Optionally trusts the signing certificate (required for self-signed dev builds).
  2. Installs the MSIX package via Add-AppxPackage (or updates an existing install).
  3. Polls the Windows SCM until RemoteAgentService is registered (up to -TimeoutSeconds).
  4. Starts the service and reports its final status.

  The service is registered by the MSIX windows.service extension with StartupType=auto,
  so it will also start automatically on every subsequent boot.

  Run as Administrator — Add-AppxPackage with a service extension requires elevation.

.PARAMETER MsixPath
  Path to the .msix file. Defaults to the most recent remote-agent_*.msix in
  <repo-root>\artifacts\.

.PARAMETER CertPath
  Path to a .cer file to trust before installing (required for self-signed dev packages).
  Imports the certificate into Cert:\LocalMachine\Root.

.PARAMETER TimeoutSeconds
  Seconds to wait for the service to appear in the SCM after package install. Default: 30.

.PARAMETER Uninstall
  Remove the installed package and stop the service instead of installing.

.EXAMPLE
  # Build then install (dev machine):
  .\scripts\package-msix.ps1 -DevCert
  .\scripts\install-remote-agent.ps1 -CertPath artifacts\remote-agent-dev.cer

  # Install a release-signed package:
  .\scripts\install-remote-agent.ps1 -MsixPath artifacts\remote-agent_2.1.0_x64.msix

  # Uninstall:
  .\scripts\install-remote-agent.ps1 -Uninstall
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $MsixPath = "",
    [string] $CertPath = "",
    [int]    $TimeoutSeconds = 30,
    [switch] $Uninstall
)

$ErrorActionPreference = "Stop"
$RepoRoot  = (Get-Item $PSScriptRoot).Parent.FullName
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$ServiceName  = "RemoteAgentService"
$PackageFamilyPattern = "RemoteAgent*"

# ── Uninstall path ────────────────────────────────────────────────────────────
if ($Uninstall) {
    Write-Host "[install] Stopping service '$ServiceName'..."
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne "Stopped") {
        Stop-Service $ServiceName -Force
        Write-Host "[install] Service stopped."
    } else {
        Write-Host "[install] Service not running or not found."
    }

    Write-Host "[install] Removing MSIX package..."
    $pkg = Get-AppxPackage -Name "RemoteAgent" -ErrorAction SilentlyContinue
    if ($pkg) {
        Remove-AppxPackage -Package $pkg.PackageFullName
        Write-Host "[install] Package removed: $($pkg.PackageFullName)"
    } else {
        Write-Host "[install] No installed RemoteAgent package found."
    }
    exit 0
}

# ── Resolve MSIX path ─────────────────────────────────────────────────────────
if (-not $MsixPath) {
    $candidates = Get-ChildItem $ArtifactsDir -Filter "remote-agent_*.msix" -ErrorAction SilentlyContinue |
                  Sort-Object LastWriteTime -Descending
    if (-not $candidates) {
        Write-Error "No remote-agent_*.msix found in $ArtifactsDir. Run scripts\package-msix.ps1 first."
    }
    $MsixPath = $candidates[0].FullName
    Write-Host "[install] Using most recent package: $MsixPath"
}

if (-not (Test-Path $MsixPath)) {
    Write-Error "MSIX file not found: $MsixPath"
}

# ── Trust signing certificate ─────────────────────────────────────────────────
if ($CertPath) {
    if (-not (Test-Path $CertPath)) {
        Write-Error "Certificate file not found: $CertPath"
    }
    Write-Host "[install] Importing certificate into Cert:\LocalMachine\Root..."
    Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Write-Host "[install] Certificate trusted."
}

# ── Install or update the MSIX ────────────────────────────────────────────────
$existing = Get-AppxPackage -Name "RemoteAgent" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "[install] Updating existing package ($($existing.Version) -> installing)..."
    Add-AppxPackage -Path $MsixPath -ForceUpdateFromAnyVersion
} else {
    Write-Host "[install] Installing package..."
    Add-AppxPackage -Path $MsixPath
}
Write-Host "[install] Package installed."

# ── Wait for the service to be registered in the SCM ─────────────────────────
# The MSIX windows.service extension registers the service asynchronously after
# the package install completes; poll until it appears or the timeout expires.
Write-Host "[install] Waiting for '$ServiceName' to be registered in the SCM..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$svc = $null
while ((Get-Date) -lt $deadline) {
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc) { break }
    Start-Sleep -Milliseconds 500
}

if (-not $svc) {
    Write-Error @"
Timed out after ${TimeoutSeconds}s waiting for '$ServiceName' to appear in the SCM.
The MSIX package installed successfully but the service extension may not have registered yet.
Try starting manually: Start-Service $ServiceName
"@
}

Write-Host "[install] Service registered (status: $($svc.Status))."

# ── Start the service ─────────────────────────────────────────────────────────
if ($svc.Status -eq "Running") {
    Write-Host "[install] Service is already running."
} else {
    Write-Host "[install] Starting '$ServiceName'..."
    Start-Service $ServiceName
    # Give it a moment and re-query.
    Start-Sleep -Seconds 2
    $svc = Get-Service $ServiceName
    Write-Host "[install] Service status: $($svc.Status)"
    if ($svc.Status -ne "Running") {
        Write-Warning "Service did not reach Running state. Check Event Viewer for details."
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────
$pkg = Get-AppxPackage -Name "RemoteAgent"
Write-Host ""
Write-Host "── Remote Agent installed ───────────────────────────────────────────────"
Write-Host "  Package : $($pkg.PackageFullName)"
Write-Host "  Version : $($pkg.Version)"
Write-Host "  Service : $ServiceName  [$($svc.Status)]  (StartType: Automatic)"
Write-Host ""
Write-Host "  Service management:"
Write-Host "    Stop-Service    $ServiceName"
Write-Host "    Start-Service   $ServiceName"
Write-Host "    Restart-Service $ServiceName"
Write-Host ""
Write-Host "  Uninstall:"
Write-Host "    .\scripts\install-remote-agent.ps1 -Uninstall"
Write-Host "────────────────────────────────────────────────────────────────────────"
