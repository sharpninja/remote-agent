#Requires -Version 7.0
<#
.SYNOPSIS
  Monitor the Remote Agent Windows Service — Event Log errors and optional file log tail.

.DESCRIPTION
  Watches the Windows Application Event Log for entries from the Remote Agent Service
  source and streams new errors/warnings to the console. Optionally also tails the
  structured file log written to Agent.LogDirectory.

  Event IDs written by the service:
    1000  — service started successfully  (Information)
    1001  — startup or runtime fatal error (Error)
    1002  — unhandled background-thread exception (Error)

  Run without -Tail to print recent history only, then exit.
  Run with -Tail to stream new events continuously until Ctrl+C.

.PARAMETER Tail
  Stream new events continuously (poll every -IntervalSeconds). Default: $false.

.PARAMETER IntervalSeconds
  Polling interval when -Tail is active. Default: 5.

.PARAMETER Hours
  How many hours of history to show on startup. Default: 24.

.PARAMETER Level
  Minimum severity to display: Error | Warning | Information. Default: Warning.

.PARAMETER LogFile
  Path to the service file log (Agent.LogDirectory\service.log).
  When set, the file log is tailed in parallel with the Event Log.
  If omitted the script auto-detects the path from the service registry.

.PARAMETER ServiceName
  Windows Service name. Default: "Remote Agent Service".

.EXAMPLE
  .\scripts\monitor-service-windows.ps1
  .\scripts\monitor-service-windows.ps1 -Tail
  .\scripts\monitor-service-windows.ps1 -Tail -Level Error -Hours 1
  .\scripts\monitor-service-windows.ps1 -Tail -LogFile "C:\ProgramData\RemoteAgent\logs\service.log"
#>
[CmdletBinding()]
param(
    [switch]   $Tail,
    [int]      $IntervalSeconds = 5,
    [int]      $Hours           = 24,
    [ValidateSet('Error','Warning','Information')]
    [string]   $Level           = 'Warning',
    [string]   $LogFile         = '',
    [string]   $ServiceName     = 'Remote Agent Service'
)

$ErrorActionPreference = 'Stop'

# ── Severity filter ───────────────────────────────────────────────────────────
# Get-WinEvent Level values: 1=Critical, 2=Error, 3=Warning, 4=Information, 5=Verbose
$levelOrder = @{ Information = 0; Warning = 1; Error = 2 }
$minLevel   = $levelOrder[$Level]

# Map WinEvent Level numbers to display names and severity order.
$winEventLevels = @{
    1 = @{ Name = 'Critical';    Order = 2 }
    2 = @{ Name = 'Error';       Order = 2 }
    3 = @{ Name = 'Warning';     Order = 1 }
    4 = @{ Name = 'Information'; Order = 0 }
    5 = @{ Name = 'Verbose';     Order = 0 }
}

function Get-EntryOrder([int]$level) {
    $info = $winEventLevels[$level]
    if ($null -eq $info) { return 0 }
    return $info.Order
}

function Get-EntryName([int]$level) {
    $info = $winEventLevels[$level]
    if ($null -eq $info) { return 'Unknown' }
    return $info.Name
}

function Test-SeverityIncluded([int]$level) {
    return (Get-EntryOrder $level) -ge $minLevel
}

# ── Colour map ────────────────────────────────────────────────────────────────
$colours = @{
    Critical    = 'Magenta'
    Error       = 'Red'
    Warning     = 'Yellow'
    Information = 'Cyan'
    Verbose     = 'DarkGray'
}

function Write-EventEntry($entry) {
    $name   = Get-EntryName $entry.Level
    $colour = $colours[$name] ?? 'Gray'
    $ts     = $entry.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss')
    Write-Host "[$ts] [$name] EventId=$($entry.Id)" -ForegroundColor $colour
    Write-Host $entry.Message                         -ForegroundColor $colour
    Write-Host ''
}

# ── Service status ────────────────────────────────────────────────────────────
Write-Host "── Remote Agent Service Monitor ────────────────────────────────" -ForegroundColor Cyan

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $svc) {
    # Try a partial-name search in case the service was registered under a slightly different name.
    $svc = Get-Service -ErrorAction SilentlyContinue |
           Where-Object { $_.DisplayName -like "*Remote Agent*" -or $_.Name -like "*RemoteAgent*" } |
           Select-Object -First 1
    if ($svc) {
        Write-Host "Note: service '$ServiceName' not found; using '$($svc.Name)' instead." -ForegroundColor DarkYellow
        $ServiceName = $svc.Name
    }
}

if ($null -eq $svc) {
    Write-Warning "Windows service '$ServiceName' is not installed."
} else {
    $statusColour = if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' }
    Write-Host "Service status : " -NoNewline
    Write-Host $svc.Status -ForegroundColor $statusColour
    Write-Host "Display name   : $($svc.DisplayName)"
}
Write-Host ''

# ── Auto-detect log file from service image path ──────────────────────────────
if (-not $LogFile -and $svc) {
    try {
        $imagePath = (Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName" `
                          -Name ImagePath -ErrorAction SilentlyContinue).ImagePath -replace '"',''
        if ($imagePath) {
            $installDir = Split-Path $imagePath -Parent
            # Service defaults: Agent.LogDirectory resolves to %ProgramData%\RemoteAgent\logs or
            # the directory next to the binary. Check both common locations.
            $candidates = @(
                "$env:ProgramData\RemoteAgent\logs\service.log",
                (Join-Path $installDir 'logs\service.log'),
                (Join-Path $installDir 'service.log')
            )
            foreach ($c in $candidates) {
                if (Test-Path $c) { $LogFile = $c; break }
            }
        }
    } catch { }
}

if ($LogFile) {
    Write-Host "File log       : $LogFile" -ForegroundColor DarkCyan
} else {
    Write-Host "File log       : (not found — Event Log only)" -ForegroundColor DarkGray
}
Write-Host ''

# ── Shared helper: query Application Event Log via Get-WinEvent ───────────────
function Get-ServiceEvents([datetime]$after, [datetime]$before = [datetime]::MaxValue) {
    $filter = @{
        LogName      = 'Application'
        ProviderName = 'Remote Agent Service'
        StartTime    = $after
    }
    if ($before -ne [datetime]::MaxValue) { $filter['EndTime'] = $before }
    try {
        $events = Get-WinEvent -FilterHashtable $filter -ErrorAction Stop |
                  Where-Object { Test-SeverityIncluded $_.Level } |
                  Sort-Object TimeCreated
        return $events
    } catch [System.Exception] {
        if ($_.Exception.Message -like '*No events were found*' -or
            $_.Exception.HResult -eq -2147024809) {
            return @()   # no matching events — normal condition
        }
        throw
    }
}

# ── Event Log: recent history ─────────────────────────────────────────────────
$since      = (Get-Date).AddHours(-$Hours)
$eventLog   = 'Application'
$source     = $ServiceName

Write-Host "── Event Log history (last $Hours h, level >= $Level) ──────────────" -ForegroundColor Cyan

try {
    $entries = Get-ServiceEvents -after $since
    if ($entries.Count -eq 0) {
        Write-Host '  (no matching events)' -ForegroundColor DarkGray
    } else {
        foreach ($e in $entries) { Write-EventEntry $e }
    }
} catch {
    Write-Warning "Could not read Event Log: $_"
}

Write-Host ''

if (-not $Tail) {
    Write-Host "Tip: run with -Tail to stream new events continuously." -ForegroundColor DarkGray
    exit 0
}

# ── Continuous tail ───────────────────────────────────────────────────────────
Write-Host "── Streaming new events (Ctrl+C to stop) ───────────────────────" -ForegroundColor Cyan
Write-Host ''

# Track the timestamp of the last event already shown.
$lastEventTime = [datetime]::UtcNow

# Track file log position.
$fileLogStream   = $null
$fileLogReader   = $null
if ($LogFile -and (Test-Path $LogFile)) {
    $fileLogStream = [System.IO.File]::Open($LogFile,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::ReadWrite)
    $fileLogStream.Seek(0, [System.IO.SeekOrigin]::End) | Out-Null
    $fileLogReader = [System.IO.StreamReader]::new($fileLogStream, [System.Text.Encoding]::UTF8, $true)
}

# Error keywords to highlight in the file log.
$errorPattern = [regex]'(?i)fail|error|exception|crit|unhandled|warn'

try {
    while ($true) {
        # ── Poll Event Log ────────────────────────────────────────────────────
        try {
            $newEntries = Get-ServiceEvents -after $lastEventTime
            foreach ($e in $newEntries) {
                Write-EventEntry $e
                if ($e.TimeCreated -gt $lastEventTime) {
                    $lastEventTime = $e.TimeCreated
                }
            }
        } catch { }

        # ── Poll file log ─────────────────────────────────────────────────────
        if ($fileLogReader) {
            while (-not $fileLogReader.EndOfStream) {
                $line = $fileLogReader.ReadLine()
                if ($null -eq $line) { break }
                if ($errorPattern.IsMatch($line)) {
                    $lineColour = if ($line -match '(?i)fail|error|exception|crit|unhandled') { 'Red' }
                                  elseif ($line -match '(?i)warn') { 'Yellow' }
                                  else { 'Gray' }
                    Write-Host "[filelog] $line" -ForegroundColor $lineColour
                }
            }
        }

        # ── Check service is still running ────────────────────────────────────
        $svcNow = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($svcNow -and $svcNow.Status -ne 'Running') {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] WARNING: service status changed to $($svcNow.Status)" `
                -ForegroundColor Red
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
} finally {
    $fileLogReader?.Dispose()
    $fileLogStream?.Dispose()
}
