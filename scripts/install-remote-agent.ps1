#Requires -Version 7.0
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install the Remote Agent MSIX package and immediately start the Windows service.

.DESCRIPTION
  1. Optionally trusts the signing certificate (required for self-signed dev builds).
  2. If the package is unsigned, automatically signs it with a self-signed dev certificate.
  3. Installs the MSIX package via Add-AppxPackage (or updates an existing install).
  4. Polls the Windows SCM until RemoteAgentService is registered (up to -TimeoutSeconds).
  5. Starts the service and reports its final status.

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

# ── Auto-sign unsigned packages ───────────────────────────────────────────────
# -AllowUnsigned requires Developer Mode; signing with a dev cert is more reliable.
$sig = Get-AuthenticodeSignature -FilePath $MsixPath -ErrorAction SilentlyContinue
$isSigned = $sig -and $sig.Status -eq 'Valid'

if (-not $isSigned) {
    Write-Host "[install] Package is unsigned — auto-signing with a self-signed dev certificate."

    # Locate signtool.exe from the Windows SDK.
    $signTool = $null
    $sdkRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
    if (Test-Path $sdkRoot) {
        $signTool = Get-ChildItem $sdkRoot -Directory | Sort-Object Name -Descending | ForEach-Object {
            $p = Join-Path $_.FullName "x64\signtool.exe"
            if (Test-Path $p) { $p }
        } | Select-Object -First 1
    }
    if (-not $signTool) {
        $signTool = (Get-Command signtool.exe -ErrorAction SilentlyContinue)?.Source
    }
    if (-not $signTool) {
        Write-Error @"
signtool.exe not found. Install the Windows SDK:
  winget install Microsoft.WindowsSDK.10.0.22621
Alternatively, build with: .\scripts\package-msix.ps1 -DevCert
"@
    }

    # Reuse or create a self-signed dev certificate matching the package publisher.
    $publisher = "CN=RemoteAgent Dev"
    $devCert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $publisher -and $_.NotAfter -gt (Get-Date) } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $devCert) {
        Write-Host "[install] Creating self-signed dev certificate for '$publisher'..."
        $devCert = New-SelfSignedCertificate `
            -Type Custom `
            -Subject $publisher `
            -KeyUsage DigitalSignature `
            -FriendlyName "Remote Agent Dev Certificate" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
        Write-Host "[install] Dev certificate created: $($devCert.Thumbprint)"
    } else {
        Write-Host "[install] Reusing existing dev certificate: $($devCert.Thumbprint)"
    }

    # Trust the dev cert so the signed package can be installed.
    $trustedRoot = Get-ChildItem Cert:\LocalMachine\Root |
        Where-Object { $_.Thumbprint -eq $devCert.Thumbprint } |
        Select-Object -First 1
    if (-not $trustedRoot) {
        Write-Host "[install] Trusting dev certificate in Cert:\LocalMachine\Root..."
        $certStore = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            [System.Security.Cryptography.X509Certificates.StoreName]::Root,
            [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
        $certStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $certStore.Add($devCert)
        $certStore.Close()
        Write-Host "[install] Certificate trusted."
    }

    # Export the cert alongside the MSIX for future reference.
    $cerPath = Join-Path (Split-Path $MsixPath) "remote-agent-dev.cer"
    Export-Certificate -Cert $devCert -FilePath $cerPath -Type CERT | Out-Null

    # Sign the MSIX in-place.
    Write-Host "[install] Signing $MsixPath ..."
    & $signTool sign /sha1 $devCert.Thumbprint /fd SHA256 /tr http://timestamp.digicert.com /td sha256 "$MsixPath"
    if ($LASTEXITCODE -ne 0) { Write-Error "[install] signtool failed (exit $LASTEXITCODE)." }
    Write-Host "[install] Package signed."
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
