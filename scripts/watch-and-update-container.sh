#!/usr/bin/env bash
# Monitor the latest (or given) GitHub Actions run; on success, pull the new
# image and restart the local Docker container so Docker Desktop runs the updated service.
# Requires: gh (GitHub CLI) logged in, docker.
# Usage: ./scripts/watch-and-update-container.sh [run_id]
#   run_id  Optional. Default: latest run of Build and Deploy workflow.
#   INSTALL_ONLY=1  Pull image only; do not stop/remove/start the container.

set -euo pipefail

CONTAINER_NAME="${CONTAINER_NAME:-remote-agent-service}"
IMAGE="${IMAGE:-ghcr.io/sharpninja/remote-agent/service:latest}"
WORKFLOW_NAME="${WORKFLOW_NAME:-build-deploy.yml}"

RUN_ID="${1:-}"
if [ -z "$RUN_ID" ]; then
  echo "Fetching latest run for workflow: $WORKFLOW_NAME"
  RUN_ID=$(gh run list --workflow="$WORKFLOW_NAME" --limit 1 --json databaseId --jq '.[0].databaseId')
  if [ -z "$RUN_ID" ] || [ "$RUN_ID" = "null" ]; then
    echo "No runs found." >&2
    exit 1
  fi
  echo "Watching run ID: $RUN_ID"
fi

echo "Waiting for run to complete..."
gh run watch "$RUN_ID" || true

STATUS=$(gh run view "$RUN_ID" --json conclusion --jq '.conclusion')
if [ "$STATUS" != "success" ]; then
  echo "Run did not succeed (conclusion: $STATUS). Skipping container update."
  exit 1
fi

echo "Run succeeded. Pulling image..."
docker pull "$IMAGE"

if [ -n "${INSTALL_ONLY:-}" ]; then
  echo "Done. Image installed (container not started)."
  exit 0
fi

if docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
  echo "Stopping and removing existing container: $CONTAINER_NAME"
  docker stop "$CONTAINER_NAME" 2>/dev/null || true
  docker rm "$CONTAINER_NAME" 2>/dev/null || true
fi

echo "Starting container: $CONTAINER_NAME"
docker run -d --name "$CONTAINER_NAME" -p 5243:5243 \
  -e Agent__Command="${Agent__Command:-/bin/cat}" \
  -e Agent__LogDirectory=/app/logs \
  "$IMAGE"

echo "Done. Container $CONTAINER_NAME is running $IMAGE."
