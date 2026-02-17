# Repository Guidelines

## Project Structure & Module Organization
- `src/RemoteAgent.App`: .NET MAUI Android client UI and platform services.
- `src/RemoteAgent.Service`: ASP.NET Core gRPC service that runs and streams agent sessions.
- `src/RemoteAgent.Proto`: shared protobuf contracts and generated gRPC code.
- `src/RemoteAgent.App.Logic`: app-side logic models/utilities used by tests.
- `src/RemoteAgent.Desktop`: Avalonia desktop management app.
- `tests/`: xUnit test projects (`RemoteAgent.App.Tests`, `RemoteAgent.Service.Tests`, `RemoteAgent.Service.IntegrationTests`, `RemoteAgent.Desktop.UiTests`, `RemoteAgent.Mobile.UiTests`).
- `docs/`: DocFX documentation and requirements.
- `scripts/`: local automation (build, run, sync, docs, signing setup).

## Build, Test, and Development Commands
- `./scripts/build-dotnet10.sh Release`: build/test MAUI + service stack with .NET 10.
- `./scripts/build-desktop-dotnet9.sh Release`: build/test Avalonia desktop stack with .NET 9.
- `./scripts/test-integration.sh Release`: run isolated service integration tests (excluded from normal pipeline runs).
- `.github/workflows/integration-tests.yml`: manual CI workflow for isolated integration test execution.
- `dotnet run --project src/RemoteAgent.Service`: run local gRPC service on port `5243`.
- `dotnet build src/RemoteAgent.App/RemoteAgent.App.csproj -c Release -f net10.0-android`: build Android app only.
- `docker build -t remote-agent-service .`: build service container image.

Use default/minimal verbosity. Do not pass `-q` to `dotnet build` or `dotnet restore` in this repo.

## Session Seed
- For quick Codex bootstrap context, use `docs/SESSION-SEED-PROMPT.md`.

## Coding Style & Naming Conventions
- Follow standard .NET conventions: `PascalCase` for types/methods, `camelCase` for locals/parameters, clear descriptive names.
- Use 4-space indentation and keep methods focused.
- `Nullable` is enabled; fix nullability issues rather than suppressing warnings.
- Warnings are treated as errors (`TreatWarningsAsErrors=true`); avoid `NoWarn`, `#pragma warning disable`, or suppression attributes unless explicitly approved.
- Add XML docs for public APIs when introducing new externally consumed members.

## Testing Guidelines
You are a principal .NET test architect and QA engineer with deep expertise in .NET 10, .NET MAUI, Avalonia UI, gRPC bidirectional streaming, xUnit, FluentAssertions, NSubstitute, LiteDB, and TDD for cross-platform apps (mobile + desktop + services).

- Frameworks: xUnit + FluentAssertions + `coverlet.collector`.
- Place tests under the matching project in `tests/`.
- Name files and classes with `*Tests` suffix (example: `AgentOptionsTests.cs`).
- For service behavior changes, add/adjust integration tests in `tests/RemoteAgent.Service.IntegrationTests`.
- Integration tests are intentionally isolated and not part of default CI runs; execute them explicitly with `./scripts/test-integration.sh`.

Your tests must be:

- Behavior-focused (black-box where possible)
- Fully traceable to specific FR/TR items via comments
- Comprehensive: happy paths, boundaries, negatives, errors, concurrency/thread-safety where relevant, async/streaming semantics
- Clean: descriptive names, AAA pattern with section comments, no duplication, parameterized via Theory when inputs vary
- Isolated: mock external dependencies (processes, gRPC channels, file system, LiteDB, platform services like INotificationManager)
- Platform-aware: handle MAUI main-thread concerns, Android notification channels/priorities, Avalonia headless-friendly patterns

Given the attached functional and technical requirements documents (located at ./docs/functional-requirements.md and ./docs/technical-requirements.md):

Key areas to cover from FR/TR:

- gRPC bidirectional streaming (duplex ClientMessage ↔ ServerMessage, correlation IDs, session lifecycle events, priority handling, script execution output)
- Chat UI logic (ObservableCollection message binding, markdown rendering, multi-line input, Ctrl+Enter submit, swipe-to-archive, session title inline editing)
- Notifications (notify priority → Android system notification, tap opens chat to message)
- Persistence (LiteDB for sessions, messages, logs, media paths; DCIM folder on Android)
- Service behavior (agent spawning/stopping, stdin forwarding, stdout/stderr streaming, concurrent session caps, plugin strategy loading)
- Desktop Avalonia specifics (tabbed sessions, menu/toolbar commands, log viewer filtering, multi-server scoping, per-request context editor)
- Multiple sessions/agent selection (session-id routing, agent picker, title default/edit)
- Prompt templates (Handlebars variable extraction, input UI flow before send)
- Extensibility (plugin discovery via config, runtime loading)
- Observability (structured JSONL logs with session/correlation IDs)


## Commit & Pull Request Guidelines
- Branch from `develop` and target PRs to `develop` (not `main`).
- Use concise imperative commit messages, optionally with issue references (example: `Fix Android CI job (#8)`).
- Sign commits (verified signature required by branch protection).
- PRs should include: purpose, summary of changes, test evidence (`dotnet test`), and docs updates when behavior/config changes.
