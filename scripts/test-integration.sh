#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUGET_CONFIG="$REPO_ROOT/NuGet.Config"

bash "$SCRIPT_DIR/clean-stale-assets.sh"
pushd "$REPO_ROOT" >/dev/null

if ! dotnet --version | grep -q '^10\.'; then
  echo "[integration] expected .NET SDK 10.x"
  echo "[integration] current SDK: $(dotnet --version)"
  exit 1
fi

echo "[integration] restoring service integration test project"
dotnet restore tests/RemoteAgent.Service.IntegrationTests/RemoteAgent.Service.IntegrationTests.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources

echo "[integration] running integration tests"
dotnet test tests/RemoteAgent.Service.IntegrationTests/RemoteAgent.Service.IntegrationTests.csproj -c "$CONFIGURATION" --verbosity minimal -nologo

popd >/dev/null
