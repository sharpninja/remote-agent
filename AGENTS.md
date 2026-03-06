# Repository Guidelines

## ⚠️ PRIORITY ORDER — NON-NEGOTIABLE ⚠️

**Speed is never more important than following workspace procedures.**

Before doing ANY work on ANY user request, you MUST complete these steps in order:

1. **Read `AGENTS-README-FIRST.yaml`** in the repo root for the current API key and endpoints
2. **GET `/health`** to verify the MCP server is running
3. **POST `/mcp/sessionlog`** with your session turn — do NOT proceed until this succeeds
4. **GET `/mcp/sessionlog?limit=5`** to review recent session history for context
5. **GET `/mcp/todo`** to check current tasks
6. **THEN** begin working on the user's request

On EVERY subsequent user message:
1. Post a new session log turn (`Add-McpSessionTurn`) before starting work.
2. Complete the user's request.
3. Update the turn with results (`Response`) and actions (`Add-McpAction`) when done.

**If you skip any of these steps, STOP and go back and do them before continuing.**
Session logging is not optional, not deferred, and not secondary to the task.
Failure to maintain the session log is a compliance violation.

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
- Frameworks: xUnit + FluentAssertions + `coverlet.collector`.
- Place tests under the matching project in `tests/`.
- Name files and classes with `*Tests` suffix (example: `AgentOptionsTests.cs`).
- For service behavior changes, add/adjust integration tests in `tests/RemoteAgent.Service.IntegrationTests`.
- Integration tests are intentionally isolated and not part of default CI runs; execute them explicitly with `./scripts/test-integration.sh`.

## Commit & Pull Request Guidelines
- Branch from `develop` and target PRs to `develop` (not `main`).
- Use concise imperative commit messages, optionally with issue references (example: `Fix Android CI job (#8)`).
- Sign commits (verified signature required by branch protection).
- PRs should include: purpose, summary of changes, test evidence (`dotnet test`), and docs updates when behavior/config changes.

