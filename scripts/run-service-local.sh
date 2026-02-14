#!/usr/bin/env bash
# Run the Remote Agent service locally (no Docker).
# Uses the Development launch profile (http://0.0.0.0:5243).
# Override via env: Agent__Command, Agent__LogDirectory, Agent__DataDirectory, etc.
# Usage: ./scripts/run-service-local.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# Optional env defaults for local dev (appsettings.Development.json already sets Command=/bin/cat)
export Agent__Command="${Agent__Command:-/bin/cat}"
export Agent__LogDirectory="${Agent__LogDirectory:-$REPO_ROOT/logs}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

mkdir -p "$Agent__LogDirectory"

echo "Starting service at http://0.0.0.0:5243 (Agent__Command=$Agent__Command)"
exec dotnet run --project src/RemoteAgent.Service/RemoteAgent.Service.csproj --launch-profile http
