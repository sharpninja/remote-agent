#Requires -Version 7.0
<#
.SYNOPSIS
  Run the Remote Agent service locally on Windows (no Docker).
.DESCRIPTION
  Stops the Docker container or kills a local process using port 5243 if necessary.
  Uses the Development launch profile (http://0.0.0.0:5243).
  Override via env: Agent__Command, Agent__LogDirectory, Agent__DataDirectory, etc.
.PARAMETER Port
  Port to use. Default: 5243.
.PARAMETER LocalRepoBase
  When the repo is on a WSL path, create a symbolic link at this path\remote-agent pointing at the repo. Default: E:\github. Others can set e.g. D:\repos.
.EXAMPLE
  .\scripts\run-service-local.ps1
  .\scripts\run-service-local.ps1 -Port 5243
  .\scripts\run-service-local.ps1 -LocalRepoBase D:\repos
#>
[CmdletBinding()]
param(
    [int] $Port = 5243,
    [string] $LocalRepoBase = "E:\github"
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$ContainerName = if ($env:CONTAINER_NAME) { $env:CONTAINER_NAME } else { "remote-agent-service" }

# When repo is on a WSL path (UNC), ensure a symbolic link exists at $LocalRepoBase\remote-agent and use it for the rest of the script.
# Derive the native WSL path for proto generation by parsing the UNC (e.g. \\wsl.localhost\Ubuntu\home\user\repo -> /home/user/repo).
$wslPathForProto = $null
$wslDistroForProto = $null
if ($RepoRoot -match '^\\\\wsl(?:\.localhost|\$)\\([^\\]+)\\(.+)$') {
    $wslDistroForProto = $Matches[1]
    $wslPathForProto = '/' + ($Matches[2] -replace '\\', '/')
    $symlinkPath = Join-Path $LocalRepoBase "remote-agent"
    if (-not (Test-Path $symlinkPath)) {
        $parent = Split-Path $symlinkPath -Parent
        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        try {
            New-Item -ItemType SymbolicLink -Path $symlinkPath -Target $RepoRoot -ErrorAction Stop | Out-Null
            Write-Host "Created symbolic link: $symlinkPath -> $RepoRoot"
        } catch {
            Write-Warning "Could not create symbolic link at $symlinkPath (may require elevated prompt): $_"
        }
    }
    if (Test-Path $symlinkPath) {
        $RepoRoot = $symlinkPath
    }
}

# Free port: stop Docker container if running, then kill any local process on the port
if (Get-Command docker -ErrorAction SilentlyContinue) {
    $running = docker ps -q -f "name=^${ContainerName}$" 2>$null
    if ($running) {
        Write-Host "Stopping Docker container: $ContainerName"
        docker stop $ContainerName 2>$null
        Start-Sleep -Seconds 1
    }
}

$pids = @()
try {
    $conn = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($conn) {
        $pids = $conn | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique
    }
} catch {
    # Fallback: netstat -ano
    $lines = netstat -ano | Select-String ":\s*${Port}\s"
    foreach ($line in $lines) {
        if ($line -match '\s+(\d+)\s*$') {
            $pids += [int]$Matches[1]
        }
    }
    $pids = $pids | Sort-Object -Unique
}

if ($pids.Count -gt 0) {
    Write-Host "Stopping process(es) on port $Port`: $($pids -join ', ')"
    foreach ($processId in $pids) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 1
}

# Agent command: use env if set; otherwise default (service uses copilot on Windows when unset)
if (-not $env:Agent__Command) {
    $agentExe = Get-Command agent -ErrorAction SilentlyContinue
    if ($agentExe) {
        $env:Agent__Command = $agentExe.Source
    }
    # else service will use its default (copilot on Windows)
}
$env:Agent__LogDirectory = if ($env:Agent__LogDirectory) { $env:Agent__LogDirectory } else { Join-Path $RepoRoot "logs" }
$env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Development" }

$logDir = $env:Agent__LogDirectory
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

$cmdDisplay = if ($env:Agent__Command) { $env:Agent__Command } else { "default" }
Set-Location $RepoRoot

# Run gRPC C# code generation in WSL. Use the native WSL path only (no translation from symlink/Windows path).
$wslPath = $null
$wslDistro = $null
if ($wslPathForProto) {
    $wslPath = $wslPathForProto
    $wslDistro = $wslDistroForProto
} else {
    # When run from the symlink (e.g. E:\github\remote-agent), resolve its target; it's a UNC path — parse out the WSL path.
    try {
        $item = Get-Item -LiteralPath $RepoRoot -ErrorAction Stop
        if ($item.LinkType -eq 'SymbolicLink') {
            $target = $null
            if ($null -ne $item.Target) {
                $target = $item.Target
                if ($target -is [array]) { $target = $target[0] }
            }
            if ($null -eq $target -and $null -ne $item.TargetInfo -and $null -ne $item.TargetInfo.Target) {
                $target = $item.TargetInfo.Target[0]
            }
            if ($null -eq $target -and [System.IO.Directory]::Exists($RepoRoot)) {
                try {
                    $linkTarget = [System.IO.File]::GetLinkTarget($RepoRoot)
                    if ($null -ne $linkTarget) { $target = $linkTarget.FullName }
                } catch { }
            }
            if ($null -ne $target -and $target -match '^\\\\wsl(?:\.localhost|\$)\\([^\\]+)\\(.+)$') {
                $wslDistro = $Matches[1]
                $wslPath = '/' + ($Matches[2] -replace '\\', '/')
            }
        }
    } catch { }
    if (-not $wslPath) {
        try {
            $wslPath = (wsl wslpath -u ($RepoRoot -replace '\\', '/') 2>$null).Trim()
        } catch {
            $wslPath = $null
        }
    }
}
if ($wslPath -and (Get-Command wsl -ErrorAction SilentlyContinue)) {
    Write-Host "=== Generating gRPC C# code (WSL) ==="
    if ($wslDistro) {
        & wsl -d $wslDistro -e bash -c "cd '$wslPath' && ./scripts/generate-proto.sh"
    } else {
        & wsl -e bash -c "cd '$wslPath' && ./scripts/generate-proto.sh"
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$serviceTestProjectPath = Join-Path $RepoRoot (Join-Path "tests" (Join-Path "RemoteAgent.Service.Tests" "RemoteAgent.Service.Tests.csproj"))
$serviceProjectPath = Join-Path $RepoRoot (Join-Path "src" (Join-Path "RemoteAgent.Service" "RemoteAgent.Service.csproj"))

# Write-Host "=== Building service and service tests ==="
# dotnet build $serviceTestProjectPath -nologo
# if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
# Write-Host "=== Running service tests ==="
# dotnet test $serviceTestProjectPath -nologo --no-build
# if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "=== Starting service at http://0.0.0.0:${Port} (Agent__Command=$cmdDisplay) ==="
dotnet run --project $serviceProjectPath
