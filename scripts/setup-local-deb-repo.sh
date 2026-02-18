#!/usr/bin/env bash
# setup-local-deb-repo.sh — Build a local APT repository from the .deb packages
# produced by package-deb.sh and register it with apt for local testing.
#
# Usage:
#   ./scripts/setup-local-deb-repo.sh [OPTIONS]
#
# Options:
#   --repo-dir <path>      Directory containing .deb files (default: <repo-root>/artifacts/)
#   --port <number>        Start an HTTP server on this port and register an http:// apt source.
#                          Omit to use a file:// apt source instead (no server needed).
#   --no-apt-update        Skip 'apt-get update' after writing the sources.list entry.
#   --no-sources-list      Skip writing /etc/apt/sources.list.d/ (print the entry instead).
#   --help
#
# What this script does:
#   1. Runs dpkg-scanpackages to generate Packages + Packages.gz
#   2. Runs apt-ftparchive to generate a signed-compatible Release file
#   3. Writes /etc/apt/sources.list.d/remote-agent-local.list  (requires sudo)
#   4. Runs apt-get update                                       (requires sudo)
#   5. If --port is set, starts a background Python HTTP server
#
# After this script:
#   sudo apt install remote-agent-service remote-agent-desktop
#
# To remove the local source:
#   sudo rm /etc/apt/sources.list.d/remote-agent-local.list && sudo apt-get update
#
# Requirements: dpkg-dev  apt-utils  gzip  python3
#   sudo apt install dpkg-dev apt-utils

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# ── Defaults ──────────────────────────────────────────────────────────────────
REPO_DIR="$REPO_ROOT/artifacts"
HTTP_PORT=""
DO_APT_UPDATE=true
DO_SOURCES_LIST=true

# ── Argument parsing ──────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-dir)        REPO_DIR="$2";       shift 2 ;;
    --port)            HTTP_PORT="$2";      shift 2 ;;
    --no-apt-update)   DO_APT_UPDATE=false; shift   ;;
    --no-sources-list) DO_SOURCES_LIST=false; shift ;;
    -h|--help)
      grep '^#' "$0" | head -n 30 | sed 's/^# \?//'
      exit 0 ;;
    *)
      echo "[local-repo] Unknown argument: $1" >&2
      exit 1 ;;
  esac
done

# ── Validate repo directory ───────────────────────────────────────────────────
if [[ ! -d "$REPO_DIR" ]]; then
  echo "[local-repo] ERROR: repo directory not found: $REPO_DIR" >&2
  echo "[local-repo] Run ./scripts/package-deb.sh first to build the packages." >&2
  exit 1
fi

DEB_COUNT="$(find "$REPO_DIR" -maxdepth 1 -name '*.deb' | wc -l)"
if [[ "$DEB_COUNT" -eq 0 ]]; then
  echo "[local-repo] ERROR: no .deb files found in $REPO_DIR" >&2
  echo "[local-repo] Run ./scripts/package-deb.sh first to build the packages." >&2
  exit 1
fi

echo "[local-repo] repo dir : $REPO_DIR  ($DEB_COUNT .deb file(s))"

# ── Check required tools ──────────────────────────────────────────────────────
missing=()
command -v dpkg-scanpackages > /dev/null 2>&1 || missing+=(dpkg-dev)
command -v apt-ftparchive    > /dev/null 2>&1 || missing+=(apt-utils)
command -v gzip              > /dev/null 2>&1 || missing+=(gzip)
command -v python3           > /dev/null 2>&1 || missing+=(python3)
if [[ ${#missing[@]} -gt 0 ]]; then
  echo "[local-repo] ERROR: missing tools. Install with:" >&2
  echo "  sudo apt install ${missing[*]}" >&2
  exit 1
fi

# ── Generate Packages ─────────────────────────────────────────────────────────
echo "[local-repo] scanning packages..."
# dpkg-scanpackages scans the directory for .deb files and writes a Packages
# index to stdout. The second argument ('.') is the path prefix written into
# the Filename: fields (relative to the repo root).
cd "$REPO_DIR"
dpkg-scanpackages --multiversion . > Packages 2>/dev/null
gzip -9 -k -f Packages          # produces Packages.gz, keeps original
echo "[local-repo] wrote: Packages, Packages.gz"

# ── Generate Release ──────────────────────────────────────────────────────────
# apt-ftparchive release generates a Release file containing SHA256 checksums
# for Packages and Packages.gz. apt uses this to verify the index integrity.
# We use [trusted=yes] in the source entry to skip the GPG signature check,
# but still provide Release so apt can detect index tampering.
echo "[local-repo] generating Release file..."
LABEL="Remote Agent Local"
SUITE="local"
ORIGIN="remote-agent-local"

apt-ftparchive \
  -o "APT::FTPArchive::Release::Origin=${ORIGIN}" \
  -o "APT::FTPArchive::Release::Label=${LABEL}" \
  -o "APT::FTPArchive::Release::Suite=${SUITE}" \
  -o "APT::FTPArchive::Release::Codename=${SUITE}" \
  -o "APT::FTPArchive::Release::Architectures=$(dpkg-scanpackages --multiversion . 2>/dev/null | grep '^Architecture:' | awk '{print $2}' | sort -u | tr '\n' ' ')" \
  -o "APT::FTPArchive::Release::Components=./" \
  -o "APT::FTPArchive::Release::Description=${LABEL} repository for testing" \
  release . > Release
echo "[local-repo] wrote: Release"

# ── Determine apt source URI ──────────────────────────────────────────────────
if [[ -n "$HTTP_PORT" ]]; then
  APT_URI="http://localhost:${HTTP_PORT}"
else
  # file:// URIs must use the absolute path; the trailing '/' is required.
  APT_URI="file://${REPO_DIR}"
fi

# Flat-format repo: suite is './' (no dists/ hierarchy).
# [trusted=yes] disables GPG signature verification for local testing.
APT_SOURCE_LINE="deb [trusted=yes] ${APT_URI}/ ./"
SOURCES_FILE="/etc/apt/sources.list.d/remote-agent-local.list"

echo "[local-repo] apt source line: ${APT_SOURCE_LINE}"

# ── Write sources.list entry ──────────────────────────────────────────────────
if [[ "$DO_SOURCES_LIST" == "true" ]]; then
  if [[ "$EUID" -eq 0 ]]; then
    SUDO=""
  elif command -v sudo > /dev/null 2>&1; then
    SUDO="sudo"
  else
    echo "[local-repo] WARNING: not root and sudo not found; skipping sources.list." >&2
    echo "[local-repo] Add manually:  echo '${APT_SOURCE_LINE}' | sudo tee ${SOURCES_FILE}" >&2
    DO_SOURCES_LIST=false
  fi

  if [[ "$DO_SOURCES_LIST" == "true" ]]; then
    echo "[local-repo] writing ${SOURCES_FILE}..."
    echo "# Remote Agent local test repository — generated by scripts/setup-local-deb-repo.sh" \
      | $SUDO tee "$SOURCES_FILE" > /dev/null
    echo "# Remove: sudo rm ${SOURCES_FILE} && sudo apt-get update" \
      | $SUDO tee -a "$SOURCES_FILE" > /dev/null
    echo "${APT_SOURCE_LINE}" \
      | $SUDO tee -a "$SOURCES_FILE" > /dev/null
    echo "[local-repo] wrote: ${SOURCES_FILE}"
  fi
else
  echo "[local-repo] skipped sources.list (--no-sources-list)."
  echo "[local-repo] To add manually:"
  echo "  echo '${APT_SOURCE_LINE}' | sudo tee ${SOURCES_FILE}"
fi

# ── Run apt-get update ────────────────────────────────────────────────────────
if [[ "$DO_APT_UPDATE" == "true" && "$DO_SOURCES_LIST" == "true" ]]; then
  echo "[local-repo] running apt-get update..."
  $SUDO apt-get update -o "Dir::Etc::sourcelist=${SOURCES_FILE}" \
                       -o "Dir::Etc::sourceparts=-" \
                       -o "APT::Get::List-Cleanup=0"
  echo "[local-repo] apt index updated."
else
  echo "[local-repo] skipped apt-get update (--no-apt-update or sources.list not written)."
  echo "[local-repo] To update manually:"
  echo "  sudo apt-get update"
fi

# ── Start HTTP server (optional) ──────────────────────────────────────────────
if [[ -n "$HTTP_PORT" ]]; then
  SERVER_PID_FILE="$REPO_DIR/.repo-server.pid"

  # Stop any previously started server for this repo.
  if [[ -f "$SERVER_PID_FILE" ]]; then
    OLD_PID="$(cat "$SERVER_PID_FILE" 2>/dev/null || true)"
    if [[ -n "$OLD_PID" ]] && kill -0 "$OLD_PID" 2>/dev/null; then
      echo "[local-repo] stopping previous HTTP server (PID ${OLD_PID})..."
      kill "$OLD_PID" 2>/dev/null || true
    fi
    rm -f "$SERVER_PID_FILE"
  fi

  echo "[local-repo] starting HTTP server on port ${HTTP_PORT} (serving ${REPO_DIR})..."
  python3 -m http.server "$HTTP_PORT" --directory "$REPO_DIR" \
    > "$REPO_DIR/.repo-server.log" 2>&1 &
  SERVER_PID=$!
  echo "$SERVER_PID" > "$SERVER_PID_FILE"
  sleep 0.5

  # Verify server is running.
  if kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "[local-repo] HTTP server running (PID ${SERVER_PID})"
    echo "[local-repo] logs: ${REPO_DIR}/.repo-server.log"
    echo "[local-repo] stop: kill ${SERVER_PID}"
  else
    echo "[local-repo] ERROR: HTTP server failed to start. Check ${REPO_DIR}/.repo-server.log" >&2
    exit 1
  fi
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "── Local APT repository ready ──────────────────────────────────────────"
echo "  Packages:"
find "$REPO_DIR" -maxdepth 1 -name '*.deb' | sort | while read -r f; do
  printf "    %s\n" "$(basename "$f")"
done
echo ""
if [[ -n "$HTTP_PORT" ]]; then
  echo "  URL      : http://localhost:${HTTP_PORT}/"
else
  echo "  URL      : file://${REPO_DIR}/"
fi
echo "  Source   : ${APT_SOURCE_LINE}"
if [[ -f "$SOURCES_FILE" ]]; then
  echo "  Registered: ${SOURCES_FILE}"
fi
echo ""
echo "  Install packages:"
echo "    sudo apt install remote-agent-service"
echo "    sudo apt install remote-agent-desktop"
echo ""
echo "  Remove local source:"
echo "    sudo rm ${SOURCES_FILE} && sudo apt-get update"
echo "────────────────────────────────────────────────────────────────────────"
