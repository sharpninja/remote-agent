#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_PROJECT="$REPO_ROOT/src/RemoteAgent.App/RemoteAgent.App.csproj"

ANDROID_SDK_ROOT_DEFAULT="$HOME/Android/Sdk"
ANDROID_API_DEFAULT="35"
SYSTEM_IMAGE_DEFAULT="system-images;android-${ANDROID_API_DEFAULT};google_apis;x86_64"
AVD_NAME_DEFAULT="RemoteAgent_API_${ANDROID_API_DEFAULT}"
DEVICE_PROFILE_DEFAULT="pixel_6"
CONFIGURATION_DEFAULT="Debug"

BOOT_EMULATOR=0
CREATE_AVD=0
DEPLOY_APP=0
NO_SUDO=0
AVD_NAME="$AVD_NAME_DEFAULT"
SYSTEM_IMAGE="$SYSTEM_IMAGE_DEFAULT"
DEVICE_PROFILE="$DEVICE_PROFILE_DEFAULT"
CONFIGURATION="$CONFIGURATION_DEFAULT"

log() {
  echo "[android-fix] $*"
}

usage() {
  cat <<'EOF'
Usage: scripts/fix-adb-and-deploy.sh [options]

Repairs common adb startup issues (PATH/env, ownership, stale daemon, port conflicts).
Optionally creates/boots an emulator and deploys the MAUI Android app.

Options:
  --create-avd                Create AVD if missing (requires cmdline-tools).
  --boot                      Boot emulator (GUI) and wait for Android boot complete.
  --deploy                    Build + install app to connected emulator/device.
  --avd-name <name>           AVD name (default: RemoteAgent_API_35).
  --system-image <id>         System image id (default: system-images;android-35;google_apis;x86_64).
  --device-profile <id>       avdmanager device profile (default: pixel_6).
  --configuration <cfg>       Build configuration (default: Debug).
  --no-sudo                   Skip ownership fix (for environments without sudo).
  -h, --help                  Show this help.

Examples:
  scripts/fix-adb-and-deploy.sh
  scripts/fix-adb-and-deploy.sh --create-avd --boot
  scripts/fix-adb-and-deploy.sh --boot --deploy --configuration Release
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --create-avd)
      CREATE_AVD=1
      shift
      ;;
    --boot)
      BOOT_EMULATOR=1
      shift
      ;;
    --deploy)
      DEPLOY_APP=1
      shift
      ;;
    --avd-name)
      AVD_NAME="${2:-}"
      shift 2
      ;;
    --system-image)
      SYSTEM_IMAGE="${2:-}"
      shift 2
      ;;
    --device-profile)
      DEVICE_PROFILE="${2:-}"
      shift 2
      ;;
    --configuration)
      CONFIGURATION="${2:-}"
      shift 2
      ;;
    --no-sudo)
      NO_SUDO=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      usage
      exit 1
      ;;
  esac
done

export ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-$ANDROID_SDK_ROOT_DEFAULT}"
export ANDROID_HOME="${ANDROID_HOME:-$ANDROID_SDK_ROOT}"
export PATH="$ANDROID_SDK_ROOT/platform-tools:$ANDROID_SDK_ROOT/emulator:$ANDROID_SDK_ROOT/cmdline-tools/latest/bin:$PATH"

if [[ -z "${JAVA_HOME:-}" ]]; then
  if [[ -d "/usr/lib/jvm/java-21-openjdk-amd64" ]]; then
    export JAVA_HOME="/usr/lib/jvm/java-21-openjdk-amd64"
  elif [[ -d "/usr/lib/jvm/default-java" ]]; then
    export JAVA_HOME="/usr/lib/jvm/default-java"
  fi
fi

ADB="$ANDROID_SDK_ROOT/platform-tools/adb"
EMULATOR="$ANDROID_SDK_ROOT/emulator/emulator"
AVDMANAGER="$ANDROID_SDK_ROOT/cmdline-tools/latest/bin/avdmanager"
SDKMANAGER="$ANDROID_SDK_ROOT/cmdline-tools/latest/bin/sdkmanager"

if [[ ! -x "$ADB" ]]; then
  echo "adb not found at $ADB"
  exit 1
fi

if [[ "$NO_SUDO" -eq 0 && -x "$(command -v sudo || true)" ]]; then
  log "Fixing ownership for Android SDK and ~/.android"
  sudo chown -R "$USER:$USER" "$HOME/.android" "$ANDROID_SDK_ROOT" 2>/dev/null || true
  chmod -R u+rwX "$HOME/.android" "$ANDROID_SDK_ROOT" 2>/dev/null || true
else
  log "Skipping ownership fix (--no-sudo set or sudo unavailable)"
fi

log "Stopping stale adb processes"
pkill -f "adb" 2>/dev/null || true
"$ADB" kill-server 2>/dev/null || true

if command -v lsof >/dev/null 2>&1; then
  CONFLICT_PIDS="$(lsof -ti tcp:5037 2>/dev/null || true)"
  if [[ -n "$CONFLICT_PIDS" ]]; then
    log "Killing processes currently bound to tcp:5037: $CONFLICT_PIDS"
    kill $CONFLICT_PIDS 2>/dev/null || true
  fi
fi

log "Starting adb server"
"$ADB" start-server
"$ADB" devices -l

if [[ "$CREATE_AVD" -eq 1 || "$BOOT_EMULATOR" -eq 1 ]]; then
  if [[ ! -x "$AVDMANAGER" ]] || [[ ! -x "$SDKMANAGER" ]]; then
    echo "cmdline-tools not found under $ANDROID_SDK_ROOT/cmdline-tools/latest/bin"
    exit 1
  fi

  if [[ "$CREATE_AVD" -eq 1 ]]; then
    if ! "$EMULATOR" -list-avds | grep -Fxq "$AVD_NAME"; then
      log "Installing system image: $SYSTEM_IMAGE"
      "$SDKMANAGER" "$SYSTEM_IMAGE"
      log "Creating AVD: $AVD_NAME"
      echo "no" | "$AVDMANAGER" create avd --force --name "$AVD_NAME" --package "$SYSTEM_IMAGE" --device "$DEVICE_PROFILE"
    else
      log "AVD already exists: $AVD_NAME"
    fi
  fi
fi

if [[ "$BOOT_EMULATOR" -eq 1 ]]; then
  if ! "$EMULATOR" -list-avds | grep -Fxq "$AVD_NAME"; then
    echo "AVD '$AVD_NAME' does not exist. Re-run with --create-avd."
    exit 1
  fi

  log "Booting emulator: $AVD_NAME"
  nohup "$EMULATOR" "@$AVD_NAME" >/tmp/remote-agent-emulator.log 2>&1 &
  sleep 2
  "$ADB" wait-for-device

  log "Waiting for boot completion"
  for _ in $(seq 1 180); do
    BOOT_OK="$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r' || true)"
    if [[ "$BOOT_OK" == "1" ]]; then
      break
    fi
    sleep 2
  done

  "$ADB" devices -l
  log "Emulator boot complete (or timed out after wait window)."
fi

if [[ "$DEPLOY_APP" -eq 1 ]]; then
  log "Building + installing app ($CONFIGURATION)"
  dotnet build "$APP_PROJECT" -f net10.0-android -c "$CONFIGURATION" -t:Install -v minimal
  log "Launching app"
  "$ADB" shell monkey -p com.companyname.remoteagent.app -c android.intent.category.LAUNCHER 1 >/dev/null
fi

log "Done"
