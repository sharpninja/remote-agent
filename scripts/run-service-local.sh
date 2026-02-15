#!/usr/bin/env bash
# Run the Remote Agent service locally (no Docker).
# Stops the Docker container or kills a local process using port 5243 if necessary.
# Uses the Development launch profile (http://0.0.0.0:5243).
# Override via env: Agent__Command, Agent__LogDirectory, Agent__DataDirectory, etc.
# Usage: ./scripts/run-service-local.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

CONTAINER_NAME="${CONTAINER_NAME:-remote-agent-service}"
PORT=5243

# Free port 5243: stop Docker container if running, then kill any local process on the port
if docker ps -q -f name="^${CONTAINER_NAME}$" 2>/dev/null | grep -q .; then
  echo "Stopping Docker container: $CONTAINER_NAME"
  docker stop "$CONTAINER_NAME" 2>/dev/null || true
  sleep 1
fi

PIDS=""
if command -v lsof >/dev/null 2>&1; then
  PIDS=$(lsof -ti ":$PORT" 2>/dev/null) || true
elif command -v fuser >/dev/null 2>&1; then
  PIDS=$(fuser "$PORT/tcp" 2>/dev/null) || true
fi
if [ -n "$PIDS" ]; then
  echo "Stopping process(es) on port $PORT: $PIDS"
  kill $PIDS 2>/dev/null || true
  sleep 1
  # Force kill if still alive
  if command -v lsof >/dev/null 2>&1; then
    PIDS=$(lsof -ti ":$PORT" 2>/dev/null) || true
  fi
  [ -n "$PIDS" ] && kill -9 $PIDS 2>/dev/null || true
  sleep 1
fi

# Agent command: use env if set; otherwise use `agent` from PATH if present; else /bin/cat
if [ -z "${Agent__Command:-}" ]; then
  AGENT_PATH="$(command -v agent 2>/dev/null)" || true
  export Agent__Command="${AGENT_PATH:-/bin/cat}"
fi
export Agent__LogDirectory="${Agent__LogDirectory:-$REPO_ROOT/logs}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

mkdir -p "$Agent__LogDirectory"

echo "=== Building service and service tests ==="
dotnet build tests/RemoteAgent.Service.Tests/RemoteAgent.Service.Tests.csproj -nologo
echo "=== Running service tests ==="
dotnet test tests/RemoteAgent.Service.Tests/RemoteAgent.Service.Tests.csproj -nologo --no-build
echo "=== Starting service at http://0.0.0.0:$PORT (Agent__Command=$Agent__Command) ==="
exec dotnet run --project src/RemoteAgent.Service/RemoteAgent.Service.csproj --launch-profile http --no-build
