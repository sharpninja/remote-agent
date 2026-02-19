# Remote Agent

![Remote Agent](docs/logo.svg) [![Build and Deploy](https://github.com/sharpninja/remote-agent/actions/workflows/build-deploy.yml/badge.svg)](https://github.com/sharpninja/remote-agent/actions/workflows/build-deploy.yml)

Android app (MAUI) and Avalonia desktop management app that communicate with a Linux gRPC service. The service spawns and manages configurable CLI agents (e.g. Cursor, Copilot, Ollama), bidirectionally streams messages, logs all interaction, and exposes HTTP management APIs.

**Documentation:** [sharpninja.github.io/remote-agent](https://sharpninja.github.io/remote-agent) — requirements, API reference, testing docs, and CLI agent guide.  
![QR: Documentation](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Fsharpninja.github.io%2Fremote-agent)

---

## Architecture

Three components form the system:

| Component | Technology | Description |
|-----------|-----------|-------------|
| **RemoteAgent.App** | MAUI Android (net10.0) | Chat client: connect/send/receive, notifications, multi-session management, media attachments, prompt templates |
| **RemoteAgent.Desktop** | Avalonia (net9.0) | Management UI: sessions, structured logs, operator panels, server registration, plugin and MCP management |
| **RemoteAgent.Service** | ASP.NET Core (net10.0) | gRPC + HTTP service: spawns agents, streams I/O, enforces session limits, exposes management APIs |

Shared libraries:

| Library | Description |
|---------|-------------|
| **RemoteAgent.Proto** | Protobuf contracts and generated gRPC C# (net10.0) |
| **RemoteAgent.App.Logic** | CQRS foundation, ViewModels, shared handlers (net10.0) |
| **RemoteAgent.Plugins.Ollama** | Ollama agent runner plugin (net10.0) |

### CQRS pattern

All user actions flow through a consistent pipeline:

- `IRequest<TResponse>` — every request carries a `Guid CorrelationId` generated at the UI command boundary
- `IRequestHandler<TRequest, TResponse>` — stateless handlers; business logic only, no cross-cutting concerns
- `ServiceProviderRequestDispatcher` — the sole cross-cutting point: Debug-level entry/exit logging with `[{CorrelationId}]`; throws `ArgumentException` on `Guid.Empty`

The desktop app has 32 handlers; the mobile app has 8 handlers; shared logic contributes 3 more.

### Desktop sub-VM decomposition

`ServerWorkspaceViewModel` owns six sub-ViewModels, each backed by CQRS handlers:

| Sub-VM | Responsibility |
|--------|---------------|
| `SecurityViewModel` | Peers, ban list, open sessions |
| `AuthUsersViewModel` | Auth users, permission roles |
| `PluginsViewModel` | Plugin assemblies and runner IDs |
| `McpRegistryDesktopViewModel` | MCP server registry and agent mappings |
| `PromptTemplatesViewModel` | Prompt templates and seed context |
| `StructuredLogsViewModel` | Log monitoring and filtering |

### Management App Log (FR-12.12)

The desktop captures its own `ILogger` output in real time:

- `AppLoggerProvider` + `InMemoryAppLogStore` — intercepts all desktop log entries (timestamp, level, category, message, exception)
- `AppLogViewModel` — observable collection with live filtering
- `ClearAppLogHandler` / `SaveAppLogHandler` — clear or export to `.txt`, `.json`, or `.csv` via `IFileSaveDialogService`

---

## Project structure

```text
src/
  RemoteAgent.Proto/              shared protobuf contracts + generated gRPC C# (net10.0)
  RemoteAgent.App.Logic/          CQRS foundation, interfaces, ViewModels, handlers (net10.0)
  RemoteAgent.App/                MAUI Android client (net10.0-android)
  RemoteAgent.Desktop/            Avalonia desktop management app (net9.0)
  RemoteAgent.Service/            ASP.NET Core gRPC + HTTP service (net10.0)
  RemoteAgent.Plugins.Ollama/     Ollama agent runner plugin (net10.0)
tests/
  RemoteAgent.App.Tests/          89 unit tests (net10.0)
  RemoteAgent.Desktop.UiTests/    151 unit tests (net9.0)
  RemoteAgent.Mobile.UiTests/     mobile UI tests
  RemoteAgent.Service.Tests/      service unit tests
  RemoteAgent.Service.IntegrationTests/  isolated integration tests
docs/
  functional-requirements.md      67 FRs (all Done)
  technical-requirements.md       112 TRs (all Done)
  requirements-completion-summary.md
  testing-strategy.md
  implementing-cli-agents.md      guide for writing custom CLI agent plugins
  SESSION-SEED-PROMPT.md          quick context seed for AI coding sessions
```

---

## Protocol (gRPC)

Contract: `Connect(stream ClientMessage) returns (stream ServerMessage)` in `AgentGateway.proto`.

**ClientMessage**

| Field | Type | Description |
|-------|------|-------------|
| `text` | string | User message forwarded to agent stdin |
| `control` | enum | `START` (spawn agent) or `STOP` (kill agent) |
| `script_request` | message | Run a bash or pwsh script; server returns stdout + stderr |
| `media_upload` | bytes | Image or video bytes attached as agent context |

**ServerMessage**

| Field | Type | Description |
|-------|------|-------------|
| `output` | string | Agent stdout line |
| `error` | string | Agent stderr line |
| `event` | enum | `SESSION_STARTED`, `SESSION_STOPPED`, `SESSION_ERROR` |
| `priority` | enum | `NORMAL`, `HIGH`, or `NOTIFY` (NOTIFY triggers a device system notification) |
| `mcp_update` | message | Notifies client of MCP server enable/disable for the active session |

Session logs are written under `Agent:LogDirectory` (default: system temp) as `remote-agent-{sessionId}.log`.

---

## Running the service

### Prerequisites

- .NET 10 SDK
- Linux or WSL (the service targets Linux; use Docker on Windows)

### Configure

Edit `src/RemoteAgent.Service/appsettings.json`:

```json
"Agent": {
  "Command": "/path/to/your/agent-or-script",
  "Arguments": "",
  "LogDirectory": "/var/log/remote-agent"
}
```

For a quick smoke test use `"/bin/cat"`. Set `RunnerId` to `process` (Linux default), `copilot-windows`, or a registered plugin ID.

### Start

```bash
dotnet run --project src/RemoteAgent.Service
```

The service listens on `http://0.0.0.0:5243` (gRPC over HTTP/2).

Override any setting with environment variables using `__` as the section separator:

```bash
Agent__Command=/usr/local/bin/cursor Agent__LogDirectory=/tmp/logs dotnet run --project src/RemoteAgent.Service
```

---

## Running with Docker

The CI pipeline publishes to **GitHub Container Registry**:

- **Image:** `ghcr.io/sharpninja/remote-agent/service:latest`
- **Package:** [github.com/.../remote-agent/service](https://github.com/sharpninja/remote-agent/pkgs/container/remote-agent%2Fservice)  
  ![QR: Docker image](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Fgithub.com%2Fsharpninja%2Fremote-agent%2Fpkgs%2Fcontainer%2Fremote-agent%252Fservice)

```bash
docker run -p 5243:5243 \
  -e Agent__Command=/path/to/agent \
  -e Agent__LogDirectory=/app/logs \
  -v /path/on/host/logs:/app/logs \
  ghcr.io/sharpninja/remote-agent/service:latest
```

Build locally:

```bash
docker build -t remote-agent-service .
docker run -p 5243:5243 -e Agent__Command=/bin/cat remote-agent-service
```

---

## Android app — install via F-Droid

### 1. Install F-Droid

Download from [f-droid.org](https://f-droid.org) and install on your Android device.  
![QR: F-Droid](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Ff-droid.org)

### 2. Add the Remote Agent repository

1. F-Droid → **Settings** → **Repositories** → **Add repository**
2. Enter URL: `https://sharpninja.github.io/remote-agent/repo`  
   ![QR: F-Droid repo](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Fsharpninja.github.io%2Fremote-agent%2Frepo)
3. Confirm, then pull-to-refresh to fetch the index.

### 3. Install

Search for **Remote Agent** in F-Droid and tap **Install**.

Direct APK download: `https://sharpninja.github.io/remote-agent/remote-agent.apk`  
![QR: Direct APK](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Fsharpninja.github.io%2Fremote-agent%2Fremote-agent.apk)

---

## Android app — build from source

```bash
dotnet build src/RemoteAgent.App -f net10.0-android
# Run on connected device or emulator:
dotnet build src/RemoteAgent.App -f net10.0-android -t:Run
```

| Device | Host | Port |
|--------|------|------|
| Android emulator | `10.0.2.2` | `5243` |
| Physical device (same LAN) | Linux machine IP | `5243` |

Tap **Connect** → the service spawns the configured agent and streams output. Enter text and tap **Send** to interact. Tap **Disconnect** to end the session.

---

## Desktop app — build from source

```bash
# Build and run (requires .NET 9 SDK):
dotnet run --project src/RemoteAgent.Desktop

# Or use the full build+test script:
./scripts/build-desktop-dotnet9.sh Release
```

---

## Tests

**240 tests, 0 failures.** Frameworks: xUnit + FluentAssertions + coverlet.

| Project | Tests | SDK | What is tested |
|---------|-------|-----|---------------|
| `RemoteAgent.App.Tests` | 89 | net10.0 | CQRS dispatcher, mobile handlers (8), MCP registry VM, prompt templates, markdown, chat messages, API client error handling |
| `RemoteAgent.Desktop.UiTests` | 151 | net9.0 | All 32 desktop handlers, AppLog view, StructuredLogStore, ConnectionSettings VM |

All test classes have `/// <summary>` XML documentation and `[Trait("Category","Requirements")]` attributes linking tests to FRs and TRs.

```bash
# Run unit tests:
dotnet test tests/RemoteAgent.App.Tests/
dotnet test tests/RemoteAgent.Desktop.UiTests/

# Run integration tests (isolated, not in default CI):
./scripts/test-integration.sh Release
```

Integration tests can also be triggered via the `integration-tests.yml` workflow using `workflow_dispatch`.

---

## Build scripts

This repo uses two SDK tracks — MAUI requires .NET 10 and the Avalonia desktop targets .NET 9:

| Script | SDK | Projects |
|--------|-----|----------|
| `./scripts/build-dotnet10.sh Release` | .NET 10 | MAUI app, service, shared libraries, App.Tests |
| `./scripts/build-desktop-dotnet9.sh Release` | .NET 9 | Avalonia desktop, Desktop.UiTests |
| `./scripts/test-integration.sh Release` | .NET 10 | Service integration tests (isolated) |

> **Important:** Do not pass `-q` (quiet) to `dotnet build` or `dotnet restore`. The .NET 10 SDK can silently fail with quiet mode. Use default verbosity or `-v m`.

All warnings are treated as errors (`TreatWarningsAsErrors=true` in `Directory.Build.props`). Fix root causes; do not suppress warnings with `#pragma warning disable` or `NoWarn`.

---

## CI/CD (GitHub Actions)

`build-deploy.yml` runs on push to `main` or via manual dispatch:

1. Builds .NET 10 stack (MAUI + service) and .NET 9 stack (desktop)
2. Builds Android APK
3. Pushes service Docker image to GHCR
4. Updates F-Droid-style APK repository and deploys to GitHub Pages
5. Generates DocFX documentation site and publishes to GitHub Pages

| Artifact | URL |
|----------|-----|
| APK (direct) | `https://sharpninja.github.io/remote-agent/remote-agent.apk` |
| F-Droid index | `https://sharpninja.github.io/remote-agent/repo` |
| Docker image | `ghcr.io/sharpninja/remote-agent/service:latest` |
| Documentation | `https://sharpninja.github.io/remote-agent` |

**Setup for forks:**
- Pages: Settings → Pages → Build and deployment → **GitHub Actions**
- Container registry: no extra setup — uses `GITHUB_TOKEN`

---

## Contributing

- All development happens on the **`develop`** branch; `main` is for production releases
- Create feature branches from `develop`: `git checkout -b feature/description develop`
- Target PRs to `develop`
- Sign commits (GPG or SSH required by branch protection)
- Run both build scripts (zero failures) before opening a PR
- See [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/REPOSITORY_RULES.md](docs/REPOSITORY_RULES.md)

---

## AI session seed

For quick startup context in AI coding sessions (GitHub Copilot, Cursor, Codex), see [`docs/SESSION-SEED-PROMPT.md`](docs/SESSION-SEED-PROMPT.md).

---

## Links

| | |
|-|-|
| Source code | [github.com/sharpninja/remote-agent](https://github.com/sharpninja/remote-agent) |
| Documentation | [sharpninja.github.io/remote-agent](https://sharpninja.github.io/remote-agent) |
| API reference | [sharpninja.github.io/remote-agent/api/](https://sharpninja.github.io/remote-agent/api/) |
| Testing API reference | [sharpninja.github.io/remote-agent/api-tests/](https://sharpninja.github.io/remote-agent/api-tests/) |
| Docker image | [ghcr.io/sharpninja/remote-agent/service](https://github.com/sharpninja/remote-agent/pkgs/container/remote-agent%2Fservice) |
| CLI agent guide | [docs/implementing-cli-agents.md](docs/implementing-cli-agents.md) |

![QR: GitHub repo](https://api.qrserver.com/v1/create-qr-code/?size=120x120&data=https%3A%2F%2Fgithub.com%2Fsharpninja%2Fremote-agent)
