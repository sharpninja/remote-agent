#!/usr/bin/env bash
# Wrapper to launch an agent process for integration tests. Logs the full command line
# before execution and when the process exits so test output is visible.
# Usage: run-agent-wrapper.sh <agent_path> [args...]
# The service passes Agent:Arguments as a single string; we execute it (e.g. "/bin/cat" or "sleep 5").

set -euo pipefail

if [ $# -eq 0 ]; then
  echo "run-agent-wrapper.sh: missing agent command (pass agent path as argument)" >&2
  exit 1
fi

# Log the full command we are about to execute (all args as one command line)
echo "[run-agent-wrapper] Executing: $*"

# Run the command; stdout/stderr stay connected to the caller (service redirects them).
eval "$*"
exitcode=$?

echo "[run-agent-wrapper] Command exited with code $exitcode"
exit $exitcode
