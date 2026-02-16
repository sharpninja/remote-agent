# GitHub Copilot Instructions for Remote Agent

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

### Warnings as Errors

- All warnings are treated as errors (see `Directory.Build.props`)
- Do not suppress warnings without explicit approval
- Fix the root cause of warnings, don't hide them
- See [docs/REPOSITORY_RULES.md](../docs/REPOSITORY_RULES.md) for details

### Build and Test

Before completing any task:

1. **Build the solution:**
   ```bash
   dotnet build RemoteAgent.slnx -c Release
   ```

2. **Run tests:**
   ```bash
   dotnet test RemoteAgent.slnx -c Release
   ```

3. **Verify changes work as expected**

### Framework

- The solution targets .NET 10 (net10.0)
- Android app uses MAUI with net10.0-android target
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
