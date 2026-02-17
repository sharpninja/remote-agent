#!/usr/bin/env bash
# Generate C# code from AgentGateway.proto using protoc and grpc_csharp_plugin.
# Run in WSL (or Linux). Uses Grpc.Tools 2.64.0 from the NuGet package cache.
# Generated files go to src/RemoteAgent.Proto/Generated/ so the .NET build
# can use them without Grpc.Tools (avoids Windows UNC path issues).
# Usage: ./scripts/generate-proto.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROTO_DIR="$REPO_ROOT/src/RemoteAgent.Proto"
OUT_DIR="$PROTO_DIR/Generated"
PROTO_FILE="AgentGateway.proto"

# Ensure Grpc.Tools is restored so we have protoc and grpc_csharp_plugin
# Use -v m (minimal); -q can cause "Question build FAILED" with .NET 10 SDK.
dotnet restore "$PROTO_DIR/RemoteAgent.Proto.csproj" -nologo -v m

# Grpc.Tools 2.64.0: tools for Linux
NUGET_PACKAGES="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
TOOLS_DIR="$NUGET_PACKAGES/grpc.tools/2.64.0/tools/linux_x64"
PROTOC="$TOOLS_DIR/protoc"
PLUGIN="$TOOLS_DIR/grpc_csharp_plugin"

if [[ ! -x "$PROTOC" ]] || [[ ! -x "$PLUGIN" ]]; then
  echo "Missing $PROTOC or $PLUGIN. Run: dotnet restore $PROTO_DIR/RemoteAgent.Proto.csproj" >&2
  exit 1
fi

mkdir -p "$OUT_DIR"
"$PROTOC" \
  --csharp_out="$OUT_DIR" \
  --grpc_out="$OUT_DIR" \
  --plugin=protoc-gen-grpc="$PLUGIN" \
  -I"$PROTO_DIR" \
  "$PROTO_DIR/$PROTO_FILE"

echo "Generated $OUT_DIR/AgentGateway.cs and $OUT_DIR/AgentGatewayGrpc.cs"
