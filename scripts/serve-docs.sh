#!/usr/bin/env bash
# Build the DocFX site and serve it locally.
# Kills any process already using the chosen port before serving.
# Usage: ./scripts/serve-docs.sh [port]
#   port  Optional. Default: 8880.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DOCS_DIR="$REPO_ROOT/docs"
PORT="${1:-8880}"

# Free the port: kill any process listening on it
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
  if command -v lsof >/dev/null 2>&1; then
    PIDS=$(lsof -ti ":$PORT" 2>/dev/null) || true
  fi
  [ -n "$PIDS" ] && kill -9 $PIDS 2>/dev/null || true
  sleep 1
fi

cd "$DOCS_DIR"
docfx build

docfx serve _site --port "$PORT" &
SERVER_PID=$!

# Wait for server to bind then open default browser
sleep 2
URL="http://localhost:$PORT"
if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$URL"
elif command -v open >/dev/null 2>&1; then
  open "$URL"
else
  echo "Docs at $URL (open in your browser)"
fi

wait $SERVER_PID
