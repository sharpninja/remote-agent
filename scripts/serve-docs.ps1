# Build the DocFX site and serve it locally.
# Usage: .\scripts\serve-docs.ps1 [-Port 8880]
#   -Port  Optional. Default: 8880.

param([int]$Port = 8880)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$DocsDir = Join-Path $RepoRoot "docs"

# Ensure docfx is available (dotnet tool)
$toolsPath = Join-Path $env:USERPROFILE ".dotnet" "tools"
if ($env:PATH -notlike "*$toolsPath*") {
    $env:PATH = "$toolsPath;$env:PATH"
}
if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) {
    Write-Host "Installing docfx..."
    dotnet tool install -g docfx
    $env:PATH = "$toolsPath;$env:PATH"
}

Set-Location $DocsDir
Write-Host "Building DocFX site..."
docfx build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Optional: ensure index exists (CI workaround)
$siteIndex = Join-Path $DocsDir "_site" "index.html"
$readmeHtml = Join-Path $DocsDir "_site" "README.html"
if (-not (Test-Path $siteIndex) -and (Test-Path $readmeHtml)) {
    Copy-Item $readmeHtml $siteIndex
}

# Serve _site (avoid docfx serve - it can stack-overflow on Windows)
$siteDir = Join-Path $DocsDir "_site"
$url = "http://localhost:$Port"
Write-Host "Serving docs at $url (Ctrl+C to stop)"

# Prefer Python if available (no tool install); else use dotnet-serve
$py = Get-Command python -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $py) { $py = Get-Command py -ErrorAction SilentlyContinue | Select-Object -First 1 }

if ($py) {
    Write-Host "Using Python http.server."
    Start-Process $url
    Push-Location $siteDir
    try {
        & $py.Source -m http.server $Port
    } finally {
        Pop-Location
    }
    exit $LASTEXITCODE
}

# Use dotnet-serve
if (-not (dotnet tool list -g 2>$null | Select-String -Quiet "dotnet-serve")) {
    Write-Host "Installing dotnet-serve..."
    dotnet tool install -g dotnet-serve
}
$env:PATH = "$toolsPath;$env:PATH"

# Run: "dotnet serve" (canonical); or direct exe if present
$serveExe = Join-Path $toolsPath "dotnet-serve.exe"
if (Test-Path $serveExe) {
    & $serveExe -d $siteDir -p $Port -o
} else {
    & dotnet serve -d $siteDir -p $Port -o
}
exit $LASTEXITCODE
