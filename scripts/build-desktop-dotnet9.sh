#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUGET_CONFIG="$REPO_ROOT/NuGet.Config"

bash "$SCRIPT_DIR/clean-stale-assets.sh"

pushd "$REPO_ROOT/src/RemoteAgent.Desktop" >/dev/null
  if ! dotnet --version | grep -q '^9\.'; then
    echo "[dotnet9] expected .NET SDK 9.x in src/RemoteAgent.Desktop"
    echo "[dotnet9] current SDK: $(dotnet --version)"
    exit 1
  fi
  echo "[dotnet9] restoring Avalonia desktop app"
  dotnet restore RemoteAgent.Desktop.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
  echo "[dotnet9] building Avalonia desktop app"
  dotnet build RemoteAgent.Desktop.csproj -c "$CONFIGURATION" --no-restore
popd >/dev/null

pushd "$REPO_ROOT/tests/RemoteAgent.Desktop.UiTests" >/dev/null
  if ! dotnet --version | grep -q '^9\.'; then
    echo "[dotnet9] expected .NET SDK 9.x in tests/RemoteAgent.Desktop.UiTests"
    echo "[dotnet9] current SDK: $(dotnet --version)"
    exit 1
  fi
  echo "[dotnet9] restoring desktop UI tests"
  dotnet restore RemoteAgent.Desktop.UiTests.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
  echo "[dotnet9] running desktop UI tests"
  dotnet test RemoteAgent.Desktop.UiTests.csproj -c "$CONFIGURATION" --verbosity minimal -nologo
popd >/dev/null
