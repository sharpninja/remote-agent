#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Opens Windows Firewall for the Remote Agent gRPC service port (5244).

.DESCRIPTION
    Adds an inbound Windows Firewall rule allowing TCP traffic on port 5244 so
    that phones and other LAN devices can reach the Remote Agent service running
    natively on Windows.

    Run once after installing the MSIX package. Safe to run again — it removes
    any existing rule with the same display name before adding a fresh one.

.PARAMETER Port
    Port number to expose. Defaults to 5244 (Windows service default).

.PARAMETER RuleName
    Display name for the firewall rule. Defaults to "Remote Agent gRPC".

.EXAMPLE
    .\scripts\expose-port-windows.ps1
    .\scripts\expose-port-windows.ps1 -Port 5244 -RuleName "Remote Agent gRPC"
#>
param(
    [int]   $Port     = 5244,
    [string]$RuleName = "Remote Agent gRPC"
)

$ErrorActionPreference = 'Stop'

Write-Host "Remote Agent — exposing port $Port on Windows Firewall" -ForegroundColor Cyan

# Remove any stale rule with the same name.
$existing = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "  Removing existing rule: $RuleName"
    Remove-NetFirewallRule -DisplayName $RuleName
}

# Add inbound allow rule on all profiles and interfaces.
New-NetFirewallRule `
    -DisplayName   $RuleName `
    -Description   "Allow inbound TCP on port $Port for the Remote Agent gRPC service." `
    -Direction     Inbound `
    -Protocol      TCP `
    -LocalPort     $Port `
    -Action        Allow `
    -Profile       Any `
    -InterfaceType Any | Out-Null

Write-Host "  Firewall rule added: $RuleName  (TCP $Port, all profiles)" -ForegroundColor Green

# Show the machine's LAN IP so the user knows what address to use on their device.
$wifiIP = (Get-NetIPAddress -AddressFamily IPv4 |
           Where-Object { $_.InterfaceAlias -like 'Wi-Fi*' -and $_.IPAddress -notlike '169.*' } |
           Select-Object -First 1).IPAddress
$ethIP  = (Get-NetIPAddress -AddressFamily IPv4 |
           Where-Object { $_.InterfaceAlias -like 'Ethernet*' -and $_.IPAddress -notlike '169.*' } |
           Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "Connect your device to:" -ForegroundColor Cyan
if ($wifiIP)  { Write-Host "  Wi-Fi   : http://$wifiIP`:$Port"   -ForegroundColor White }
if ($ethIP)   { Write-Host "  Ethernet: http://$ethIP`:$Port"    -ForegroundColor White }
if (-not $wifiIP -and -not $ethIP) {
    Write-Host "  (could not detect LAN IP — check ipconfig)" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "To remove the rule later:" -ForegroundColor DarkGray
Write-Host "  Remove-NetFirewallRule -DisplayName '$RuleName'" -ForegroundColor DarkGray
