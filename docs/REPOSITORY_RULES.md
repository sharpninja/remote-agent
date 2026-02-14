# Repository rules

## Warnings as errors

- **All warnings are treated as errors.** The build is configured with `TreatWarningsAsErrors=true` (see `Directory.Build.props`). The CI build fails on any warning.
- **Do not use exclusions or suppress warnings.** Do not add `NoWarn`, `WarningsAsErrors` (to turn off specific warnings), `#pragma warning disable`, or `[SuppressMessage]` to hide warnings.
- **Resolve warnings by refactoring code.** Fix the underlying issue (nullability, unused code, style, etc.) so that the warning no longer occurs.
- **Suppressing a specific warning is allowed only when explicitly approved.** If in a particular case the product owner/maintainer has agreed that a specific warning may be suppressed (e.g. in a comment or PR), then and only then may that single warning be suppressed, and only for that specific location.

### Explicit allowance (nullable in Android bindings)

- **File:** `src/RemoteAgent.App/Platforms/Android/Services/PlatformNotificationService.Android.cs`
- **Reason:** The Android/AndroidX library bindings declare parameters and return types as nullable. At our call sites we perform null checks and use `is not null` / `ThrowIfNull`, but the compiler still reports CS8602 (dereference of a possibly null reference) because it does not narrow null state for these external types. Resolving this without any suppression would require updated bindings or a different API.
- **Allowance:** Nullable reference context is disabled for this file only (`#nullable disable`), with this rule and the file comment documenting the exception. No other suppressions or exclusions are used in this file.
