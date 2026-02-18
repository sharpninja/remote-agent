#!/usr/bin/env bash
# package-deb.sh — Build Debian packages for the Remote Agent service and desktop app.
#
# Usage:
#   ./scripts/package-deb.sh [OPTIONS]
#
# Options:
#   --configuration <Release|Debug>     Build configuration (default: Release)
#   --version <x.y.z>                   Package version (default: from git tag, else 1.0.0)
#   --rid <linux-x64|linux-arm64|...>   .NET runtime identifier (default: linux-x64)
#   --self-contained                    Build self-contained packages (bundles .NET runtime)
#   --service-only                      Build only the service package
#   --desktop-only                      Build only the desktop package
#   --out-dir <path>                    Output directory (default: <repo-root>/artifacts/)
#
# Output:
#   artifacts/remote-agent-service_<ver>_<arch>.deb
#   artifacts/remote-agent-desktop_<ver>_<arch>.deb
#
# Requirements on the packaging machine:
#   - .NET SDK 10.x (service + plugin) and 9.x (desktop; resolved via src/RemoteAgent.Desktop/global.json)
#   - dpkg-deb  (package: dpkg, available on any Debian/Ubuntu host)
#
# Requirements on the target machine:
#   - systemd (service registration)
#   - aspnetcore-runtime-10.0 / dotnet-runtime-9.0 (framework-dependent builds only)
#   - With --self-contained: no .NET runtime needed
#
# Service install layout:
#   /usr/lib/remote-agent/service/     application binaries
#   /usr/lib/remote-agent/plugins/     optional agent plugins (e.g. Ollama)
#   /etc/remote-agent/                 configuration (conffiles — preserved on upgrade)
#   /var/lib/remote-agent/             runtime data (LiteDB, media uploads)
#   /var/log/remote-agent/             session log files
#   /lib/systemd/system/               remote-agent.service unit
#
# Desktop install layout:
#   /usr/lib/remote-agent/desktop/     application binaries
#   /usr/bin/remote-agent-desktop      launcher wrapper
#   /usr/share/applications/           .desktop entry (appears in app menus)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUGET_CONFIG="$REPO_ROOT/NuGet.Config"

# ── Defaults ──────────────────────────────────────────────────────────────────
CONFIGURATION="Release"
VERSION=""
RID="linux-x64"
SELF_CONTAINED=false
BUILD_SERVICE=true
BUILD_DESKTOP=true
OUT_DIR="$REPO_ROOT/artifacts"

# ── Argument parsing ──────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)  CONFIGURATION="$2"; shift 2 ;;
    --version)        VERSION="$2";        shift 2 ;;
    --rid)            RID="$2";            shift 2 ;;
    --self-contained) SELF_CONTAINED=true; shift   ;;
    --service-only)   BUILD_DESKTOP=false; shift   ;;
    --desktop-only)   BUILD_SERVICE=false; shift   ;;
    --out-dir)        OUT_DIR="$2";        shift 2 ;;
    -h|--help)
      grep '^#' "$0" | head -n 40 | sed 's/^# \?//'
      exit 0 ;;
    *)
      echo "[package-deb] Unknown argument: $1" >&2
      exit 1 ;;
  esac
done

# ── Version detection ─────────────────────────────────────────────────────────
if [[ -z "$VERSION" ]]; then
  if git -C "$REPO_ROOT" describe --tags --abbrev=0 2>/dev/null | grep -qE '^v?[0-9]'; then
    VERSION="$(git -C "$REPO_ROOT" describe --tags --abbrev=0 2>/dev/null | sed 's/^v//')"
  else
    VERSION="1.0.0"
  fi
fi

# ── Architecture mapping ──────────────────────────────────────────────────────
case "$RID" in
  linux-x64)      ARCH="amd64" ;;
  linux-arm64)    ARCH="arm64" ;;
  linux-arm)      ARCH="armhf" ;;
  linux-musl-x64) ARCH="amd64" ;;
  *)              ARCH="${RID#linux-}" ;;
esac

# ── Runtime dependency strings (empty when self-contained) ───────────────────
if [[ "$SELF_CONTAINED" == "true" ]]; then
  SC_FLAG="--self-contained true"
  DEP_SERVICE=""
  DEP_DESKTOP="remote-agent-service (= ${VERSION})"
else
  SC_FLAG=""
  DEP_SERVICE="aspnetcore-runtime-10.0 | dotnet-runtime-10.0"
  DEP_DESKTOP="remote-agent-service (= ${VERSION}), dotnet-runtime-9.0"
fi

echo "[package-deb] version=${VERSION}  rid=${RID}  arch=${ARCH}  config=${CONFIGURATION}  self-contained=${SELF_CONTAINED}"
echo "[package-deb] service=${BUILD_SERVICE}  desktop=${BUILD_DESKTOP}  out=${OUT_DIR}"

# ── Prepare workspace ─────────────────────────────────────────────────────────
BUILD_TMP="$REPO_ROOT/deb-build"
rm -rf "$BUILD_TMP"
mkdir -p "$OUT_DIR" "$BUILD_TMP"

# ── Publish: service ─────────────────────────────────────────────────────────
if [[ "$BUILD_SERVICE" == "true" ]]; then
  echo "[package-deb] publishing service (net10.0, $RID)..."
  SERVICE_PUBLISH="$BUILD_TMP/service-publish"

  pushd "$REPO_ROOT" >/dev/null
  dotnet restore src/RemoteAgent.Service/RemoteAgent.Service.csproj \
    --configfile "$NUGET_CONFIG" --ignore-failed-sources
  dotnet publish src/RemoteAgent.Service/RemoteAgent.Service.csproj \
    -c "$CONFIGURATION" \
    -r "$RID" \
    $SC_FLAG \
    -o "$SERVICE_PUBLISH"
  popd >/dev/null

  # ── Publish: Ollama plugin ───────────────────────────────────────────────
  echo "[package-deb] publishing Ollama plugin..."
  PLUGIN_PUBLISH="$BUILD_TMP/plugin-publish"

  pushd "$REPO_ROOT" >/dev/null
  dotnet restore src/RemoteAgent.Plugins.Ollama/RemoteAgent.Plugins.Ollama.csproj \
    --configfile "$NUGET_CONFIG" --ignore-failed-sources
  # Plugin always framework-dependent (it loads into the service's runtime process).
  dotnet publish src/RemoteAgent.Plugins.Ollama/RemoteAgent.Plugins.Ollama.csproj \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained false \
    -o "$PLUGIN_PUBLISH"
  popd >/dev/null
fi

# ── Publish: desktop ─────────────────────────────────────────────────────────
if [[ "$BUILD_DESKTOP" == "true" ]]; then
  echo "[package-deb] publishing desktop app (net9.0, $RID)..."
  DESKTOP_PUBLISH="$BUILD_TMP/desktop-publish"

  # Publish from the repo root using SDK 10, which can build both net9.0 (Desktop)
  # and net10.0 (App.Logic dependency) in one pass. The Desktop project's own
  # per-directory global.json is intentionally bypassed here; it exists only to
  # validate SDK-9 compatibility in build-desktop-dotnet9.sh, not for packaging.
  dotnet restore src/RemoteAgent.Desktop/RemoteAgent.Desktop.csproj \
    --configfile "$NUGET_CONFIG" --ignore-failed-sources
  dotnet publish src/RemoteAgent.Desktop/RemoteAgent.Desktop.csproj \
    -c "$CONFIGURATION" \
    -r "$RID" \
    -f net9.0 \
    $SC_FLAG \
    -o "$DESKTOP_PUBLISH"
fi

# ══════════════════════════════════════════════════════════════════════════════
# Helper: write a Debian control file
# ══════════════════════════════════════════════════════════════════════════════
write_control() {
  local file="$1" pkg="$2" ver="$3" arch="$4" dep="$5" short_desc="$6"
  shift 6
  # Remaining args are long description continuation lines (must start with " ").

  {
    echo "Package: $pkg"
    echo "Version: $ver"
    echo "Architecture: $arch"
    echo "Maintainer: Remote Agent Contributors <https://github.com/sharpninja/remote-agent>"
    echo "Section: net"
    echo "Priority: optional"
    [[ -n "$dep" ]] && echo "Depends: $dep"
    echo "Description: $short_desc"
    for line in "$@"; do
      echo "$line"
    done
  } > "$file"
}

# ══════════════════════════════════════════════════════════════════════════════
# Build: service .deb
# ══════════════════════════════════════════════════════════════════════════════
if [[ "$BUILD_SERVICE" == "true" ]]; then
  SVC_PKG="remote-agent-service"
  SVC_DIR="$BUILD_TMP/${SVC_PKG}_${VERSION}_${ARCH}"
  echo "[package-deb] assembling $SVC_PKG..."

  # ── Application binaries ─────────────────────────────────────────────────
  install -d "$SVC_DIR/usr/lib/remote-agent/service"
  cp -a "$SERVICE_PUBLISH/." "$SVC_DIR/usr/lib/remote-agent/service/"
  chmod 755 "$SVC_DIR/usr/lib/remote-agent/service/RemoteAgent.Service"

  # ── Ollama plugin ─────────────────────────────────────────────────────────
  install -d "$SVC_DIR/usr/lib/remote-agent/plugins"
  cp "$PLUGIN_PUBLISH/RemoteAgent.Plugins.Ollama.dll" \
     "$SVC_DIR/usr/lib/remote-agent/plugins/"

  # ── Default configuration ─────────────────────────────────────────────────
  install -d "$SVC_DIR/etc/remote-agent"

  # Copy the source appsettings.json and patch in deb-specific runtime paths.
  # This ensures the installed config always matches the source defaults
  # (including Urls, Kestrel settings, etc.) without duplication.
  if command -v jq > /dev/null 2>&1; then
    jq '.Agent.LogDirectory = "/var/log/remote-agent" |
        .Agent.DataDirectory = "/var/lib/remote-agent"' \
      "$REPO_ROOT/src/RemoteAgent.Service/appsettings.json" \
      > "$SVC_DIR/etc/remote-agent/appsettings.json"
  else
    # jq not available — copy verbatim; paths can be set post-install.
    cp "$REPO_ROOT/src/RemoteAgent.Service/appsettings.json" \
       "$SVC_DIR/etc/remote-agent/appsettings.json"
  fi

  # Environment file: systemd reads this to set ASPNETCORE_* variables.
  # ASPNETCORE_CONTENTROOT tells ASP.NET Core where to find appsettings.json.
  cat > "$SVC_DIR/etc/remote-agent/environment" <<'EOF'
# Remote Agent environment variables.
# Edit to override service behaviour; takes effect after:  systemctl restart remote-agent
#
# The listen URL and most settings are configured in appsettings.json.
# Use this file for environment-specific overrides only, e.g.:
#   ASPNETCORE_ENVIRONMENT=Production

# Point ASP.NET Core configuration root at /etc/remote-agent so that
# appsettings.json is loaded from the config directory rather than the
# binary directory.
ASPNETCORE_CONTENTROOT=/etc/remote-agent
EOF

  # ── Systemd unit ──────────────────────────────────────────────────────────
  install -d "$SVC_DIR/lib/systemd/system"
  cat > "$SVC_DIR/lib/systemd/system/remote-agent.service" <<'EOF'
[Unit]
Description=Remote Agent gRPC Service
Documentation=https://github.com/sharpninja/remote-agent
After=network.target

[Service]
Type=simple
User=remote-agent
Group=remote-agent
WorkingDirectory=/usr/lib/remote-agent/service

# Load overrides from /etc/remote-agent/environment (optional; '-' suppresses
# "file not found" errors so the unit still starts if the file is absent).
EnvironmentFile=-/etc/remote-agent/environment

ExecStart=/usr/lib/remote-agent/service/RemoteAgent.Service

Restart=on-failure
RestartSec=5
# Do not restart on clean exit (e.g. intentional shutdown).
RestartPreventExitStatus=0

StandardOutput=journal
StandardError=journal
SyslogIdentifier=remote-agent

# Minimal hardening: drop privilege escalation and isolate /tmp.
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
EOF

  # ── DEBIAN metadata ───────────────────────────────────────────────────────
  install -d "$SVC_DIR/DEBIAN"

  # conffiles: dpkg preserves these files during upgrades instead of overwriting.
  cat > "$SVC_DIR/DEBIAN/conffiles" <<'EOF'
/etc/remote-agent/appsettings.json
/etc/remote-agent/environment
EOF

  write_control \
    "$SVC_DIR/DEBIAN/control" \
    "$SVC_PKG" "$VERSION" "$ARCH" "$DEP_SERVICE" \
    "Remote Agent gRPC Service" \
    " ASP.NET Core gRPC service that spawns and manages CLI agents (Cursor," \
    " Ollama, GitHub Copilot CLI, etc.) and streams their output to connected" \
    " client apps over a persistent bidirectional gRPC stream."

  # ── postinst ──────────────────────────────────────────────────────────────
  cat > "$SVC_DIR/DEBIAN/postinst" <<'POSTINST'
#!/bin/bash
set -e

SERVICE_USER="remote-agent"
SERVICE_GROUP="remote-agent"

case "$1" in
  configure)
    # Create system group and user for the service process.
    if ! getent group "$SERVICE_GROUP" > /dev/null 2>&1; then
      addgroup --system "$SERVICE_GROUP"
    fi
    if ! getent passwd "$SERVICE_USER" > /dev/null 2>&1; then
      adduser --system \
              --ingroup "$SERVICE_GROUP" \
              --no-create-home \
              --disabled-password \
              --home /var/lib/remote-agent \
              --gecos "Remote Agent Service" \
              "$SERVICE_USER"
    fi

    # Runtime directories.
    install -d -m 750 -o "$SERVICE_USER" -g "$SERVICE_GROUP" /var/lib/remote-agent
    install -d -m 750 -o "$SERVICE_USER" -g "$SERVICE_GROUP" /var/log/remote-agent

    # Config files: root-owned but group-readable by the service user.
    chown root:"$SERVICE_GROUP" /etc/remote-agent/appsettings.json
    chmod 640 /etc/remote-agent/appsettings.json
    chown root:"$SERVICE_GROUP" /etc/remote-agent/environment
    chmod 640 /etc/remote-agent/environment

    # Register and start/restart the systemd unit.
    # [ -d /run/systemd/system ] is the canonical check for systemd being the
    # active init; it works even in degraded state unlike is-system-running.
    if command -v systemctl > /dev/null 2>&1; then
      systemctl daemon-reload                 || true
      systemctl enable remote-agent.service   || true
      if [ -d /run/systemd/system ]; then
        if [ -n "$2" ]; then
          # Upgrade — restart to pick up new binaries.
          systemctl restart remote-agent.service || true
        else
          # Fresh install — start for the first time.
          systemctl start remote-agent.service   || true
        fi
      fi
    fi

    # ── Daemonize fallback (WSL / Pengwin without systemd) ──────────────────
    # When systemd is not the active init (e.g. WSL without systemd=true),
    # fall back to daemonize to start the service immediately, and write a
    # [boot] command to /etc/wsl.conf so it auto-starts on every WSL launch.
    if [ ! -d /run/systemd/system ]; then
      _svc="/usr/lib/remote-agent/service/RemoteAgent.Service"
      _log="/var/log/remote-agent/service.log"
      _err="/var/log/remote-agent/service.err"
      _pid="/run/remote-agent.pid"

      # Write a small wrapper so /etc/wsl.conf [boot] command= stays simple.
      _wrapper="/usr/lib/remote-agent/service/wsl-start.sh"
      cat > "$_wrapper" <<'WSLWRAP'
#!/bin/sh
# Wrapper started by /etc/wsl.conf [boot] command= (runs as remote-agent).
# Source environment overrides (ASPNETCORE_URLS, etc.) before exec.
if [ -f /etc/remote-agent/environment ]; then
  set -a
  # shellcheck disable=SC1091
  . /etc/remote-agent/environment 2>/dev/null || true
  set +a
fi
export ASPNETCORE_CONTENTROOT=/etc/remote-agent
exec /usr/lib/remote-agent/service/RemoteAgent.Service \
  >> /var/log/remote-agent/service.log 2>&1
WSLWRAP
      chmod 755 "$_wrapper"
      chown remote-agent:remote-agent "$_wrapper"

      if [ -x "$_svc" ]; then
        # Stop any running instance before (re)starting.
        start-stop-daemon --stop --quiet --retry 5 \
          --pidfile "$_pid" --exec "$_svc" 2>/dev/null || true
        sleep 1

        # Primary: start-stop-daemon (always available on Debian).
        # Falls back to daemonize if start-stop-daemon is unavailable.
        if command -v start-stop-daemon > /dev/null 2>&1; then
          echo "systemd not active — starting Remote Agent via start-stop-daemon"
          # Source the environment file so ASPNETCORE_URLS and other overrides apply.
          _env_file="/etc/remote-agent/environment"
          if [ -f "$_env_file" ]; then
            # Export only non-comment, non-empty lines.
            set -a
            # shellcheck disable=SC1090
            . "$_env_file" 2>/dev/null || true
            set +a
          fi
          ASPNETCORE_CONTENTROOT=/etc/remote-agent \
          start-stop-daemon --start --background \
            --make-pidfile --pidfile "$_pid" \
            --chuid remote-agent \
            --exec "$_svc" \
            -- >> "$_log" 2>> "$_err" || true
        else
          # Locate daemonize: Pengwin installs to /usr/bin, others to /usr/sbin.
          _daemonize=""
          for _p in /usr/bin/daemonize /usr/sbin/daemonize; do
            if [ -x "$_p" ]; then _daemonize="$_p"; break; fi
          done
          if [ -n "$_daemonize" ]; then
            echo "systemd not active — starting Remote Agent via $_daemonize"
            # Build env prefix from environment file.
            _env_prefix="ASPNETCORE_CONTENTROOT=/etc/remote-agent"
            if [ -f /etc/remote-agent/environment ]; then
              _env_prefix="$(grep -v '^\s*#' /etc/remote-agent/environment | grep -v '^\s*$' | tr '\n' ' ') $_env_prefix"
            fi
            su -s /bin/sh -c \
              "$_env_prefix $_daemonize -o $_log -e $_err $_svc" \
              remote-agent || true
          else
            echo "WARNING: cannot start service: neither start-stop-daemon nor daemonize found." >&2
          fi
        fi
      fi

      # On WSL: write [boot] command= to /etc/wsl.conf for auto-start.
      if grep -qEi 'microsoft|wsl' /proc/version 2>/dev/null; then
        if ! grep -q '^\[boot\]' /etc/wsl.conf 2>/dev/null; then
          {
            echo ""
            echo "[boot]"
            echo "# Remote Agent: auto-start on WSL launch (systemd not enabled)."
            echo "# For full systemd support add 'systemd = true' here, then: wsl --shutdown"
            echo 'command = "su -s /bin/sh remote-agent /usr/lib/remote-agent/service/wsl-start.sh"'
          } >> /etc/wsl.conf
          echo "Added [boot] command to /etc/wsl.conf — service will auto-start on WSL launch."
          echo "Tip: enable systemd for full service management: add 'systemd = true' under [boot]."
        else
          echo "NOTE: /etc/wsl.conf already has a [boot] section; skipping auto-start registration." >&2
          echo "      Add manually to [boot]: command = \"su -s /bin/sh remote-agent /usr/lib/remote-agent/service/wsl-start.sh\"" >&2
        fi
      fi
    fi
    ;;
esac

exit 0
POSTINST

  # ── prerm ─────────────────────────────────────────────────────────────────
  cat > "$SVC_DIR/DEBIAN/prerm" <<'PRERM'
#!/bin/bash
set -e

case "$1" in
  remove|upgrade|deconfigure)
    if command -v systemctl > /dev/null 2>&1; then
      systemctl stop    remote-agent.service 2>/dev/null || true
      systemctl disable remote-agent.service 2>/dev/null || true
    fi
    # Fallback: stop the daemonized process directly when systemd is not active.
    if [ ! -d /run/systemd/system ]; then
      _svc="/usr/lib/remote-agent/service/RemoteAgent.Service"
      _pid="/run/remote-agent.pid"
      start-stop-daemon --stop --quiet --retry 5 \
        --pidfile "$_pid" --exec "$_svc" 2>/dev/null || true
    fi
    ;;
esac

exit 0
PRERM

  # ── postrm ────────────────────────────────────────────────────────────────
  cat > "$SVC_DIR/DEBIAN/postrm" <<'POSTRM'
#!/bin/bash
set -e

case "$1" in
  purge)
    # Remove runtime data only on purge, not on plain remove.
    rm -rf /var/lib/remote-agent /var/log/remote-agent
    if command -v systemctl > /dev/null 2>&1; then
      systemctl daemon-reload || true
    fi
    ;;
  remove)
    if command -v systemctl > /dev/null 2>&1; then
      systemctl daemon-reload || true
    fi
    ;;
esac

exit 0
POSTRM

  chmod 755 \
    "$SVC_DIR/DEBIAN/postinst" \
    "$SVC_DIR/DEBIAN/prerm" \
    "$SVC_DIR/DEBIAN/postrm"

  # ── Build .deb ────────────────────────────────────────────────────────────
  SVC_DEB="$OUT_DIR/${SVC_PKG}_${VERSION}_${ARCH}.deb"
  dpkg-deb --build --root-owner-group "$SVC_DIR" "$SVC_DEB"
  echo "[package-deb] created: $SVC_DEB"
fi

# ══════════════════════════════════════════════════════════════════════════════
# Build: desktop .deb
# ══════════════════════════════════════════════════════════════════════════════
if [[ "$BUILD_DESKTOP" == "true" ]]; then
  DSK_PKG="remote-agent-desktop"
  DSK_DIR="$BUILD_TMP/${DSK_PKG}_${VERSION}_${ARCH}"
  echo "[package-deb] assembling $DSK_PKG..."

  # ── Application binaries ─────────────────────────────────────────────────
  install -d "$DSK_DIR/usr/lib/remote-agent/desktop"
  cp -a "$DESKTOP_PUBLISH/." "$DSK_DIR/usr/lib/remote-agent/desktop/"
  chmod 755 "$DSK_DIR/usr/lib/remote-agent/desktop/RemoteAgent.Desktop"

  # ── /usr/bin launcher wrapper ─────────────────────────────────────────────
  install -d "$DSK_DIR/usr/bin"
  cat > "$DSK_DIR/usr/bin/remote-agent-desktop" <<'EOF'
#!/bin/bash
exec /usr/lib/remote-agent/desktop/RemoteAgent.Desktop "$@"
EOF
  chmod 755 "$DSK_DIR/usr/bin/remote-agent-desktop"

  # ── .desktop entry (shows in GNOME/KDE application menus) ────────────────
  install -d "$DSK_DIR/usr/share/applications"
  cat > "$DSK_DIR/usr/share/applications/remote-agent-desktop.desktop" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Remote Agent Desktop
GenericName=Agent Management
Comment=Desktop management app for the Remote Agent gRPC service
Exec=/usr/bin/remote-agent-desktop
Icon=remote-agent-desktop
Categories=Development;Network;Utility;
Terminal=false
StartupNotify=true
StartupWMClass=RemoteAgent.Desktop
Keywords=remote;agent;gRPC;cursor;ollama;copilot;
EOF

  # ── DEBIAN metadata ───────────────────────────────────────────────────────
  install -d "$DSK_DIR/DEBIAN"

  write_control \
    "$DSK_DIR/DEBIAN/control" \
    "$DSK_PKG" "$VERSION" "$ARCH" "$DEP_DESKTOP" \
    "Remote Agent Desktop Management App" \
    " Avalonia UI desktop application for managing the Remote Agent gRPC service." \
    " Provides session monitoring, structured log viewing, plugin management," \
    " multi-server support, and concurrent connections."

  # ── postinst (update .desktop database) ──────────────────────────────────
  cat > "$DSK_DIR/DEBIAN/postinst" <<'POSTINST'
#!/bin/bash
set -e

case "$1" in
  configure)
    if command -v update-desktop-database > /dev/null 2>&1; then
      update-desktop-database /usr/share/applications || true
    fi
    ;;
esac

exit 0
POSTINST
  chmod 755 "$DSK_DIR/DEBIAN/postinst"

  # ── Build .deb ────────────────────────────────────────────────────────────
  DSK_DEB="$OUT_DIR/${DSK_PKG}_${VERSION}_${ARCH}.deb"
  dpkg-deb --build --root-owner-group "$DSK_DIR" "$DSK_DEB"
  echo "[package-deb] created: $DSK_DEB"
fi

# ── Cleanup ───────────────────────────────────────────────────────────────────
rm -rf "$BUILD_TMP"

echo ""
echo "[package-deb] packaging complete. Output:"
ls -lh "$OUT_DIR"/*.deb 2>/dev/null || true
echo ""
echo "Install with:"
if [[ "$BUILD_SERVICE" == "true" ]]; then
  echo "  sudo dpkg -i ${OUT_DIR}/remote-agent-service_${VERSION}_${ARCH}.deb"
fi
if [[ "$BUILD_DESKTOP" == "true" ]]; then
  echo "  sudo dpkg -i ${OUT_DIR}/remote-agent-desktop_${VERSION}_${ARCH}.deb"
fi
