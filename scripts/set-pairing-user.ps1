<#
.SYNOPSIS
    Adds or updates a pairing user in the RemoteAgent service appsettings.json.

.PARAMETER Username
    The pairing user's login name (case-insensitive match for upsert).

.PARAMETER Password
    The plaintext password. The SHA-256 hex digest is stored, never the plaintext.

.PARAMETER Replace
    When specified, replaces the entire PairingUsers array with only this user.
    Without this switch the entry is upserted (added if not present, updated if it exists).

.EXAMPLE
    .\set-pairing-user.ps1 -Username alice -Password s3cr3t
    .\set-pairing-user.ps1 -Username alice -Password newpassword -Replace
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Username,

    [Parameter(Mandatory = $true)]
    [string] $Password,

    [switch] $Replace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# 1. Locate appsettings.json
# ---------------------------------------------------------------------------
function Find-AppSettings {
    # Try to find the service executable path via SC query
    try {
        $scOutput = & sc.exe qc RemoteAgentService 2>$null
        if ($LASTEXITCODE -eq 0) {
            $binaryLine = $scOutput | Where-Object { $_ -match 'BINARY_PATH_NAME' }
            if ($binaryLine) {
                $exePath = ($binaryLine -replace '.*BINARY_PATH_NAME\s+:\s+', '').Trim()
                # Strip any CLI arguments (path ends at the first unquoted space after the exe)
                $exePath = $exePath -replace '^"([^"]+)".*', '$1'
                $exePath = $exePath -replace '^(\S+).*', '$1'
                $serviceDir = Split-Path -Parent $exePath
                $candidate = Join-Path $serviceDir 'appsettings.json'
                if (Test-Path $candidate) {
                    return $candidate
                }
            }
        }
    }
    catch {
        # SC query failed – ignore and fall through
    }

    # Default installed location
    $defaultPath = 'C:\Program Files\WindowsApps\RemoteAgent\service\appsettings.json'
    if (Test-Path $defaultPath) {
        return $defaultPath
    }

    # Fall back to repo source (development)
    $repoFallback = Join-Path $PSScriptRoot '..' 'src' 'RemoteAgent.Service' 'appsettings.json'
    $repoFallback = [System.IO.Path]::GetFullPath($repoFallback)
    if (Test-Path $repoFallback) {
        return $repoFallback
    }

    throw "Could not find appsettings.json. Searched:`n  $defaultPath`n  $repoFallback"
}

# ---------------------------------------------------------------------------
# 2. Compute SHA-256 hex of the password
# ---------------------------------------------------------------------------
$passwordBytes  = [System.Text.Encoding]::UTF8.GetBytes($Password)
$hashBytes      = [System.Security.Cryptography.SHA256]::HashData($passwordBytes)
$passwordHash   = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''

Write-Verbose "Password SHA-256: $passwordHash"

# ---------------------------------------------------------------------------
# 3. Find and read appsettings.json
# ---------------------------------------------------------------------------
$appSettingsPath = Find-AppSettings
Write-Host "Using appsettings.json: $appSettingsPath"

$json = Get-Content -Path $appSettingsPath -Raw -Encoding UTF8
$settings = $json | ConvertFrom-Json -AsHashtable

# ---------------------------------------------------------------------------
# 4. Upsert (or replace) the pairing user entry
# ---------------------------------------------------------------------------
if (-not $settings.ContainsKey('Agent')) {
    $settings['Agent'] = @{}
}

$agent = $settings['Agent']

if ($Replace) {
    # Replace entire array with only this user
    $agent['PairingUsers'] = @(
        [ordered]@{ Username = $Username; PasswordHash = $passwordHash }
    )
}
else {
    # Upsert: replace existing entry matched case-insensitively, or append
    $existingUsers = if ($agent.ContainsKey('PairingUsers') -and $agent['PairingUsers']) {
        @($agent['PairingUsers'])
    }
    else {
        @()
    }

    $replaced = $false
    $newUsers  = @()
    foreach ($user in $existingUsers) {
        $uName = if ($user -is [hashtable]) { $user['Username'] } else { $user.Username }
        if ([string]::Equals($uName, $Username, [System.StringComparison]::OrdinalIgnoreCase)) {
            $newUsers += [ordered]@{ Username = $Username; PasswordHash = $passwordHash }
            $replaced = $true
        }
        else {
            $newUsers += $user
        }
    }

    if (-not $replaced) {
        $newUsers += [ordered]@{ Username = $Username; PasswordHash = $passwordHash }
    }

    $agent['PairingUsers'] = $newUsers
}

$settings['Agent'] = $agent

# ---------------------------------------------------------------------------
# 5. Write appsettings.json back with indentation
# ---------------------------------------------------------------------------
$updatedJson = $settings | ConvertTo-Json -Depth 10
Set-Content -Path $appSettingsPath -Value $updatedJson -Encoding UTF8

Write-Host "SUCCESS: Pairing user '$Username' has been set in $appSettingsPath"

# ---------------------------------------------------------------------------
# 6. Restart RemoteAgentService (non-fatal if not found)
# ---------------------------------------------------------------------------
try {
    $svc = Get-Service -Name 'RemoteAgentService' -ErrorAction SilentlyContinue
    if ($svc) {
        Write-Host "Stopping RemoteAgentService..."
        Stop-Service -Name 'RemoteAgentService' -Force
        Write-Host "Starting RemoteAgentService..."
        Start-Service -Name 'RemoteAgentService'
        Write-Host "RemoteAgentService restarted."
    }
    else {
        Write-Verbose "RemoteAgentService not found – skipping service restart."
    }
}
catch {
    Write-Warning "Could not restart RemoteAgentService: $_"
}
