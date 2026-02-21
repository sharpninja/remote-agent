#!/usr/bin/env bash
# remote-agent-ctl — Start, stop, restart, or check the Remote Agent gRPC service.
#
# Works with both systemd and non-systemd environments (WSL, Pengwin, etc.).
# When systemd is active, delegates to systemctl. Otherwise uses start-stop-daemon
# directly, tracking the process via /run/remote-agent.pid.
#
# Usage:
#   remote-agent-ctl {start|stop|restart|status}

set -euo pipefail

SVC_NAME="remote-agent.service"
SVC_BIN="/usr/lib/remote-agent/service/RemoteAgent.Service"
SVC_USER="remote-agent"
PID_FILE="/run/remote-agent.pid"
LOG_FILE="/var/log/remote-agent/service.log"
ENV_FILE="/etc/remote-agent/environment"

# ── Helpers ───────────────────────────────────────────────────────────────────

_has_systemd() {
  [ -d /run/systemd/system ]
}

_source_env() {
  if [ -f "$ENV_FILE" ]; then
    set -a
    # shellcheck disable=SC1090
    . "$ENV_FILE" 2>/dev/null || true
    set +a
  fi
  export ASPNETCORE_CONTENTROOT=/etc/remote-agent
}

_is_running() {
  # Check PID file first.
  if [ -f "$PID_FILE" ]; then
    _pid=$(cat "$PID_FILE" 2>/dev/null || true)
    if [ -n "$_pid" ] && kill -0 "$_pid" 2>/dev/null; then
      return 0
    fi
  fi
  # Fallback: check if the binary is already running (e.g. started at boot
  # outside of this script, so no PID file was written by us).
  if pgrep -x "$(basename "$SVC_BIN")" > /dev/null 2>&1; then
    # Capture the PID and update the PID file so future calls use it.
    _live_pid=$(pgrep -x "$(basename "$SVC_BIN")" | head -1)
    echo "$_live_pid" | sudo tee "$PID_FILE" > /dev/null 2>/dev/null || true
    return 0
  fi
  return 1
}

# ── Commands ──────────────────────────────────────────────────────────────────

_start() {
  if _has_systemd; then
    systemctl start "$SVC_NAME"
    return
  fi

  if _is_running; then
    echo "remote-agent is already running (pid $(cat "$PID_FILE" 2>/dev/null || pgrep -x "$(basename "$SVC_BIN")" | head -1))."
    return 0
  fi

  if [ ! -x "$SVC_BIN" ]; then
    echo "ERROR: service binary not found: $SVC_BIN" >&2
    exit 1
  fi

  _source_env
  mkdir -p "$(dirname "$LOG_FILE")"

  echo "Starting remote-agent via start-stop-daemon..."
  start-stop-daemon --start --background \
    --make-pidfile --pidfile "$PID_FILE" \
    --chuid "$SVC_USER" \
    --output "$LOG_FILE" \
    --exec "$SVC_BIN"

  # Give the process a moment to start.
  sleep 1

  if _is_running; then
    echo "remote-agent started (pid $(cat "$PID_FILE"))."
  else
    echo "ERROR: remote-agent failed to start. Check $LOG_FILE" >&2
    exit 1
  fi
}

_stop() {
  if _has_systemd; then
    systemctl stop "$SVC_NAME"
    return
  fi

  if ! _is_running; then
    echo "remote-agent is not running."
    return 0
  fi

  echo "Stopping remote-agent..."
  start-stop-daemon --stop --retry 5 \
    --pidfile "$PID_FILE" \
    --exec "$SVC_BIN" 2>/dev/null || true
  rm -f "$PID_FILE"
  echo "remote-agent stopped."
}

_restart() {
  if _has_systemd; then
    systemctl restart "$SVC_NAME"
    return
  fi

  _stop
  sleep 1
  _start
}

_status() {
  if _has_systemd; then
    systemctl status "$SVC_NAME"
    return
  fi

  if _is_running; then
    echo "remote-agent is running (pid $(cat "$PID_FILE"))."
  else
    echo "remote-agent is not running."
    exit 1
  fi
}

# ── Dispatch ──────────────────────────────────────────────────────────────────

case "${1:-}" in
  start)   _start   ;;
  stop)    _stop    ;;
  restart) _restart ;;
  status)  _status  ;;
  *)
    echo "Usage: $(basename "$0") {start|stop|restart|status}" >&2
    exit 1
    ;;
esac
