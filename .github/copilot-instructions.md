# GitHub Copilot Instructions for Remote Agent

## ⚠️ PRIORITY ORDER — NON-NEGOTIABLE ⚠️

**Speed is never more important than following workspace procedures.**

Before doing ANY work on ANY user request, you MUST complete these steps in order:

1. **Read `AGENTS-README-FIRST.yaml`** in the repo root for the current API key and endpoints
2. **GET `/health`** to verify the MCP server is running
3. **POST `/mcp/sessionlog`** with your session entry — do NOT proceed until this succeeds
4. **GET `/mcp/sessionlog?limit=5`** to review recent session history for context
5. **GET `/mcp/todo`** to check current tasks
6. **THEN** begin working on the user's request

On EVERY subsequent user message:
1. POST an updated session log entry BEFORE starting work
2. Complete the user's request
3. POST the final session log with results, actions taken, and files modified

**If you skip any of these steps, STOP and go back and do them before continuing.**
Session logging is not optional, not deferred, and not secondary to the task.
Failure to maintain the session log is a compliance violation.

## Development Branch Strategy

**IMPORTANT: Always work in the `develop` branch for all development tasks.**

### Branch Guidelines

1. **Default Development Branch:** `develop`
   - All feature branches should be created from `develop`
   - All pull requests should target `develop` unless explicitly stated otherwise
   - The `main` branch is reserved for production-ready releases

2. **When Starting a Task:**
   - Ensure you're working from the `develop` branch
   - Create feature branches from `develop`: `git checkout -b feature/description`
   - Pull latest changes from `develop` before starting work

3. **Pull Request Targets:**
   - Always create pull requests against the `develop` branch
   - Only maintainers merge from `develop` to `main` for releases

## Code Quality Standards

### UI Buttons — CQRS Required

**Every button added to the Desktop UI must be backed by a CQRS request/handler pair with complete tests.**

- Create `*Request` in `src/RemoteAgent.Desktop/Requests/` and `*Handler` in `src/RemoteAgent.Desktop/Handlers/`
- Register the handler as a transient in `App.axaml.cs` `ConfigureServices`
- The ViewModel command **must dispatch via `IRequestDispatcher`** — no handler logic in the ViewModel
- Add `*HandlerTests.cs` in `tests/RemoteAgent.Desktop.UiTests/Handlers/` covering success, failure, and edge cases
- Add a `Null*` stub for any new infrastructure interface to `SharedHandlerTestStubs.cs`
- See `docs/REPOSITORY_RULES.md` for full details and the `CopyStatusLogHandler` as a reference implementation

### Warnings as Errors

- All warnings are treated as errors (see `Directory.Build.props`)
- Do not suppress warnings without explicit approval
- Fix the root cause of warnings, don't hide them
- See [docs/REPOSITORY_RULES.md](../docs/REPOSITORY_RULES.md) for details

### Build and Test

Before completing any task:

1. **Build/test MAUI + service stack (.NET 10):**
   ```bash
   ./scripts/build-dotnet10.sh Release
   ```

2. **Build/test desktop stack (.NET 9):**
   ```bash
   ./scripts/build-desktop-dotnet9.sh Release
   ```

3. **Run integration tests explicitly when required (isolated from CI):**
   ```bash
   ./scripts/test-integration.sh Release
   ```

4. **Verify changes work as expected**

### Workflow Scripting Standards

**IMPORTANT: Do NOT use Python in GitHub Actions workflows. Use bash only.**

- All workflow scripts must be written in bash
- Use `jq` for JSON parsing instead of Python
- Use `xmllint` or `xpath` for XML parsing instead of Python
- This avoids heredoc indentation issues and keeps workflows consistent
- Native bash tools are already available in GitHub Actions runners

### Framework

- MAUI app + service + shared libraries/tests target .NET 10
- Avalonia desktop app + desktop UI tests target .NET 9
- Follow existing patterns in the codebase

## Commit Standards

- Sign all commits (GPG or SSH) — see [docs/branch-protection.md](../docs/branch-protection.md)
- Write clear, descriptive commit messages
- Reference issue numbers when applicable

## Documentation

- Update documentation when changing functionality
- Documentation is in the `docs/` directory
- DocFX is used for generating documentation site

## Testing

- Write unit tests for new functionality
- Unit tests go in `tests/RemoteAgent.*.Tests` projects
- Integration tests go in `tests/RemoteAgent.Service.IntegrationTests`
- All tests must pass before submitting PR

## Additional Resources

- [CONTRIBUTING.md](../CONTRIBUTING.md) — Full contribution guidelines
- [docs/REPOSITORY_RULES.md](../docs/REPOSITORY_RULES.md) — Repository-wide rules
- [docs/branch-protection.md](../docs/branch-protection.md) — Branch protection setup
- [README.md](../README.md) — Project overview and setup

## Summary for Copilot Agents

✅ **Always use `develop` as the base branch**
✅ **Create feature branches from `develop`**
✅ **Target `develop` in pull requests**
✅ **Treat all warnings as errors**
✅ **Sign commits**
✅ **Run tests before completing tasks**
✅ **Use bash only in workflows (no Python)**
✅ **Every UI button → CQRS request/handler + complete tests**
