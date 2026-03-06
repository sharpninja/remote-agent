# Contributing to Remote Agent

Thank you for your interest in contributing to Remote Agent! This document provides guidelines for contributing to the project.

## Development Branch Workflow

**Important:** All development work should be based on the `develop` branch, not `main`.

### Branch Strategy

- **`main`** — Production-ready code. Protected branch. All changes must go through pull requests.
- **`develop`** — Main development branch. All feature branches should be created from and merged back into `develop`.
- **Feature branches** — Create feature branches from `develop` using descriptive names (e.g., `feature/add-authentication`, `fix/connection-timeout`).

### Workflow for Contributing

1. **Fork the repository** (if you're not a direct collaborator).

2. **Clone your fork** and add the upstream remote:
   ```bash
   git clone https://github.com/YOUR_USERNAME/remote-agent.git
   cd remote-agent
   git remote add upstream https://github.com/sharpninja/remote-agent.git
   ```

3. **Create a feature branch from `develop`:**
   ```bash
   git checkout develop
   git pull upstream develop
   git checkout -b feature/your-feature-name
   ```

4. **Make your changes** following the coding standards below.

5. **Test your changes:**
   ```bash
   ./scripts/build-dotnet10.sh Release
   ./scripts/build-desktop-dotnet9.sh Release
   ```

6. **Commit your changes** with clear, descriptive commit messages:
   ```bash
   git add .
   git commit -m "Add feature: description of your change"
   ```
   
   Note: All commits must be signed. See [branch-protection.md](docs/branch-protection.md) for setup instructions.

7. **Push to your fork:**
   ```bash
   git push origin feature/your-feature-name
   ```

8. **Create a Pull Request** from your feature branch to the `develop` branch of the upstream repository.

## Coding Standards

### Warnings as Errors

All warnings are treated as errors. See [REPOSITORY_RULES.md](docs/REPOSITORY_RULES.md) for details.

- Do not suppress warnings without explicit approval.
- Fix the underlying issue that causes the warning.
- The build fails on any warning.

### Code Style

- Follow standard .NET coding conventions.
- Use meaningful variable and method names.
- Add XML documentation comments for public APIs.
- Keep methods focused and concise.

### Testing

- Write unit tests for new functionality.
- Ensure all tests pass before submitting a PR.
- Integration tests should go in `RemoteAgent.Service.IntegrationTests`.

### Requirements Traceability

All test classes and methods should be annotated with the functional (FR) and technical (TR) requirements they cover:

```csharp
/// <summary>Tests for authentication. FR-13.5; TR-18.1, TR-18.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
public class AuthUserServiceTests
{
    [Fact]
    public void UpsertListDelete_ShouldPersistUser()
    {
        // Test implementation
    }
}
```

After adding or updating requirement annotations, regenerate the traceability matrix:

```bash
./scripts/generate-requirements-matrix.sh
```

This updates `docs/requirements-test-coverage.md`, which maps all requirements to their test coverage.

## Building and Testing

### Prerequisites

- .NET 10 SDK (MAUI app + service + shared libs/tests)
- .NET 9 SDK (Avalonia desktop app + desktop UI tests)
- For Android development: Android SDK and MAUI workload

### Build

```bash
./scripts/build-dotnet10.sh Release
./scripts/build-desktop-dotnet9.sh Release
```

### Test

```bash
dotnet test tests/RemoteAgent.App.Tests/RemoteAgent.App.Tests.csproj -c Release
dotnet test tests/RemoteAgent.Service.Tests/RemoteAgent.Service.Tests.csproj -c Release
dotnet test tests/RemoteAgent.Desktop.UiTests/RemoteAgent.Desktop.UiTests.csproj -c Release
```

### Integration Tests (isolated)

Integration tests are intentionally isolated and excluded from default pipeline runs. Execute them explicitly when needed:

```bash
./scripts/test-integration.sh Release
```

CI runs integration tests only through the manual `integration-tests.yml` workflow.

### Run the Service

```bash
dotnet run --project src/RemoteAgent.Service
```

## Pull Request Guidelines

- **Base branch:** Always target the `develop` branch unless explicitly coordinating a hotfix to `main`.
- **Title:** Use a clear, descriptive title.
- **Description:** Explain what changes were made and why.
- **Tests:** Include tests for new features or bug fixes.
- **Documentation:** Update documentation if you're changing functionality.

## Code Review Process

1. A maintainer will review your PR.
2. Address any feedback or requested changes.
3. Once approved, your PR will be merged into `develop`.
4. Periodically, `develop` will be merged into `main` for releases.

## Questions or Issues?

If you have questions or run into issues:

- Check existing [Issues](https://github.com/sharpninja/remote-agent/issues)
- Open a new issue if your question/problem isn't already covered
- Join discussions in pull requests

Thank you for contributing!
