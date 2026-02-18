#!/usr/bin/env bash
# install-deb-packages.sh — Register the local APT repo and install Remote Agent packages.
#
# Usage:
#   sudo ./scripts/install-deb-packages.sh [OPTIONS]
#
# Options:
#   --repo-dir <path>      Directory containing the .deb files and APT index.
#                          Default: <repo-root>/artifacts/
#   --service-only         Install only remote-agent-service.
#   --desktop-only         Install only remote-agent-desktop (pulls in service as dep).
#   --reinstall            Pass --reinstall to apt-get (re-installs already-installed packages).
#   --remove               Stop service and remove both packages instead of installing.
#   --help

set -euo pipefail

if [[ "$EUID" -ne 0 ]]; then
  echo "ERROR: this script must be run as root.  Use: sudo $0 $*" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_DIR="$REPO_ROOT/artifacts"
SOURCES_FILE="/etc/apt/sources.list.d/remote-agent-local.list"

INSTALL_SERVICE=true
INSTALL_DESKTOP=true
REINSTALL_FLAG=""
REMOVE=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-dir)      REPO_DIR="$2"; shift 2 ;;
    --service-only)  INSTALL_DESKTOP=false; shift ;;
    --desktop-only)  INSTALL_SERVICE=false; shift ;;
    --reinstall)     REINSTALL_FLAG="--reinstall"; shift ;;
    --remove)        REMOVE=true; shift ;;
    -h|--help) grep '^#' "$0" | head -n 15 | sed 's/^# \?//'; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

# ── Remove path ───────────────────────────────────────────────────────────────
if [[ "$REMOVE" == "true" ]]; then
  echo "[install-deb] stopping remote-agent service..."
  if [ -d /run/systemd/system ]; then
    systemctl stop    remote-agent.service 2>/dev/null || true
    systemctl disable remote-agent.service 2>/dev/null || true
  else
    _pid_file="/run/remote-agent.pid"
    if [[ -f "$_pid_file" ]]; then
      _pid="$(cat "$_pid_file" 2>/dev/null || true)"
      [[ -n "$_pid" ]] && kill "$_pid" 2>/dev/null || true
      rm -f "$_pid_file"
    fi
  fi

  echo "[install-deb] removing packages..."
  apt-get remove -y remote-agent-desktop remote-agent-service 2>/dev/null || true

  echo "[install-deb] removing local APT source..."
  rm -f "$SOURCES_FILE"
  apt-get update -o "Dir::Etc::sourcelist=$SOURCES_FILE" \
                 -o "Dir::Etc::sourceparts=-" \
                 -o "APT::Get::List-Cleanup=0" 2>/dev/null || true

  echo "[install-deb] done."
  exit 0
fi

# ── Validate repo dir ─────────────────────────────────────────────────────────
if [[ ! -f "$REPO_DIR/Packages" ]]; then
  echo "[install-deb] ERROR: APT index not found in $REPO_DIR" >&2
  echo "[install-deb] Run ./scripts/setup-local-deb-repo.sh first." >&2
  exit 1
fi

# ── Register APT source ───────────────────────────────────────────────────────
echo "[install-deb] registering local APT source: file://${REPO_DIR}/"
{
  echo "# Remote Agent local test repository"
  echo "# Remove: sudo rm $SOURCES_FILE && sudo apt-get update"
  echo "deb [trusted=yes] file://${REPO_DIR}/ ./"
} > "$SOURCES_FILE"

# ── Update index ──────────────────────────────────────────────────────────────
echo "[install-deb] running apt-get update..."
apt-get update \
  -o "Dir::Etc::sourcelist=$SOURCES_FILE" \
  -o "Dir::Etc::sourceparts=-" \
  -o "APT::Get::List-Cleanup=0"

# ── Install ───────────────────────────────────────────────────────────────────
PACKAGES=()
# Always include desktop when requested (it pulls service as a dep).
# When both are requested with --reinstall, list both explicitly so apt
# reinstalls service too (apt only reinstalls packages explicitly named).
if [[ "$INSTALL_DESKTOP" == "true" ]]; then
  PACKAGES+=(remote-agent-desktop)
  # With --reinstall, explicitly name service so apt reinstalls it too.
  [[ -n "$REINSTALL_FLAG" && "$INSTALL_SERVICE" == "true" ]] && PACKAGES+=(remote-agent-service)
else
  [[ "$INSTALL_SERVICE" == "true" ]] && PACKAGES+=(remote-agent-service)
fi

echo "[install-deb] installing: ${PACKAGES[*]}"
apt-get install -y $REINSTALL_FLAG "${PACKAGES[@]}"

# ── Report ────────────────────────────────────────────────────────────────────
echo ""
echo "── Installation complete ───────────────────────────────────────────────"
for pkg in remote-agent-service remote-agent-desktop; do
  if dpkg -s "$pkg" &>/dev/null; then
    ver=$(dpkg -s "$pkg" | awk '/^Version:/{print $2}')
    printf "  %-30s %s\n" "$pkg" "$ver"
  fi
done

# Check service status: prefer systemd, fall back to PID file on non-systemd (WSL).
_svc_status="not running"
if [ -d /run/systemd/system ]; then
  systemctl is-active --quiet remote-agent.service 2>/dev/null \
    && _svc_status="active (running)"
else
  _pid_file="/run/remote-agent.pid"
  if [[ -f "$_pid_file" ]]; then
    _pid="$(cat "$_pid_file" 2>/dev/null || true)"
    if [[ -n "$_pid" ]] && kill -0 "$_pid" 2>/dev/null; then
      _svc_status="running (pid $_pid, no systemd)"
    fi
  fi
fi
printf "  %-30s %s\n" "remote-agent.service" "$_svc_status"
echo "────────────────────────────────────────────────────────────────────────"
echo ""
if [ -d /run/systemd/system ]; then
  echo "  Service logs : journalctl -u remote-agent.service -f"
else
  echo "  Service logs : tail -f /var/log/remote-agent/service.log"
fi
echo "  Remove       : sudo $0 --remove"
