#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NUGET_CONFIG="$REPO_ROOT/NuGet.Config"

if ! dotnet --version | grep -q '^10\.'; then
  echo "[dotnet10] expected .NET SDK 10.x in repo root"
  echo "[dotnet10] current SDK: $(dotnet --version)"
  exit 1
fi

bash "$SCRIPT_DIR/clean-stale-assets.sh"
pushd "$REPO_ROOT" >/dev/null

echo "[dotnet10] restoring MAUI/service projects"
dotnet restore src/RemoteAgent.Proto/RemoteAgent.Proto.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore src/RemoteAgent.App.Logic/RemoteAgent.App.Logic.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore src/RemoteAgent.Service/RemoteAgent.Service.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore src/RemoteAgent.App/RemoteAgent.App.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore tests/RemoteAgent.App.Tests/RemoteAgent.App.Tests.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore tests/RemoteAgent.Service.Tests/RemoteAgent.Service.Tests.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources
dotnet restore tests/RemoteAgent.Mobile.UiTests/RemoteAgent.Mobile.UiTests.csproj --configfile "$NUGET_CONFIG" --ignore-failed-sources

echo "[dotnet10] building server"
dotnet build src/RemoteAgent.Service/RemoteAgent.Service.csproj -c "$CONFIGURATION" --no-restore

echo "[dotnet10] building Android app"
dotnet build src/RemoteAgent.App/RemoteAgent.App.csproj -f net10.0-android -c "$CONFIGURATION" --no-restore

echo "[dotnet10] running unit tests"
dotnet test tests/RemoteAgent.App.Tests/RemoteAgent.App.Tests.csproj -c "$CONFIGURATION" --verbosity minimal -nologo
dotnet test tests/RemoteAgent.Service.Tests/RemoteAgent.Service.Tests.csproj -c "$CONFIGURATION" --verbosity minimal -nologo

popd >/dev/null
