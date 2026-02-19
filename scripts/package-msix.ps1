#Requires -Version 7.0
<#
.SYNOPSIS
  Build a combined MSIX package for the Remote Agent service and desktop management app.

.DESCRIPTION
  Publishes the service (net10.0, win-x64) and desktop app (net9.0, win-x64), assembles
  them into a single MSIX package, and optionally signs it.

  The resulting package installs both:
    - RemoteAgent.Service.exe, registered as a Windows service (RemoteAgentService) that
      starts automatically under LocalSystem via the MSIX windows.service extension.
    - RemoteAgent.Desktop.exe, visible in the Start menu as "Remote Agent Desktop".

  The Ollama plugin DLL is included in the service sub-directory when it has been built.

  Install layout inside the package:
    service\          service binaries + appsettings.json
    service\plugins\  Ollama plugin DLL (if built)
    desktop\          desktop app binaries
    Assets\           MSIX icon assets

  Requirements:
    - Windows SDK >= 10.0.19041  (makeappx.exe, signtool.exe)
    - .NET SDK 10.x (service + plugin) and 9.x (desktop; via src/RemoteAgent.Desktop/global.json)

  Signing:
    -DevCert        Create a temporary self-signed certificate (development / CI testing).
    -CertThumbprint Use an existing certificate from the current user's My store.
    Neither flag    Build an unsigned package (can be installed with Add-AppxPackage -AllowUnsigned).

  After building, install on the same machine:
    Add-AppxPackage -Path .\artifacts\remote-agent_<ver>_x64.msix

  To trust a self-signed dev cert on the target machine before installing:
    $cert = (Get-PfxCertificate -FilePath artifacts\remote-agent-dev.cer)
    Import-Certificate -Certificate $cert -CertStoreLocation Cert:\LocalMachine\Root

.PARAMETER Configuration
  Build configuration: Release (default) or Debug.

.PARAMETER Version
  Package version (e.g. 1.2.3). Defaults to GitVersion SemVer, then most recent git tag, then next-version from GitVersion.yml.

.PARAMETER Publisher
  MSIX Identity Publisher string, must match the signing certificate Subject exactly.
  Default: "CN=RemoteAgent Dev".

.PARAMETER CertThumbprint
  SHA1 thumbprint of an existing certificate in Cert:\CurrentUser\My to sign with.
  Mutually exclusive with -DevCert.

.PARAMETER DevCert
  Create (or reuse) a self-signed certificate and sign the package with it.
  The public certificate is exported to <OutDir>\remote-agent-dev.cer.

.PARAMETER SelfContained
  Publish self-contained, single-file packages (bundles .NET runtime; no runtime prereq on target).
  Default: $true. Pass -SelfContained $false for a framework-dependent build.

.PARAMETER Clean
  Run 'dotnet clean' on each project before publishing.

.PARAMETER ServiceOnly
  Build and bundle only the service component; omit the desktop app.

.PARAMETER DesktopOnly
  Build and bundle only the desktop component; omit the service and Windows service extension.

.PARAMETER OutDir
  Output directory for the .msix file. Default: <repo-root>\artifacts\.

.EXAMPLE
  .\scripts\package-msix.ps1 -DevCert
  .\scripts\package-msix.ps1 -Configuration Release -DevCert -SelfContained
  .\scripts\package-msix.ps1 -CertThumbprint "ABC123DEF456..." -Version 2.1.0
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Version = "",

    [string] $Publisher = "CN=RemoteAgent Dev",

    [string] $CertThumbprint = "",

    [switch] $DevCert,

    [bool] $SelfContained = $true,

    [switch] $ServiceOnly,

    [switch] $DesktopOnly,

    [switch] $Clean,

    [switch] $Force,

    [string] $OutDir = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot   = (Get-Item $PSScriptRoot).Parent.FullName
$NuGetConfig = Join-Path $RepoRoot "NuGet.Config"
if (-not $OutDir) { $OutDir = Join-Path $RepoRoot "artifacts" }

# ── Validate mutually exclusive flags ────────────────────────────────────────
if ($ServiceOnly -and $DesktopOnly) {
    Write-Error "-ServiceOnly and -DesktopOnly are mutually exclusive."
}
if ($DevCert -and $CertThumbprint) {
    Write-Error "-DevCert and -CertThumbprint are mutually exclusive."
}

$BuildService = -not $DesktopOnly
$BuildDesktop = -not $ServiceOnly

# ── Version detection ─────────────────────────────────────────────────────────
if (-not $Version) {
    # Prefer GitVersion (dotnet tool) for accurate semver from branch/tag history.
    try {
        $gvJson = dotnet tool run dotnet-gitversion -- /output json 2>$null | ConvertFrom-Json
        if ($gvJson -and $gvJson.SemVer) {
            $Version = $gvJson.SemVer
            Write-Host "[package-msix] version source: dotnet-gitversion -> $Version"
        }
    } catch { }

    if (-not $Version) {
        # Fallback: read next-version from GitVersion.yml (authoritative for this repo).
        $gvYml = Join-Path $RepoRoot "GitVersion.yml"
        if (Test-Path $gvYml) {
            $m = (Get-Content $gvYml | Select-String '^\s*next-version:\s*(.+)').Matches
            if ($m.Count -gt 0) {
                $Version = $m[0].Groups[1].Value.Trim()
                Write-Host "[package-msix] version source: GitVersion.yml next-version -> $Version"
            }
        }
    }

    if (-not $Version) {
        # Last resort: most recent git tag (may be unreliable if tags have pre-release suffixes).
        $tag = git -C $RepoRoot describe --tags --abbrev=0 2>$null
        if ($tag -match '^v?(\d+\.\d+\.\d+)') {
            $Version = $Matches[1]
            Write-Host "[package-msix] version source: git tag ($tag) -> $Version"
        } else {
            $Version = "0.1.0"
            Write-Host "[package-msix] version source: hardcoded fallback -> $Version"
        }
    }
} else {
    Write-Host "[package-msix] version source: -Version parameter -> $Version"
}

# MSIX Identity Version must be 4-part (major.minor.patch.revision).
# Strip pre-release suffix (e.g. 1.0.0-develop.1 -> 1.0.0) then pad to 4 parts.
function ConvertTo-MsixVersion([string]$semver) {
    $base  = ($semver -split '-')[0]
    $parts = $base -split '\.'
    while ($parts.Count -lt 4) { $parts += "0" }
    ($parts[0..3] -join '.')
}
$Version4 = ConvertTo-MsixVersion $Version

# ── Architecture / RID ────────────────────────────────────────────────────────
# MSIX currently supports x64, x86, and arm64 for full-trust desktop apps.
$Rid          = "win-x64"
$MsixArch     = "x64"
$PackageName  = "remote-agent"
$MsixFile     = Join-Path $OutDir "${PackageName}_${Version}_${MsixArch}.msix"

Write-Host "[package-msix] version=$Version ($Version4)  rid=$Rid  config=$Configuration  self-contained=$SelfContained"
Write-Host "[package-msix] service=$BuildService  desktop=$BuildDesktop  out=$OutDir"

# ── Locate Windows SDK tools ─────────────────────────────────────────────────
function Find-WinSdkTool([string]$Name) {
    $sdkRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
    if (Test-Path $sdkRoot) {
        $found = Get-ChildItem $sdkRoot -Directory | Sort-Object Name -Descending | ForEach-Object {
            $p = Join-Path $_.FullName "x64\$Name"
            if (Test-Path $p) { $p }
        } | Select-Object -First 1
        if ($found) { return $found }
    }
    $inPath = Get-Command $Name -ErrorAction SilentlyContinue
    if ($inPath) { return $inPath.Source }
    return $null
}

$MakeAppx = Find-WinSdkTool "makeappx.exe"
$SignTool  = Find-WinSdkTool "signtool.exe"

if (-not $MakeAppx) {
    Write-Error @"
makeappx.exe not found. Install the Windows SDK:
  winget install Microsoft.WindowsSDK.10.0.22621
  -- or --
  https://developer.microsoft.com/windows/downloads/windows-sdk/
"@
}

Write-Host "[package-msix] makeappx : $MakeAppx"
if ($SignTool) { Write-Host "[package-msix] signtool : $SignTool" }

# ── Ensure output directory ───────────────────────────────────────────────────
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# ── Clean ─────────────────────────────────────────────────────────────────────
if ($Clean) {
    $projectsToClean = @()
    if ($BuildService) {
        $projectsToClean += Join-Path $RepoRoot "src\RemoteAgent.Service"
        $ollamaDir = Join-Path $RepoRoot "src\RemoteAgent.Plugins.Ollama"
        if (Test-Path $ollamaDir) { $projectsToClean += $ollamaDir }
    }
    if ($BuildDesktop) {
        $projectsToClean += Join-Path $RepoRoot "src\RemoteAgent.Desktop"
    }
    foreach ($projDir in $projectsToClean) {
        foreach ($target in @("bin", "obj")) {
            $path = Join-Path $projDir $target
            if (Test-Path $path) {
                Write-Host "[package-msix] removing $path ..."
                Remove-Item $path -Recurse -Force
            }
        }
    }
}

# ── Publish ───────────────────────────────────────────────────────────────────
$scFlag = if ($SelfContained) { "--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true" } else { "" }

if ($BuildService) {
    $ServicePub = Join-Path $OutDir "publish-service"
    Write-Host "[package-msix] publishing service -> $ServicePub"
    $cmd = "dotnet publish `"$(Join-Path $RepoRoot 'src\RemoteAgent.Service\RemoteAgent.Service.csproj')`" " +
           "-c $Configuration -r $Rid -f net10.0 --configfile `"$NuGetConfig`" " +
           "-o `"$ServicePub`" $scFlag"
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Publish Ollama plugin (always framework-dependent; it loads into the service process).
    $OllamaProj = Join-Path $RepoRoot "src\RemoteAgent.Plugins.Ollama\RemoteAgent.Plugins.Ollama.csproj"
    if (Test-Path $OllamaProj) {
        $PluginPub = Join-Path $OutDir "publish-plugin"
        Write-Host "[package-msix] publishing Ollama plugin -> $PluginPub"
        $cmd = "dotnet publish `"$OllamaProj`" " +
               "-c $Configuration -r $Rid -f net10.0 --configfile `"$NuGetConfig`" " +
               "--self-contained false -o `"$PluginPub`""
        Invoke-Expression $cmd
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

if ($BuildDesktop) {
    $DesktopPub = Join-Path $OutDir "publish-desktop"
    Write-Host "[package-msix] publishing desktop -> $DesktopPub"
    $cmd = "dotnet publish `"$(Join-Path $RepoRoot 'src\RemoteAgent.Desktop\RemoteAgent.Desktop.csproj')`" " +
           "-c $Configuration -r $Rid -f net9.0 --configfile `"$NuGetConfig`" " +
           "-o `"$DesktopPub`" $scFlag"
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# ── Generate MSIX icon assets ─────────────────────────────────────────────────
# Generates solid-color placeholder PNGs using System.Drawing. Replace with
# real artwork before publishing to the Microsoft Store or any production channel.
function New-IconPng([string]$Path, [int]$W, [int]$H) {
    Add-Type -AssemblyName System.Drawing
    $bmp = [System.Drawing.Bitmap]::new($W, $H)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(0x1E, 0x88, 0xE5))   # Material Blue 600
    try {
        $fontSize = [int]([Math]::Max(8, $H * 0.35))
        $font     = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold)
        $sf       = [System.Drawing.StringFormat]::new()
        $sf.Alignment     = [System.Drawing.StringAlignment]::Center
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rect = [System.Drawing.RectangleF]::new(0, 0, $W, $H)
        $g.DrawString("RA", $font, [System.Drawing.Brushes]::White, $rect, $sf)
        $font.Dispose(); $sf.Dispose()
    } catch { }   # text rendering is best-effort
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# ── Assemble MSIX package layout ──────────────────────────────────────────────
$PkgRoot   = Join-Path $OutDir "msix-layout"
$AssetsDir = Join-Path $PkgRoot "Assets"

# Clean any previous layout.
if (Test-Path $PkgRoot) { Remove-Item $PkgRoot -Recurse -Force }
New-Item -ItemType Directory -Path $AssetsDir -Force | Out-Null

# Icons required by the MSIX manifest.
$iconSpecs = @(
    @{ Name = "Square44x44Logo.png";   W = 44;  H = 44  },
    @{ Name = "Square150x150Logo.png"; W = 150; H = 150 },
    @{ Name = "Wide310x150Logo.png";   W = 310; H = 150 },
    @{ Name = "Square310x310Logo.png"; W = 310; H = 310 },
    @{ Name = "StoreLogo.png";         W = 50;  H = 50  }
)
foreach ($spec in $iconSpecs) {
    $iconPath = Join-Path $AssetsDir $spec.Name

    # Try to rasterise the repo SVG with Inkscape or ImageMagick first.
    $svgSrc = Join-Path $RepoRoot "src\RemoteAgent.Desktop\Assets\AppIcon\appicon.svg"
    $rendered = $false
    if (Test-Path $svgSrc) {
        $inkscape = Get-Command inkscape -ErrorAction SilentlyContinue
        $magick   = Get-Command magick   -ErrorAction SilentlyContinue
        if ($inkscape) {
            & inkscape --export-filename="$iconPath" --export-width=$spec.W --export-height=$spec.H "$svgSrc" 2>$null
            $rendered = $LASTEXITCODE -eq 0
        } elseif ($magick) {
            & magick -background none -resize "$($spec.W)x$($spec.H)" "$svgSrc" "$iconPath" 2>$null
            $rendered = $LASTEXITCODE -eq 0
        }
    }

    if (-not $rendered) { New-IconPng -Path $iconPath -W $spec.W -H $spec.H }
}
Write-Host "[package-msix] icon assets written to $AssetsDir"

# Service binaries.
if ($BuildService) {
    $ServiceDest = Join-Path $PkgRoot "service"
    New-Item -ItemType Directory -Path $ServiceDest -Force | Out-Null
    Copy-Item -Path (Join-Path $ServicePub "*") -Destination $ServiceDest -Recurse -Force

    # Include default appsettings so the service has sensible defaults on first run.
    $appSettings = Join-Path $RepoRoot "src\RemoteAgent.Service\appsettings.json"
    if (Test-Path $appSettings) {
        Copy-Item -Path $appSettings -Destination $ServiceDest -Force
    }

    # Ollama plugin.
    if (Test-Path (Join-Path $OutDir "publish-plugin")) {
        $PluginDest = Join-Path $ServiceDest "plugins"
        New-Item -ItemType Directory -Path $PluginDest -Force | Out-Null
        Get-ChildItem (Join-Path $OutDir "publish-plugin") -Filter "*.dll" |
            Copy-Item -Destination $PluginDest -Force
    }
}

# Desktop binaries.
if ($BuildDesktop) {
    $DesktopDest = Join-Path $PkgRoot "desktop"
    New-Item -ItemType Directory -Path $DesktopDest -Force | Out-Null
    Copy-Item -Path (Join-Path $DesktopPub "*") -Destination $DesktopDest -Recurse -Force
}

# ── Write AppxManifest.xml ────────────────────────────────────────────────────
# The manifest always has one Application entry (desktop app, or a headless stub
# when -ServiceOnly is set). The windows.service Extension is added when the
# service component is included.
$desktopExe = "desktop\RemoteAgent.Desktop.exe"
$serviceExe = "service\RemoteAgent.Service.exe"

# Application element: use desktop exe if available, else service exe as stub.
$appExe         = if ($BuildDesktop) { $desktopExe } else { $serviceExe }
$appDisplayName = if ($BuildDesktop) { "Remote Agent Desktop" } else { "Remote Agent Service" }
$appDescription = if ($BuildDesktop) {
    "Remote Agent desktop management application"
} else {
    "Remote Agent gRPC service host"
}

$serviceExtensionXml = ""
if ($BuildService) {
    $serviceExtensionXml = @"

      <Extensions>
        <!-- Registers RemoteAgent.Service.exe as a Windows service that starts
             automatically under LocalSystem when the MSIX package is installed.
             The service is stopped and unregistered automatically on uninstall. -->
        <desktop6:Extension Category="windows.service"
                             Executable="$serviceExe"
                             EntryPoint="Windows.FullTrustApplication">
          <desktop6:Service Name="RemoteAgentService"
                             StartupType="auto"
                             StartAccount="localSystem" />
        </desktop6:Extension>
      </Extensions>
"@
}

$rescapNs   = 'xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"'
$desktop6Ns = if ($BuildService) { 'xmlns:desktop6="http://schemas.microsoft.com/appx/manifest/desktop/windows10/6"' } else { "" }
$ignorable  = if ($BuildService) { 'IgnorableNamespaces="rescap desktop6"' } else { 'IgnorableNamespaces="rescap"' }

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  $rescapNs
  $desktop6Ns
  $ignorable>

  <Identity
    Name="RemoteAgent"
    Publisher="$Publisher"
    Version="$Version4"
    ProcessorArchitecture="$MsixArch" />

  <Properties>
    <DisplayName>Remote Agent</DisplayName>
    <PublisherDisplayName>Remote Agent</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <!-- Minimum: Windows 10 2004 (build 19041) for windows.service MSIX extension support. -->
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Capabilities>
    <!-- runFullTrust: required for Win32 full-trust apps packaged with MSIX.     -->
    <!-- allowElevation: lets the service run as LocalSystem without being blocked -->
    <!--                 by the MSIX AppContainer restrictions.                    -->
    <rescap:Capability Name="runFullTrust" />
    <rescap:Capability Name="allowElevation" />
    <rescap:Capability Name="packagedServices" />
    <rescap:Capability Name="localSystemServices" />
  </Capabilities>

  <Applications>
    <Application Id="RemoteAgentApp"
                 Executable="$appExe"
                 EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="$appDisplayName"
        Description="$appDescription"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png"
                          Square310x310Logo="Assets\Square310x310Logo.png" />
      </uap:VisualElements>
$serviceExtensionXml
    </Application>
  </Applications>

</Package>
"@

$manifestPath = Join-Path $PkgRoot "AppxManifest.xml"
$manifest | Set-Content -Path $manifestPath -Encoding UTF8
Write-Host "[package-msix] wrote AppxManifest.xml"

# ── Version / manifest review ─────────────────────────────────────────────────
Write-Host ""
Write-Host "── Version applied ─────────────────────────────────────────────────────"
Write-Host "  SemVer        : $Version"
Write-Host "  MSIX Identity : $Version4"
Write-Host ""
Write-Host "── AppxManifest.xml ────────────────────────────────────────────────────"
Get-Content $manifestPath | ForEach-Object { Write-Host "  $_" }
Write-Host "────────────────────────────────────────────────────────────────────────"
Write-Host ""

if (-not $Force) {
    Write-Host "Press Enter to continue packaging, or Ctrl+C to abort ..."
    $null = Read-Host
}

# ── Run makeappx ──────────────────────────────────────────────────────────────
Write-Host "[package-msix] packing $MsixFile ..."
$makeappxProc = Start-Process -FilePath $MakeAppx `
    -ArgumentList "pack", "/d", "`"$PkgRoot`"", "/p", "`"$MsixFile`"", "/o", "/nv" `
    -NoNewWindow -PassThru -Wait
if ($makeappxProc.ExitCode -ne 0) { exit $makeappxProc.ExitCode }
Write-Host "[package-msix] packed: $MsixFile"

# ── Sign ──────────────────────────────────────────────────────────────────────
$signThumb = $CertThumbprint

if ($DevCert -and -not $signThumb) {
    if (-not $SignTool) {
        Write-Warning "signtool.exe not found — skipping signing. Install Windows SDK to enable signing."
    } else {
        # Reuse existing dev cert if one with the same subject already exists.
        $existing = Get-ChildItem Cert:\CurrentUser\My |
            Where-Object { $_.Subject -eq $Publisher -and $_.NotAfter -gt (Get-Date) } |
            Sort-Object NotAfter -Descending |
            Select-Object -First 1

        if ($existing) {
            Write-Host "[package-msix] reusing existing dev cert: $($existing.Thumbprint)"
            $signThumb = $existing.Thumbprint
        } else {
            Write-Host "[package-msix] creating self-signed dev certificate for '$Publisher'..."
            $cert = New-SelfSignedCertificate `
                -Type Custom `
                -Subject $Publisher `
                -KeyUsage DigitalSignature `
                -FriendlyName "Remote Agent Dev Certificate" `
                -CertStoreLocation "Cert:\CurrentUser\My" `
                -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
            $signThumb = $cert.Thumbprint
            Write-Host "[package-msix] dev cert thumbprint: $signThumb"

            # Export the public certificate so it can be installed as a trusted root
            # on the target machine before running Add-AppxPackage.
            $cerPath = Join-Path $OutDir "remote-agent-dev.cer"
            Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT | Out-Null
            Write-Host "[package-msix] exported public cert: $cerPath"
            Write-Host "[package-msix] To trust on target machine:"
            Write-Host "               Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\LocalMachine\Root"
        }
    }
}

if ($signThumb -and $SignTool) {
    Write-Host "[package-msix] signing with thumbprint $signThumb ..."
    & $SignTool sign /sha1 $signThumb /fd SHA256 /tr http://timestamp.digicert.com /td sha256 "$MsixFile"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "[package-msix] signed: $MsixFile"
} elseif (-not $signThumb) {
    Write-Warning "Package is unsigned. To install it locally, use:"
    Write-Warning "  Add-AppxPackage -Path '$MsixFile' -AllowUnsigned"
}

# ── Summary ────────────────────────────────────────────────────────────────────
$msixSize   = [math]::Round((Get-Item $MsixFile).Length / 1MB, 1)
$installPs1 = Join-Path $PSScriptRoot "install-remote-agent.ps1"
$cerArg     = if ($DevCert -and (Test-Path (Join-Path $OutDir "remote-agent-dev.cer"))) {
    " -CertPath '$(Join-Path $OutDir 'remote-agent-dev.cer')'"
} else { "" }

Write-Host ""
Write-Host "── MSIX package ready ──────────────────────────────────────────────────"
Write-Host "  File     : $MsixFile  ($msixSize MB)"
Write-Host "  Identity : RemoteAgent  $Version4  $MsixArch"
Write-Host "  Publisher: $Publisher"
if ($BuildService) { Write-Host "  Service  : service\RemoteAgent.Service.exe  (RemoteAgentService, Automatic, LocalSystem)" }
if ($BuildDesktop) { Write-Host "  Desktop  : desktop\RemoteAgent.Desktop.exe  (Start menu: Remote Agent Desktop)" }
Write-Host ""
Write-Host "  Install + start service (run as Administrator):"
Write-Host "    .\scripts\install-remote-agent.ps1$cerArg"
Write-Host "  -- or manually --"
if ($signThumb) {
    Write-Host "    Add-AppxPackage -Path '$MsixFile'"
} else {
    Write-Host "    Add-AppxPackage -Path '$MsixFile' -AllowUnsigned"
}
if ($BuildService) {
    Write-Host "    Start-Service RemoteAgentService"
}
Write-Host ""
Write-Host "  Uninstall:"
Write-Host "    .\scripts\install-remote-agent.ps1 -Uninstall"
Write-Host "────────────────────────────────────────────────────────────────────────"
Write-Host ""
Write-Host "[package-msix] calculated version : $Version  (MSIX identity: $Version4)"
