# Repository rules

## UI Buttons and CQRS

**Every button added to the Desktop UI must be backed by a CQRS request/handler pair with complete tests.**

- **Request:** Create a `*Request` record in `src/RemoteAgent.Desktop/Requests/` implementing `IRequest<CommandResult>`.
- **Handler:** Create a matching `*Handler` class in `src/RemoteAgent.Desktop/Handlers/` implementing `IRequestHandler<TRequest, CommandResult>`.
- **Register:** Add the handler to `ConfigureServices` in `App.axaml.cs` as a transient `IRequestHandler<TRequest, CommandResult>`.
- **ViewModel:** The command bound to the button must dispatch via `IRequestDispatcher`. No logic that belongs in the handler may live directly in the ViewModel.
- **Handler tests:** Add `*HandlerTests.cs` in `tests/RemoteAgent.Desktop.UiTests/Handlers/` covering at least success, failure/empty, and any meaningful edge cases. Use `[Fact]` for pure logic tests and `[AvaloniaFact]` only when Avalonia UI context is required.
- **UI tests:** If the button interaction involves ViewModel state changes observable from the UI, add corresponding tests in `tests/RemoteAgent.Desktop.UiTests/`.
- **Stubs:** Any new infrastructure interface introduced for the handler must have a `Null*` stub added to `SharedHandlerTestStubs.cs`.

This rule exists so that all button behaviour is independently testable, auditable through the request pipeline, and consistent with the established CQRS architecture.

## Warnings as errors

- **All warnings are treated as errors.** The build is configured with `TreatWarningsAsErrors=true` (see `Directory.Build.props`). The CI build fails on any warning.
- **Do not use exclusions or suppress warnings.** Do not add `NoWarn`, `WarningsAsErrors` (to turn off specific warnings), `#pragma warning disable`, or `[SuppressMessage]` to hide warnings.
- **Resolve warnings by refactoring code.** Fix the underlying issue (nullability, unused code, style, etc.) so that the warning no longer occurs.
- **Suppressing a specific warning is allowed only when explicitly approved.** If in a particular case the product owner/maintainer has agreed that a specific warning may be suppressed (e.g. in a comment or PR), then and only then may that single warning be suppressed, and only for that specific location.

### Explicit allowance (nullable in Android bindings)

- **File:** `src/RemoteAgent.App/Platforms/Android/Services/PlatformNotificationService.Android.cs`
- **Reason:** The Android/AndroidX library bindings declare parameters and return types as nullable. At our call sites we perform null checks and use `is not null` / `ThrowIfNull`, but the compiler still reports CS8602 (dereference of a possibly null reference) because it does not narrow null state for these external types. Resolving this without any suppression would require updated bindings or a different API.
- **Allowance:** Nullable reference context is disabled for this file only (`#nullable disable`), with this rule and the file comment documenting the exception. No other suppressions or exclusions are used in this file.
