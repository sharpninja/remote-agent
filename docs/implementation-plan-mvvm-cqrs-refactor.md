# Implementation Plan: MVVM + CQRS Refactoring

This plan completes the refactoring described in the session handoff: **no code-behind**, **strict MVVM**, **strict CQRS** for all UI commands/events, with full test coverage and passing unit, integration, and UI tests.

**References:** [TR-18.1–TR-18.4](technical-requirements.md#18-ui-commandevent-cqrs-testability), [SESSION_HANDOFF](../SESSION_HANDOFF_2026-02-18T12-26-48-0600.md).

---

## 0. CQRS scope policy

TR-18.1 states: *"**All** UI-triggered commands and UI events shall be implemented using a CQRS-style split between command/query request objects and handler components so UI layers do not directly embed business or transport logic."*

### 0.1 Interpretation

The word **all** applies to every `ICommand` that performs business logic, transport/API calls, or side-effects beyond trivial UI state toggling. The following classification governs which commands need CQRS handlers and which do not:

| Category | Needs CQRS handler? | Examples |
|----------|---------------------|----------|
| Commands that call API / gRPC endpoints | **Yes** | `RefreshOpenSessionsCommand`, `SaveMcpServerCommand`, `ConnectCommand` |
| Commands that mutate persistent state | **Yes** | `SaveServerCommand`, `DeleteAuthUserCommand`, `ArchiveMessageCommand` |
| Commands that coordinate multi-step workflows | **Yes** | `NewSessionCommand` (capacity check → create → connect), `UsePromptTemplateCommand` |
| Commands that require platform UI interaction | **Yes** | Commands that show dialogs, pickers, or notifications via abstractions |
| Commands that toggle trivial local VM state only | **No** | `CollapseStatusLogCommand` (sets `IsStatusLogExpanded = false`), `ClearLogFilterCommand` (resets filter strings) |
| Pure view concerns (layout, theme) | **No** | `ToggleThemeCommand` (sets `ThemeVariant`), `OnOpened` layout measurement |

Trivial state toggles remain as `RelayCommand` lambdas in the VM. Everything else gets a request/handler pair.

**CorrelationId convention:** Every request record listed below includes `Guid CorrelationId` as its first positional parameter. This is omitted from the table for brevity — see Section 2.8 for full design.

### 0.2 Complete command inventory

#### Desktop — `MainWindowViewModel` (6 commands)

| Command | Classification | CQRS handler required? |
|---------|---------------|----------------------|
| `NewServerCommand` | Trivial state (clears editor fields) | No |
| `SaveServerCommand` | Persistent state mutation (upsert to `IServerRegistrationStore`) | **Yes** — `SaveServerRegistrationRequest` |
| `RemoveServerCommand` | Persistent state mutation (delete from store) | **Yes** — `RemoveServerRegistrationRequest` |
| `CheckLocalServerCommand` | External probe (`ILocalServerManager.ProbeAsync`) | **Yes** — `CheckLocalServerRequest` |
| `ApplyLocalServerActionCommand` | External action + probe (start/stop server) | **Yes** — `ApplyLocalServerActionRequest` |
| `CollapseStatusLogCommand` | Trivial toggle (`IsStatusLogExpanded = false`) | No |

#### Desktop — `ServerWorkspaceViewModel` (28 commands)

| Command | Classification | CQRS handler required? |
|---------|---------------|----------------------|
| `NewSessionCommand` | Multi-step workflow: capacity check → factory create → connect | **Yes** — `CreateDesktopSessionRequest` |
| `CheckCapacityCommand` | API call (`IServerCapacityClient`) | **Yes** — `CheckSessionCapacityRequest` |
| `RefreshOpenSessionsCommand` | API call | **Yes** — `RefreshOpenSessionsRequest` |
| `TerminateOpenServerSessionCommand` | API call (terminate remote session) | **Yes** — `TerminateOpenServerSessionRequest` |
| `TerminateCurrentSessionCommand` | Side-effect: disconnect gRPC, remove from collection | **Yes** — `TerminateDesktopSessionRequest` |
| `TerminateSessionCommand` (parameterized) | Same as above with explicit target | **Yes** — reuse `TerminateDesktopSessionRequest` |
| `SendCurrentMessageCommand` | gRPC send + collection mutation | **Yes** — `SendDesktopMessageRequest` |
| `RefreshSecurityDataCommand` | Multiple API calls (abandoned, peers, history, banned) | **Yes** — `RefreshSecurityDataRequest` |
| `BanSelectedPeerCommand` | API call + refresh | **Yes** — `BanPeerRequest` |
| `UnbanSelectedPeerCommand` | API call + refresh | **Yes** — `UnbanPeerRequest` |
| `RefreshAuthUsersCommand` | API call | **Yes** — `RefreshAuthUsersRequest` |
| `SaveAuthUserCommand` | API call | **Yes** — `SaveAuthUserRequest` |
| `DeleteAuthUserCommand` | API call | **Yes** — `DeleteAuthUserRequest` |
| `RefreshPluginsCommand` | API call | **Yes** — `RefreshPluginsRequest` |
| `SavePluginsCommand` | API call | **Yes** — `SavePluginsRequest` |
| `RefreshMcpCommand` | API call | **Yes** — `RefreshMcpRegistryRequest` |
| `SaveMcpServerCommand` | API call | **Yes** — `SaveMcpServerRequest` |
| `DeleteMcpServerCommand` | API call | **Yes** — `DeleteMcpServerRequest` |
| `SaveAgentMcpMappingCommand` | API call | **Yes** — `SaveAgentMcpMappingRequest` |
| `RefreshPromptTemplatesCommand` | API call | **Yes** — `RefreshPromptTemplatesRequest` |
| `SavePromptTemplateCommand` | API call | **Yes** — `SavePromptTemplateRequest` |
| `DeletePromptTemplateCommand` | API call | **Yes** — `DeletePromptTemplateRequest` |
| `SeedContextCommand` | API call | **Yes** — `SeedSessionContextRequest` |
| `ToggleThemeCommand` | Pure view concern (`ThemeVariant`) | No |
| `StartLogMonitoringCommand` | API streaming + LiteDB ingest | **Yes** — `StartLogMonitoringRequest` |
| `StopLogMonitoringCommand` | Cancels token + trivial state | No |
| `ApplyLogFilterCommand` | Local LiteDB query (no API) | No — stays as local VM action |
| `ClearLogFilterCommand` | Trivial state reset + local query | No |

**Desktop CQRS handler count: 24** (plus 4 code-behind removals from Phase 1)

#### Desktop — Code-behind removal (currently in `MainWindow.axaml.cs`)

| Code-behind handler | Target request | Notes |
|---------------------|---------------|-------|
| `OnManagementNavItemInvoked` | `SetManagementSectionRequest` | VM command bound via attached behavior |
| `OnStartSessionClick` | `OpenNewSessionRequest` | Shows dialog via `IConnectionSettingsDialogService`; applies result; dispatches `CreateDesktopSessionRequest` |
| `OnStatusBarPointerPressed` | `ExpandStatusLogPanelRequest` | Bound via double-tap behavior |
| `OnOpened` (layout measurement) | None — remains as minimal view-adapter | See Section 4.1 view-adapter exceptions |

#### Mobile — `MainPageViewModel` (9 commands)

| Command | Classification | CQRS handler required? |
|---------|---------------|----------------------|
| `ConnectCommand` | Multi-step: mode select → server info → agent pick → capacity check → gRPC connect | **Yes** — `ConnectMobileSessionRequest` |
| `DisconnectCommand` | gRPC disconnect | **Yes** — `DisconnectMobileSessionRequest` |
| `NewSessionCommand` | Persistent state (create session in `ISessionStore`) | **Yes** — `CreateMobileSessionRequest` |
| `TerminateCurrentSessionCommand` | Persistent + gRPC stop | **Yes** — `TerminateMobileSessionRequest` |
| `TerminateSessionCommand` (parameterized) | Same as above | **Yes** — reuse `TerminateMobileSessionRequest` |
| `SendMessageCommand` | gRPC send + collection mutation + auto-title | **Yes** — `SendMobileMessageRequest` |
| `AttachCommand` | Platform picker + gRPC send | **Yes** — `SendMobileAttachmentRequest` |
| `ArchiveMessageCommand` | Persistent state (`ILocalMessageStore`) | **Yes** — `ArchiveMessageRequest` |
| `UsePromptTemplateCommand` | API call + platform prompts + send | **Yes** — `UsePromptTemplateRequest` |

#### Mobile — Code-behind delegate callbacks (currently in `MainPage.xaml.cs`)

| Delegate | Target platform abstraction | Request (if dispatched from handler) |
|----------|---------------------------|--------------------------------------|
| `ConnectionModeSelector` | `IConnectionModeSelector` | Invoked inside `ConnectMobileSessionRequest` handler |
| `AgentSelector` | `IAgentSelector` | Invoked inside `ConnectMobileSessionRequest` handler |
| `AttachmentPicker` | `IAttachmentPicker` | Invoked inside `SendMobileAttachmentRequest` handler |
| `PromptTemplateSelector` | `IPromptTemplateSelector` | Invoked inside `UsePromptTemplateRequest` handler |
| `PromptVariableValueProvider` | `IPromptVariableProvider` | Invoked inside `UsePromptTemplateRequest` handler |
| `SessionTerminationConfirmation` | `ISessionTerminationConfirmation` | Invoked inside `TerminateMobileSessionRequest` handler |
| `ShowNotificationForMessage` | `INotificationService` | Invoked inside a `NotifyMessageRequest` handler or from the gateway message event |

#### Mobile — Code-behind in `McpRegistryPage.xaml.cs`

All logic (refresh, save, delete, clear, tap-select, preference read/write) moves to `McpRegistryPageViewModel` which dispatches:
- `LoadMcpServersRequest`
- `SaveMcpServerRequest` (mobile)
- `DeleteMcpServerRequest` (mobile)

#### Mobile — Code-behind in `AppShell.xaml.cs`

All flyout handlers and `BuildSessionButtons` move to `AppShellViewModel` which exposes:
- `SessionItems` (observable collection, bound in XAML template)
- `StartSessionCommand` → dispatches `CreateMobileSessionRequest`
- `SelectSessionCommand` → dispatches `SelectMobileSessionRequest`
- `TerminateSessionCommand` → dispatches `TerminateMobileSessionRequest`
- `NavigateToSettingsCommand`, `NavigateToAccountCommand` → dispatches `NavigateToRouteRequest`

**Mobile CQRS handler count: 9 (from VM) + 3 (MCP page) + 5 (AppShell) = 17**

**Grand total CQRS handlers: ~41** (24 desktop + 17 mobile; some request types may be shared). Every request carries `Guid CorrelationId` as its first parameter (Section 2.8).

### 0.3 What is NOT in scope

- `SettingsPage.xaml.cs` and `AccountManagementPage.xaml.cs` are currently stub pages with no logic. They are out of scope until they gain functionality.
- `DesktopSessionViewModel` is a data-carrier created by a factory. Its operations (connect, send, terminate) are invoked by `ServerWorkspaceViewModel` commands, which will go through CQRS. The session VM itself does not need its own handlers unless it exposes its own `ICommand` bindings in XAML. Currently it does not.

---

## 1. Current state summary

### 1.1 Desktop (Avalonia)

| Location | Code-behind / violation | Description |
|----------|-------------------------|-------------|
| `MainWindow.axaml.cs` | `OnManagementNavItemInvoked` | Navigation: calls `viewModel.SetManagementSection(sectionKey)`. |
| `MainWindow.axaml.cs` | `OnStartSessionClick` | Opens `ConnectionSettingsDialog`, applies result to workspace, runs `NewSessionCommand`. |
| `MainWindow.axaml.cs` | `OnOpened` | Layout: measures nav items and sets `NavigationView.OpenPaneLength`. Pure view-adapter; will remain as minimal code-behind (see Section 4.1). |
| `MainWindow.axaml.cs` | `OnStatusBarPointerPressed` | Double-tap: calls `viewModel.ExpandStatusLogPanel()`. |
| `ConnectionSettingsDialog.axaml.cs` | Full dialog logic | Reads/writes controls by name, validation, `Close(true/false)`, builds `ConnectionSettingsDialogResult`. |

**XAML:** `MainWindow.axaml` uses `Click="OnStartSessionClick"`, `PointerPressed="OnStatusBarPointerPressed"`, `ItemInvoked="OnManagementNavItemInvoked"`; `Opened` in code-behind.

**`ServerWorkspaceViewModel`:** 1704 lines, 28 commands, 74 properties. Responsibilities span sessions, security, auth, plugins, MCP, prompts, structured logs, and theme. This is a god object that must be decomposed (see Section 4.3).

**Static API calls:** `ServerWorkspaceViewModel` calls `ServerApiClient.GetStructuredLogsSnapshotAsync()` and `ServerApiClient.MonitorStructuredLogsAsync()` as static methods. These are not mockable and must be wrapped behind an interface.

### 1.2 Mobile (MAUI)

| Location | Code-behind / violation | Description |
|----------|-------------------------|-------------|
| `MainPage.xaml.cs` | Delegate injection | `ConnectionModeSelector`, `AgentSelector`, `AttachmentPicker`, etc. set on VM; VM calls back into page for dialogs/pickers. |
| `MainPage.xaml.cs` | `ShowNotificationForMessage` | Event handler; platform-specific notification. |
| `MainPage.xaml.cs` | `WireMessageInputShortcuts` / `OnMessageTextBoxKeyDown` | Windows Ctrl+Enter submit. |
| `MainPage.xaml.cs` | Session title focus/tap handlers | `OnSessionTitleFocused`, `OnSessionTitleUnfocused`, `OnSessionTitleCompleted`, `OnSessionTitleLabelTapped`, `CommitSessionTitle`. |
| `MainPage.xaml.cs` | Shell entry points | `StartNewSessionFromShell`, `SelectSessionFromShell`, `TerminateSessionFromShellAsync`, `GetCurrentSessionId` called from AppShell. |
| `McpRegistryPage.xaml.cs` | Entire page logic | No VM; `Preferences` direct access, `AgentGatewayClientService` static calls, form state, refresh/save/delete/clear in code-behind. |
| `AppShell.xaml.cs` | `BuildSessionButtons` | Reads `ISessionStore`, builds UI controls in code. |
| `AppShell.xaml.cs` | Flyout click handlers | `OnOpenSessionsClicked`, `OnStartSessionClicked`, `OnSettingsClicked`, `OnAccountManagementClicked` call MainPage or navigate. |

**Concrete dependency:** `MainPageViewModel` constructor takes concrete `AgentGatewayClientService`, not an interface. Must be wrapped.

**Static API calls:** `MainPageViewModel` calls `ServerApiClient.GetServerInfoAsync()`, `ServerApiClient.GetSessionCapacityAsync()`, and `ServerApiClient.ListPromptTemplatesAsync()` as static methods. `McpRegistryPage` calls `AgentGatewayClientService.ListMcpServersAsync()` etc. as static/class calls. All must be wrapped behind interfaces.

**Direct `Preferences` access:** `McpRegistryPage` and `MainPageViewModel` read/write MAUI `Preferences.Default` directly. Must be abstracted for testability.

### 1.3 Shared / infrastructure

- **No CQRS yet:** No `IRequest`/`IRequestHandler` or mediator; commands are `ICommand` implementations that call methods directly.
- **No MVVM toolkit:** Both apps manually implement `INotifyPropertyChanged` with boilerplate. The refactor should evaluate `CommunityToolkit.Mvvm` adoption (see Section 2.4).
- **Custom `RelayCommand`:** Desktop has `RelayCommand` and `RelayCommand<T>` in `Infrastructure/RelayCommand.cs`. Mobile uses MAUI's `Command`. Both will continue to wrap dispatched calls.
- **Shared lib:** `RemoteAgent.App.Logic` is referenced by both Desktop and App; suitable for shared CQRS abstractions, DTOs, and request types. Handler implementations live in each client project.

---

## 2. Target architecture (TR-18)

- **Commands/Queries/Events:** Represented as request types (e.g. `SaveMcpServerRequest`, `ConnectMobileSessionRequest`). Handlers implement `IRequestHandler<TRequest, TResponse>`.
- **UI:** Only binds to ViewModel commands; commands dispatch requests to a handler (interface). No business/transport logic in views or code-behind.
- **Testability:** Handlers are unit-tested with mocked dependencies; UI tests substitute mock dispatcher/handlers and assert UI state and status/error behavior.

### 2.1 Interface contracts

All interfaces live in `RemoteAgent.App.Logic` (no new project needed).

```csharp
// Base interface. TResponse is the handler return type.
// Every request carries a CorrelationId for end-to-end tracing of UI interactions.
public interface IRequest<TResponse>
{
    Guid CorrelationId { get; }
}

// Handler contract. One handler per request type.
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

// Dispatcher resolves and invokes the correct handler.
public interface IRequestDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
```

### 2.2 Dispatcher implementation

```csharp
public sealed class ServiceProviderRequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceProviderRequestDispatcher> _logger;

    public ServiceProviderRequestDispatcher(
        IServiceProvider serviceProvider,
        ILogger<ServiceProviderRequestDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CorrelationId == Guid.Empty)
            throw new ArgumentException("CorrelationId must not be Guid.Empty.", nameof(request));

        var requestType = request.GetType();
        var correlationId = request.CorrelationId;

        _logger.LogDebug("CQRS Enter {RequestType} [{CorrelationId}]: {Request}",
            requestType.Name, correlationId, request);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;

        try
        {
            var result = await (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
            _logger.LogDebug("CQRS Leave {RequestType} [{CorrelationId}]: {Result}",
                requestType.Name, correlationId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CQRS Leave {RequestType} [{CorrelationId}] with exception: {ExceptionMessage}",
                requestType.Name, correlationId, ex.Message);
            throw;
        }
    }
}
```

Note: This uses reflection-based dispatch for simplicity. If perf matters, a source-generated or dictionary-keyed approach can replace it later. Entry/exit logging is a cross-cutting concern handled here — individual handlers do not log their own entry/exit (see Section 2.6). The `[{CorrelationId}]` in each log message enables filtering all log entries for a single UI interaction chain (see Section 2.8).

### 2.3 Result types

Every handler returns a typed result, never `void`. Use `Unit` for side-effect-only handlers:

```csharp
public readonly record struct Unit
{
    public static readonly Unit Value = default;
    public static readonly Task<Unit> TaskValue = Task.FromResult(Value);
}
```

For handlers that can fail, return a result record:

```csharp
public record CommandResult(bool Success, string? ErrorMessage = null)
{
    public static CommandResult Ok() => new(true);
    public static CommandResult Fail(string message) => new(false, message);
}

public record CommandResult<T>(bool Success, T? Data = default, string? ErrorMessage = null)
    where T : class
{
    public static CommandResult<T> Ok(T data) => new(true, data);
    public static CommandResult<T> Fail(string message) => new(false, ErrorMessage: message);
}
```

### 2.4 MVVM toolkit evaluation

**Decision: Do NOT adopt `CommunityToolkit.Mvvm` in this refactor.**

Rationale:
- The desktop project targets .NET 9 (Avalonia); mobile targets .NET 10 (MAUI). The toolkit is compatible with both, but adding it changes the base class and attribute model for every VM.
- The current `INotifyPropertyChanged` boilerplate is verbose but working. Replacing it simultaneously with the CQRS refactor doubles the diff size and risk.
- **Recommendation:** Adopt `CommunityToolkit.Mvvm` in a separate follow-up refactor after TR-18.x is complete. The CQRS plan should not be blocked or complicated by a toolkit migration.

### 2.5 Error propagation pattern

Current VMs use `RunCommand`/`RunCommandAsync` wrappers that catch exceptions and set `StatusText`. The CQRS pattern replaces this with:

1. **Handlers return `CommandResult` or typed results.** Handlers do not throw for expected failures (validation, API errors, timeouts). They catch and return `CommandResult.Fail(message)`.
2. **Handlers throw only for truly unexpected errors** (null service, programming bug). The VM command wrapper catches those.
3. **VM command pattern:**

```csharp
// In ViewModel — generates CorrelationId at the UI interaction boundary:
private async Task ExecuteRefreshOpenSessions()
{
    var correlationId = Guid.NewGuid();
    StatusText = "Refreshing open sessions...";
    try
    {
        var result = await _dispatcher.SendAsync(
            new RefreshOpenSessionsRequest(correlationId, Host, Port, ApiKey));
        if (!result.Success)
        {
            StatusText = result.ErrorMessage ?? "Failed.";
            return;
        }
        // Apply result.Data to VM collections
        StatusText = $"Loaded {result.Data.Count} session(s).";
    }
    catch (Exception ex)
    {
        StatusText = $"Command failed: {ex.Message}";
    }
}
```

This preserves the existing UX pattern while moving business logic into testable handlers.

### 2.6 Handler entry/exit logging

**Requirement:** All CQRS commands and queries shall write Debug-level log messages upon entering the command/query and leaving the command/query. Entry logs include the request type name and all parameters. Exit logs include the request type name and the result (or exception).

**Design:** Logging is implemented as a cross-cutting concern in the dispatcher, not in individual handlers. This guarantees every request is logged uniformly without handler authors needing to remember to add logging code.

**Why the dispatcher and not each handler:**
- **DRY:** One logging call site covers all ~41 handlers. No risk of a handler forgetting to log.
- **Consistent format:** Every entry/exit log has the same structure, making log search and filtering trivial.
- **Testable in isolation:** Three dispatcher tests cover all logging behavior. Handlers remain focused on business logic.
- **Exception coverage:** The dispatcher's `try/catch` logs failures that a handler might not anticipate.

**Log format:**

| Event | Level | Template | Structured properties |
|-------|-------|----------|-----------------------|
| Entry | `Debug` | `"CQRS Enter {RequestType} [{CorrelationId}]: {Request}"` | `RequestType` = request class name, `CorrelationId` = `request.CorrelationId`, `Request` = full request record (`ToString()`) |
| Exit (success) | `Debug` | `"CQRS Leave {RequestType} [{CorrelationId}]: {Result}"` | `RequestType` = request class name, `CorrelationId` = `request.CorrelationId`, `Result` = handler return value (`ToString()`) |
| Exit (exception) | `Debug` | `"CQRS Leave {RequestType} [{CorrelationId}] with exception: {ExceptionMessage}"` | `RequestType` = request class name, `CorrelationId` = `request.CorrelationId`, `ExceptionMessage` = `ex.Message`; exception object attached to log entry |

The `[{CorrelationId}]` component enables filtering all log entries for a single UI interaction chain — including parent and child request dispatches (see Section 2.8).

**Sensitive data policy:** Request records that carry API keys or credentials (e.g., `ApiKey` in connection-related requests) MUST override `ToString()` to redact sensitive fields. The override MUST still include `CorrelationId`. Example:

```csharp
public sealed record RefreshOpenSessionsRequest(Guid CorrelationId, string Host, int Port, string? ApiKey)
    : IRequest<CommandResult<OpenSessionsData>>
{
    public override string ToString() =>
        $"RefreshOpenSessionsRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = {(ApiKey is null ? "null" : "***")} }}";
}
```

Any request record containing `ApiKey`, `Password`, `Token`, or `Secret` properties MUST implement this redaction pattern. A code-review checklist item is added for this in Phase 4.

**Logger dependency:** The dispatcher takes `ILogger<ServiceProviderRequestDispatcher>` via constructor injection. No additional NuGet packages are needed — `Microsoft.Extensions.Logging.Abstractions` is already transitively referenced.

### 2.7 Thread-marshaling design

- **Handlers run on the calling thread** (typically UI thread for synchronous dispatches, or a thread-pool thread if the VM uses `await`).
- **Handlers that call async APIs** (`HttpClient`, gRPC) naturally return to the calling context via `await`.
- **Handlers MUST NOT call `Dispatcher.UIThread.Post`** or any UI-framework API. They return data; the VM is responsible for applying data to `ObservableCollection` on the correct thread.
- **The dispatcher does not handle thread marshaling.** The VM's command wrapper (above) runs on the UI thread; the `await` naturally resumes there.
- **Exception:** The structured log monitor handler returns an `IAsyncEnumerable` or accepts a callback. If it pushes data from a background thread, the VM posts to the UI thread when applying data. The handler itself remains UI-framework-free.

### 2.8 CorrelationId design

**Requirement:** Every command/request MUST have a required `CorrelationId` parameter (`Guid`) that is tracked through to any subsequent commands/queries dispatched within the same handler chain, enabling end-to-end tracing of individual UI interactions.

#### 2.8.1 Interface contract

`IRequest<TResponse>` requires `Guid CorrelationId { get; }` (see Section 2.1). All request records satisfy this via a positional parameter. `CorrelationId` is always the **first** parameter of every request record, by convention.

#### 2.8.2 Generation

The CorrelationId is generated **once** at the UI interaction boundary — the ViewModel command method that responds to a user action. Every request dispatched from that command (and every child request dispatched by handlers within that chain) shares the same CorrelationId.

```csharp
// In ViewModel — the UI interaction boundary:
private async Task ExecuteRefreshOpenSessions()
{
    var correlationId = Guid.NewGuid();
    StatusText = "Refreshing open sessions...";
    try
    {
        var result = await _dispatcher.SendAsync(
            new RefreshOpenSessionsRequest(correlationId, Host, Port, ApiKey));
        // ...
    }
    catch (Exception ex)
    {
        StatusText = $"Command failed: {ex.Message}";
    }
}
```

#### 2.8.3 Propagation

When a handler dispatches subsequent commands/queries, it MUST propagate the CorrelationId from the parent request:

```csharp
public sealed class OpenNewSessionHandler : IRequestHandler<OpenNewSessionRequest, CommandResult>
{
    private readonly IConnectionSettingsDialogService _dialogService;
    private readonly IRequestDispatcher _dispatcher;

    // constructor omitted

    public async Task<CommandResult> HandleAsync(OpenNewSessionRequest request, CancellationToken ct)
    {
        var dialogResult = await _dialogService.ShowAsync(request.OwnerWindow, /* defaults */, ct);
        if (dialogResult is null)
            return CommandResult.Fail("Cancelled.");

        // Propagate the parent's CorrelationId to the child request:
        return await _dispatcher.SendAsync(
            new CreateDesktopSessionRequest(request.CorrelationId, dialogResult.Host, dialogResult.Port, /* ... */), ct);
    }
}
```

This produces a log trace like:

```
[Debug] CQRS Enter OpenNewSessionRequest [3a7f...c1d2]: OpenNewSessionRequest { CorrelationId = 3a7f...c1d2, ... }
[Debug] CQRS Enter CreateDesktopSessionRequest [3a7f...c1d2]: CreateDesktopSessionRequest { CorrelationId = 3a7f...c1d2, ... }
[Debug] CQRS Leave CreateDesktopSessionRequest [3a7f...c1d2]: CommandResult { Success = True }
[Debug] CQRS Leave OpenNewSessionRequest [3a7f...c1d2]: CommandResult { Success = True }
```

Filtering logs by `[3a7f...c1d2]` shows the entire interaction chain.

#### 2.8.4 Handlers that dispatch sub-commands

The following handlers dispatch one or more child requests and MUST propagate CorrelationId:

| Handler | Child request(s) dispatched |
|---------|---------------------------|
| `OpenNewSessionHandler` | `CreateDesktopSessionRequest` |
| `CreateDesktopSessionHandler` | `CheckSessionCapacityRequest` (conditionally) |
| `ConnectMobileSessionHandler` | `CreateMobileSessionRequest` (conditionally, if no current session) |
| `UsePromptTemplateHandler` | `SendMobileMessageRequest` (after template rendering) |
| `BanPeerHandler` | `RefreshSecurityDataRequest` (after ban) |
| `UnbanPeerHandler` | `RefreshSecurityDataRequest` (after unban) |

All other handlers are leaf handlers (they do not dispatch sub-commands) and simply carry the CorrelationId for logging purposes.

#### 2.8.5 Conventions

1. `CorrelationId` is always the **first** positional parameter of every request record.
2. `CorrelationId` MUST NOT be `Guid.Empty`. The dispatcher validates this and throws `ArgumentException` if violated.
3. `CorrelationId` is included in `ToString()` output for all request records (records do this automatically unless `ToString()` is overridden — overrides must include it).
4. Test helpers provide `Guid.NewGuid()` for CorrelationId in test request construction. A `TestCorrelationId` constant can simplify assertions.
5. `CorrelationId` is NOT stored in business state (sessions, messages, etc.). It is purely for log tracing.

---

## 3. Phase 0: Shared CQRS foundation

**Goal:** Introduce request/handler/dispatcher abstractions in shared code, wire one Desktop flow end-to-end, prove the pattern with a unit test.

**Estimated effort:** 1 session.

### 3.1 Abstractions (in `RemoteAgent.App.Logic`)

Add the following files to `RemoteAgent.App.Logic`:

- `Cqrs/IRequest.cs` — `IRequest<TResponse>` with required `Guid CorrelationId` property
- `Cqrs/IRequestHandler.cs` — `IRequestHandler<TRequest, TResponse>`
- `Cqrs/IRequestDispatcher.cs` — `IRequestDispatcher`
- `Cqrs/ServiceProviderRequestDispatcher.cs` — reflection-based dispatcher with `ILogger<ServiceProviderRequestDispatcher>` for entry/exit Debug logging (see Section 2.6)
- `Cqrs/Unit.cs` — `Unit` return type
- `Cqrs/CommandResult.cs` — `CommandResult` and `CommandResult<T>`

`ServiceProviderRequestDispatcher` requires `Microsoft.Extensions.Logging.Abstractions` which is already transitively available in both Desktop and Mobile projects. No new package reference needed.

### 3.2 One Desktop handler end-to-end

Pick `SetManagementSectionRequest` as the simplest possible handler to prove the pattern:

**Request:**
```csharp
public sealed record SetManagementSectionRequest(Guid CorrelationId, string SectionKey) : IRequest<Unit>;
```

**Handler:**
```csharp
public sealed class SetManagementSectionHandler : IRequestHandler<SetManagementSectionRequest, Unit>
{
    public Task<Unit> HandleAsync(SetManagementSectionRequest request, CancellationToken cancellationToken)
    {
        return Unit.TaskValue;
    }
}
```

**VM change:** `MainWindowViewModel` takes `IRequestDispatcher`; `SetManagementSection(string)` generates a `CorrelationId` and dispatches `SetManagementSectionRequest`; the VM applies the result (sets `SelectedManagementSection`).

```csharp
private async Task ExecuteSetManagementSection(string sectionKey)
{
    await _dispatcher.SendAsync(new SetManagementSectionRequest(Guid.NewGuid(), sectionKey));
    SelectedManagementSection = sectionKey;
}
```

**XAML change:** Replace `ItemInvoked="OnManagementNavItemInvoked"` with an attached behavior (see Section 3.3).

**DI registration:** In `App.axaml.cs`:
```csharp
services.AddSingleton<IRequestDispatcher, ServiceProviderRequestDispatcher>();
services.AddTransient<IRequestHandler<SetManagementSectionRequest, Unit>, SetManagementSectionHandler>();
```

### 3.3 Avalonia attached behavior for `NavigationView.ItemInvoked`

Avalonia's `NavigationView` does not expose a command property for `ItemInvoked`. A custom attached behavior is required:

**File:** `src/RemoteAgent.Desktop/Behaviors/NavigationViewItemInvokedBehavior.cs`

The behavior attaches to a `NavigationView` and, on `ItemInvoked`, resolves the section key from `InvokedItemContainer.Tag` (or `IsSettingsInvoked`), then invokes an `ICommand` attached property with the section key as parameter.

Attached properties:
- `NavigationViewItemInvokedBehavior.CommandProperty` (type `ICommand`)
- `NavigationViewItemInvokedBehavior.SettingsKeyProperty` (type `string`, default `"Settings"`)

XAML usage:
```xml
<fa:NavigationView x:Name="ManagementNavigationView"
    behaviors:NavigationViewItemInvokedBehavior.Command="{Binding SetManagementSectionCommand}"
    behaviors:NavigationViewItemInvokedBehavior.SettingsKey="Settings">
```

### 3.4 Avalonia attached behavior for double-tap → command

**File:** `src/RemoteAgent.Desktop/Behaviors/DoubleTapBehavior.cs`

Attaches to any `Control`; on `PointerPressed` with `ClickCount == 2`, invokes the bound `ICommand`.

Attached properties:
- `DoubleTapBehavior.CommandProperty` (type `ICommand`)

XAML usage:
```xml
<Border behaviors:DoubleTapBehavior.Command="{Binding ExpandStatusLogCommand}" ... />
```

### 3.5 Unit test

In `tests/RemoteAgent.App.Tests` (or a new `tests/RemoteAgent.Cqrs.Tests`):

- Test `ServiceProviderRequestDispatcher` resolves and invokes a handler.
- Test `ServiceProviderRequestDispatcher` logs entry Debug message with request type and parameters.
- Test `ServiceProviderRequestDispatcher` logs exit Debug message with request type and result.
- Test `ServiceProviderRequestDispatcher` logs exit Debug message with exception when handler throws.
- Test `SetManagementSectionHandler` returns `Unit` for a valid key.

### 3.6 Tasks — Phase 0

1. Add CQRS interfaces and types to `RemoteAgent.App.Logic/Cqrs/`.
2. Implement `ServiceProviderRequestDispatcher` with `ILogger<ServiceProviderRequestDispatcher>` for entry/exit Debug logging.
3. Add `SetManagementSectionRequest`, `SetManagementSectionHandler`.
4. Add `NavigationViewItemInvokedBehavior` and `DoubleTapBehavior` in Desktop.
5. Wire `IRequestDispatcher` into `MainWindowViewModel`; expose `SetManagementSectionCommand` that dispatches.
6. Replace `ItemInvoked="OnManagementNavItemInvoked"` in XAML with behavior binding.
7. Register dispatcher and handler in Desktop DI.
8. Add unit tests for dispatcher (resolve, invoke, missing-handler, CancellationToken passthrough, entry log, exit log, exception log) and handler.
9. Verify solution builds (`./scripts/build-desktop-dotnet9.sh Release`).

**Exit criteria:** One Desktop flow goes through request → dispatcher → handler → VM state update; dispatcher entry/exit logging verified; unit tests pass; solution builds; existing desktop UI tests still pass.

---

## 4. Phase 1: Desktop code-behind removal and CQRS migration

**Goal:** Zero business logic in `MainWindow.axaml.cs` and `ConnectionSettingsDialog.axaml.cs`; all behavior in VMs + command bindings or request handlers. Decompose `ServerWorkspaceViewModel`. Migrate all desktop commands per the inventory.

**Estimated effort:** 3–4 sessions.

### 4.1 View-adapter exceptions

The following remain in code-behind as **view-adapter** code. They contain no business logic and are documented as acceptable:

| Code-behind | Justification |
|-------------|---------------|
| `MainWindow` constructor: `InitializeComponent(); DataContext = viewModel; Opened += OnOpened;` | Standard Avalonia view wiring. |
| `OnOpened` → `TrySetOpenPaneLength` | Measures rendered controls and sets `OpenPaneLength`. This is inherently a view-layout concern: it reads `DesiredSize` of rendered `NavigationViewItem` controls, which requires access to the visual tree. Cannot be expressed in a VM or handler. The method is a pure function of visual measurements and does not call services, mutate persistent state, or make API calls. |

**Rule:** If a code-behind method (a) accesses only visual-tree / layout APIs, (b) has no service dependencies, and (c) does not mutate persistent or business state, it is an acceptable view-adapter. Document each exception in a `// View-adapter:` comment.

### 4.2 `IConnectionSettingsDialogService` specification

**Interface (in `RemoteAgent.Desktop/Infrastructure/`):**

```csharp
public interface IConnectionSettingsDialogService
{
    Task<ConnectionSettingsDialogResult?> ShowAsync(
        Window ownerWindow,
        ConnectionSettingsDefaults defaults,
        CancellationToken cancellationToken = default);
}

public sealed record ConnectionSettingsDefaults(
    string Host,
    string Port,
    string SelectedConnectionMode,
    string SelectedAgentId,
    string ApiKey,
    string PerRequestContext,
    IReadOnlyList<string> ConnectionModes);
```

**Production implementation (`AvaloniaConnectionSettingsDialogService`):**
1. Creates a `ConnectionSettingsDialogViewModel` from `defaults`.
2. Creates a `ConnectionSettingsDialog` window with `DataContext = viewModel`.
3. Subscribes to the VM's `RequestClose` event (see below).
4. Calls `dialog.ShowDialog<bool>(ownerWindow)`.
5. If accepted, returns `viewModel.ToResult()`; else returns `null`.

**Test mock (`MockConnectionSettingsDialogService`):**
Returns a preconfigured result or `null` without showing any window.

**DI registration:** `services.AddTransient<IConnectionSettingsDialogService, AvaloniaConnectionSettingsDialogService>();`

### 4.3 `ConnectionSettingsDialogViewModel`

**File:** `src/RemoteAgent.Desktop/ViewModels/ConnectionSettingsDialogViewModel.cs`

Properties: `Host`, `Port`, `SelectedConnectionMode`, `SelectedAgentId`, `ApiKey`, `PerRequestContext`, `ConnectionModes`, `ValidationMessage`.

Commands: `SubmitCommand`, `CancelCommand`.

`SubmitCommand` validates inputs (host required, port 1–65535, mode required, agent required). On validation failure, sets `ValidationMessage`. On success, sets `IsAccepted = true` and raises `RequestClose` event.

`CancelCommand` sets `IsAccepted = false` and raises `RequestClose` event.

Event: `event Action<bool>? RequestClose;`

`ToResult()` method returns `ConnectionSettingsDialogResult` from current property values.

**Dialog code-behind after refactor:**
```csharp
public partial class ConnectionSettingsDialog : Window
{
    public ConnectionSettingsDialog(ConnectionSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += accepted => Close(accepted);
    }
}
```

**XAML changes:** Replace all `x:Name` control references with `{Binding ...}` to VM properties. Replace `Click="OnCancelClick"` with `Command="{Binding CancelCommand}"`. Replace `Click="OnCreateSessionClick"` with `Command="{Binding SubmitCommand}"`.

### 4.4 `OpenNewSessionRequest` and handler

```csharp
public sealed record OpenNewSessionRequest(
    Guid CorrelationId,
    Window OwnerWindow,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>;
```

**Handler:** `OpenNewSessionHandler`
1. Builds `ConnectionSettingsDefaults` from `Workspace`.
2. Calls `IConnectionSettingsDialogService.ShowAsync(ownerWindow, defaults)`.
3. If result is `null`, returns `CommandResult.Fail("Cancelled.")`.
4. Applies result fields to `Workspace` properties (Host, Port, Mode, Agent, ApiKey, PerRequestContext).
5. Dispatches `CreateDesktopSessionRequest` **propagating `request.CorrelationId`** (the session-creation handler).
6. Returns `CommandResult.Ok()`.

**Note:** This handler takes a `Window` reference (for dialog ownership). This is acceptable because the handler itself does not touch the visual tree — it passes the reference to the dialog service which is the boundary between CQRS and UI.

**VM change:** `MainWindowViewModel.StartSessionCommand` generates a `CorrelationId` and dispatches `OpenNewSessionRequest(Guid.NewGuid(), this.OwnerWindow, CurrentServerViewModel)`.

**Getting `OwnerWindow`:** The VM receives a `Func<Window?>` factory in its constructor (or the window sets a `SetOwnerWindow` method on the VM after construction). This avoids the VM holding a direct `Window` reference.

### 4.5 `ServerWorkspaceViewModel` decomposition

The 1704-line `ServerWorkspaceViewModel` is decomposed into focused sub-VMs. Each sub-VM handles one management section. The parent `ServerWorkspaceViewModel` retains shared state (Host, Port, ApiKey, PerRequestContext, StatusText, Sessions) and exposes sub-VMs as properties.

| New VM class | Responsibility | Commands migrated |
|-------------|---------------|-------------------|
| `SessionManagementViewModel` | Session CRUD, connect, send, terminate | `NewSessionCommand`, `TerminateCurrentSessionCommand`, `TerminateSessionCommand`, `SendCurrentMessageCommand`, `CheckCapacityCommand` |
| `OpenServerSessionsViewModel` | Remote session list + terminate | `RefreshOpenSessionsCommand`, `TerminateOpenServerSessionCommand` |
| `SecurityViewModel` | Peers, banned, connection history | `RefreshSecurityDataCommand`, `BanSelectedPeerCommand`, `UnbanSelectedPeerCommand` |
| `AuthUsersViewModel` | Auth user CRUD + roles | `RefreshAuthUsersCommand`, `SaveAuthUserCommand`, `DeleteAuthUserCommand` |
| `PluginsViewModel` | Plugin assembly CRUD | `RefreshPluginsCommand`, `SavePluginsCommand` |
| `McpRegistryViewModel` | MCP server CRUD + agent mapping | `RefreshMcpCommand`, `SaveMcpServerCommand`, `DeleteMcpServerCommand`, `SaveAgentMcpMappingCommand` |
| `PromptTemplatesViewModel` | Template CRUD + seed context | `RefreshPromptTemplatesCommand`, `SavePromptTemplateCommand`, `DeletePromptTemplateCommand`, `SeedContextCommand` |
| `StructuredLogsViewModel` | Log monitoring, filtering, display | `StartLogMonitoringCommand`, `StopLogMonitoringCommand`, `ApplyLogFilterCommand`, `ClearLogFilterCommand` |

Each sub-VM takes `IRequestDispatcher` and dispatches its commands through handlers. Shared state (Host, Port, ApiKey) is accessed via a shared `IServerConnectionContext` interface or by the parent passing values.

**Decomposition strategy:** This is done incrementally *within* Phase 1. Each sub-VM is extracted one at a time, and the corresponding XAML `DataContext` bindings are updated. Existing UI tests are run after each extraction to catch regressions.

### 4.6 Interface wrappers for static API calls

**File:** `src/RemoteAgent.Desktop/Infrastructure/IStructuredLogClient.cs`

```csharp
public interface IStructuredLogClient
{
    Task<StructuredLogsSnapshot?> GetSnapshotAsync(string host, int port, long fromOffset, int limit, string? apiKey, CancellationToken ct);
    Task MonitorAsync(string host, int port, long fromOffset, Func<StructuredLogEntry, Task> onEntry, string? apiKey, CancellationToken ct);
}
```

Production implementation wraps the existing static `ServerApiClient` methods. Registered as `services.AddSingleton<IStructuredLogClient, ServerApiClientStructuredLogAdapter>();`.

### 4.7 DI scoping for handlers

Handler lifetimes depend on their dependencies:

| Handler depends on | Handler lifetime | Why |
|-------------------|-----------------|-----|
| Singleton services only (`IServerCapacityClient`, `ILocalServerManager`) | Transient | Safe to resolve from root |
| Scoped services (`CurrentServerContext`) | Scoped | Must be resolved within the server-workspace scope |
| No dependencies (pure logic) | Transient | Cheapest |

**For scoped handlers:** The `ServerWorkspaceFactory` already creates `IServiceScope` per server. The scoped `ServerWorkspaceViewModel` (and now its sub-VMs) resolve handlers from the scoped `IServiceProvider`. The `IRequestDispatcher` in scoped VMs is itself resolved from the scope, so it resolves handlers from the correct scope.

**Concrete DI pattern:**
```csharp
// In App.axaml.cs ConfigureServices:
services.AddScoped<IRequestDispatcher, ServiceProviderRequestDispatcher>();
services.AddScoped<IRequestHandler<RefreshOpenSessionsRequest, CommandResult<OpenSessionsData>>, RefreshOpenSessionsHandler>();
// ... etc for all scoped handlers

// Singleton handlers (for MainWindowViewModel):
services.AddSingleton<IRequestDispatcher>(sp => new ServiceProviderRequestDispatcher(sp));
// MainWindowViewModel gets the singleton dispatcher which resolves singleton handlers
```

**Alternative (simpler):** Register all handlers as transient and the dispatcher as transient. Each scope resolves its own dispatcher which resolves handlers within that scope. Handlers that need scoped services will correctly get the scoped instance.

### 4.8 MainWindow code-behind after refactor

```csharp
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetOwnerWindow(() => this);
        Opened += OnOpened;
    }

    // View-adapter: measures rendered NavigationViewItem controls to set OpenPaneLength.
    // Pure layout concern — no business logic, no service calls, no state mutation.
    private void OnOpened(object? sender, EventArgs e)
    {
        // ... existing TrySetOpenPaneLength logic unchanged ...
    }

    private static bool TrySetOpenPaneLength(NavigationView navView)
    {
        // ... existing measurement logic unchanged ...
    }
}
```

All other event handlers (`OnStartSessionClick`, `OnManagementNavItemInvoked`, `OnStatusBarPointerPressed`) are deleted. Their functionality is replaced by XAML command bindings and behaviors.

### 4.9 MainWindow.axaml changes summary

| Current | Replacement |
|---------|-------------|
| `Click="OnStartSessionClick"` on menu item and toolbar button | `Command="{Binding StartSessionCommand}"` |
| `ItemInvoked="OnManagementNavItemInvoked"` | `behaviors:NavigationViewItemInvokedBehavior.Command="{Binding SetManagementSectionCommand}"` |
| `PointerPressed="OnStatusBarPointerPressed"` | `behaviors:DoubleTapBehavior.Command="{Binding ExpandStatusLogCommand}"` |

### 4.10 Desktop CQRS handler implementation order

Implement handlers in this order to minimize risk and enable incremental testing:

1. **`SetManagementSectionHandler`** (done in Phase 0)
2. **`ExpandStatusLogPanelHandler`** (trivial, validates pattern)
3. **`ConnectionSettingsDialogViewModel`** + `IConnectionSettingsDialogService` + `OpenNewSessionHandler`
4. **`SaveServerRegistrationHandler`**, `RemoveServerRegistrationHandler`
5. **`CheckLocalServerHandler`**, `ApplyLocalServerActionHandler`
6. **`CreateDesktopSessionHandler`** (replaces `NewSessionAsync`)
7. **`CheckSessionCapacityHandler`**
8. **`SendDesktopMessageHandler`**, `TerminateDesktopSessionHandler`
9. **Bulk management handlers:** security (3), auth (3), plugins (2), MCP (4), prompts (3), seed (1), open sessions (2), log monitoring (1)

After steps 1–3, run desktop UI tests. After step 8, run again. After step 9, full test.

### 4.11 Tasks — Phase 1

1. Add `IConnectionSettingsDialogService` interface and `AvaloniaConnectionSettingsDialogService` implementation.
2. Add `ConnectionSettingsDialogViewModel`; refactor dialog XAML to use bindings; reduce code-behind to constructor + `RequestClose` subscription.
3. Add `OpenNewSessionRequest`/handler; expose `StartSessionCommand` on `MainWindowViewModel` that dispatches it; bind in XAML; remove `OnStartSessionClick`.
4. Add `NavigationViewItemInvokedBehavior`; bind `SetManagementSectionCommand` in XAML; remove `OnManagementNavItemInvoked`.
5. Add `DoubleTapBehavior`; bind `ExpandStatusLogCommand` in XAML; remove `OnStatusBarPointerPressed`.
6. Add `SaveServerRegistrationHandler`, `RemoveServerRegistrationHandler`; wire commands.
7. Add `CheckLocalServerHandler`, `ApplyLocalServerActionHandler`; wire commands.
8. Decompose `ServerWorkspaceViewModel` into sub-VMs (see 4.5). Update XAML `DataContext` paths accordingly.
9. Add `IStructuredLogClient` interface wrapper for static `ServerApiClient` log methods.
10. Implement all 24 desktop CQRS handlers (per inventory in Section 0.2). For each handler:
    - Define request record in `RemoteAgent.Desktop/Requests/` (or `App.Logic/Cqrs/Requests/` if shared).
    - Implement handler in `RemoteAgent.Desktop/Handlers/`.
    - Register handler in DI.
    - Replace `RelayCommand` lambda with dispatcher call.
11. Delete all unused event handlers from `MainWindow.axaml.cs`. Verify only view-adapter code remains.
12. Run desktop UI tests and fix regressions.

**Exit criteria:** No business or navigation logic in `MainWindow.axaml.cs` or `ConnectionSettingsDialog.axaml.cs`; all commands dispatch through CQRS handlers; all desktop unit and UI tests pass.

---

## 5. Phase 2: Mobile code-behind removal and CQRS

**Goal:** Remove code-behind from `MainPage.xaml.cs`, `McpRegistryPage.xaml.cs`, and `AppShell.xaml.cs`; introduce VMs and CQRS where applicable; platform concerns behind abstractions and request handlers.

**Estimated effort:** 4–6 sessions.

### 5.1 Interface wrappers for mobile dependencies

Before any CQRS migration, the following concrete/static dependencies must be wrapped:

#### `IAgentGatewayClient` (wraps `AgentGatewayClientService`)

```csharp
public interface IAgentGatewayClient
{
    bool IsConnected { get; }
    ObservableCollection<ChatMessage> Messages { get; }
    string? PerRequestContext { get; set; }
    event Action? ConnectionStateChanged;
    event Action<ChatMessage>? MessageReceived;
    Task ConnectAsync(string host, int port, string? sessionId, string? agentId);
    void Disconnect();
    void LoadFromStore(string? sessionId);
    void AddUserMessage(ChatMessage message);
    void SetArchived(ChatMessage message, bool archived);
    Task SendTextAsync(string text);
    Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType);
    Task SendMediaAsync(byte[] content, string contentType, string? fileName);
    Task StopSessionAsync();
}
```

`AgentGatewayClientService` already has these methods; make it implement `IAgentGatewayClient`. Change `MainPageViewModel` constructor to accept `IAgentGatewayClient`.

#### `IServerApiClient` (wraps static `ServerApiClient`)

```csharp
public interface IServerApiClient
{
    Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, CancellationToken ct = default);
    Task<SessionCapacityResponse?> GetSessionCapacityAsync(string host, int port, string? agentId, CancellationToken ct = default);
    Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, CancellationToken ct = default);
    // Add more as needed for MCP operations used by McpRegistryPage
    Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, CancellationToken ct = default);
    Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, CancellationToken ct = default);
    Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, CancellationToken ct = default);
}
```

Place the interface in `RemoteAgent.App.Logic`. Production implementation in `RemoteAgent.App` wraps the static calls.

#### `IAppPreferences` (wraps `Preferences.Default`)

```csharp
public interface IAppPreferences
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
}
```

MAUI implementation: `MauiAppPreferences : IAppPreferences` that delegates to `Preferences.Default`. Test mock: `InMemoryAppPreferences`.

#### Platform UI abstractions

| Interface | MAUI implementation | Purpose |
|-----------|-------------------|---------|
| `IConnectionModeSelector` | `MauiConnectionModeSelector` (calls `DisplayActionSheet`) | Replace `ConnectionModeSelector` delegate |
| `IAgentSelector` | `MauiAgentSelector` (calls `DisplayActionSheet`) | Replace `AgentSelector` delegate |
| `IAttachmentPicker` | `MauiAttachmentPicker` (calls `FilePicker.Default.PickAsync`) | Replace `AttachmentPicker` delegate |
| `IPromptTemplateSelector` | `MauiPromptTemplateSelector` (calls `DisplayActionSheet`) | Replace `PromptTemplateSelector` delegate |
| `IPromptVariableProvider` | `MauiPromptVariableProvider` (calls `DisplayPromptAsync`) | Replace `PromptVariableValueProvider` delegate |
| `ISessionTerminationConfirmation` | `MauiSessionTerminationConfirmation` (calls `DisplayAlert`) | Replace `SessionTerminationConfirmation` delegate |
| `INotificationService` | `PlatformNotificationServiceAdapter` (calls `PlatformNotificationService.ShowNotification`) | Replace `ShowNotificationForMessage` event handler |

Each interface has one method matching the delegate signature. Each MAUI implementation requires a `Page` reference for `DisplayActionSheet`/`DisplayAlert`/`DisplayPromptAsync`. This is provided via DI: register the `MainPage` instance and have the MAUI implementations accept it.

**New type count for this step:** 7 interfaces + 7 MAUI implementations + 7 test mocks = **21 types**.

### 5.2 MainPage

#### Remove delegates

`MainPageViewModel` removes all `Func<...>` delegate properties. The constructor accepts `IRequestDispatcher` instead. Each command dispatches a request. Each handler receives the platform abstraction interfaces via DI.

Example — `ConnectCommand`:

**Request:** `ConnectMobileSessionRequest(string Host, string Port, SessionItem? CurrentSession, IReadOnlyList<SessionItem> Sessions)`

**Handler (`ConnectMobileSessionHandler`):**
1. Calls `IConnectionModeSelector.SelectAsync()` → mode.
2. Validates host/port.
3. If server mode: calls `IServerApiClient.GetServerInfoAsync()` → server info.
4. Calls `IAgentSelector.SelectAsync(serverInfo)` → agent ID.
5. If server mode: calls `IServerApiClient.GetSessionCapacityAsync()` → capacity check.
6. Calls `IAgentGatewayClient.ConnectAsync(...)`.
7. Returns `CommandResult<ConnectResult>` with session info.

The VM applies the result to its properties.

#### Remove notification subscription

Replace `_vm.NotifyMessage += ShowNotificationForMessage;` with: the gateway message-received event dispatches `NotifyMessageRequest` when priority is `Notify`. Handler calls `INotificationService.ShowAsync(title, body)`.

#### Session title

Add to `MainPageViewModel`:
- `IsEditingTitle` (bool)
- `BeginEditTitleCommand` — sets `IsEditingTitle = true`
- `CommitTitleCommand` — validates, saves, sets `IsEditingTitle = false`

XAML binds label visibility to `!IsEditingTitle`, entry visibility to `IsEditingTitle`. TapGestureRecognizer on label binds to `BeginEditTitleCommand`. Entry `Unfocused` and `Completed` bind to `CommitTitleCommand` (via `EventToCommandBehavior` from MAUI Community Toolkit, or a minimal behavior). Text selection on focus: use a MAUI behavior that selects all text when the entry receives focus.

#### Keyboard (Ctrl+Enter on Windows)

The Windows `OnMessageTextBoxKeyDown` handler accesses platform-native APIs. This remains as a **view-adapter** in `MainPage.xaml.cs` — it detects Ctrl+Enter and invokes `_vm.SendMessageCommand.Execute(null)`. No business logic; pure input translation.

#### Shell entry points

Remove `StartNewSessionFromShell()`, `SelectSessionFromShell(sid)`, `TerminateSessionFromShellAsync(sid)`, `GetCurrentSessionId()` public methods. Replace with messaging:

**Messaging approach:** Use a simple `ISessionCommandBus` (or `WeakReferenceMessenger` from CommunityToolkit if adopted later):

```csharp
public interface ISessionCommandBus
{
    void StartNewSession();
    void SelectSession(string? sessionId);
    Task<bool> TerminateSessionAsync(string? sessionId);
    string? GetCurrentSessionId();
}
```

`MainPageViewModel` implements `ISessionCommandBus`. `AppShellViewModel` receives `ISessionCommandBus` via DI. This eliminates the direct `MainPage` reference from `AppShell`.

### 5.3 McpRegistryPage

Add `McpRegistryPageViewModel`:
- Constructor takes `IRequestDispatcher`, `IAppPreferences`.
- Properties: `Host`, `Port`, `Servers`, `SelectedServer`, form fields, `Status`.
- Commands: `LoadCommand`, `RefreshCommand`, `SelectServerCommand`, `SaveCommand`, `DeleteCommand`, `ClearCommand`.
- Each command dispatches a request. Handlers use `IServerApiClient`.

**Page code-behind after refactor:**
```csharp
public partial class McpRegistryPage : ContentPage
{
    public McpRegistryPage(McpRegistryPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is McpRegistryPageViewModel vm)
            vm.RefreshCommand.Execute(null);
    }
}
```

### 5.4 AppShell

Add `AppShellViewModel`:
- Constructor takes `ISessionStore`, `ISessionCommandBus`, `IRequestDispatcher`.
- `SessionItems` — `ObservableCollection<SessionDisplayItem>` (title, sessionId).
- `StartSessionCommand` → calls `_sessionCommandBus.StartNewSession()`.
- `SelectSessionCommand(sessionId)` → calls `_sessionCommandBus.SelectSession(sessionId)`.
- `TerminateSessionCommand(sessionId)` → calls `_sessionCommandBus.TerminateSessionAsync(sessionId)`.
- `NavigateToSettingsCommand`, `NavigateToAccountCommand` → dispatches `NavigateToRouteRequest` (handler calls `Shell.Current.GoToAsync`... but this is a view concern).

**Navigation:** `Shell.GoToAsync()` is inherently a view concern. Use an `INavigationService` interface:

```csharp
public interface INavigationService
{
    Task NavigateToAsync(string route);
    void CloseFlyout();
}
```

MAUI implementation: `MauiNavigationService` that calls `Shell.Current.GoToAsync(route)` and sets `Shell.Current.FlyoutIsPresented = false`.

**AppShell code-behind after refactor:**
```csharp
public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel, ...)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Shell content registration (Items.Add) remains here — view concern
        Items.Add(new ShellContent { Title = "Home", Route = "MainPage", Content = mainPage });
        // ... etc
    }
}
```

**XAML:** Session list is a `BindableLayout` bound to `SessionItems`. Each item has `SelectSessionCommand` and `TerminateSessionCommand` bindings. Flyout buttons bind to `NavigateToSettingsCommand` etc.

`BuildSessionButtons` is replaced entirely by the data-bound template.

### 5.5 Tasks — Phase 2

1. Add `IAgentGatewayClient` interface; make `AgentGatewayClientService` implement it; change `MainPageViewModel` constructor parameter.
2. Add `IServerApiClient` interface in `App.Logic`; implement wrapper in `App`; register in DI.
3. Add `IAppPreferences` interface and `MauiAppPreferences` implementation; register in DI.
4. Add 7 platform UI abstraction interfaces + 7 MAUI implementations; register in DI.
5. Add `ISessionCommandBus` interface; make `MainPageViewModel` implement it; register in DI.
6. Add `INavigationService` interface and `MauiNavigationService` implementation; register in DI.
7. Add request types and handlers for all 9 `MainPageViewModel` commands (per inventory). Replace each command body with dispatcher call. Remove delegate properties from VM.
8. In `MainPage.xaml.cs`: remove all delegate assignments, `NotifyMessage` subscription, session title handlers (replaced by VM properties + behaviors), and shell entry points. Keep constructor (`BindingContext = _vm`) and Windows keyboard hook.
9. Add `McpRegistryPageViewModel`; add 3 MCP request/handlers; bind page; remove all logic from `McpRegistryPage.xaml.cs`.
10. Add `AppShellViewModel` with session list and commands; bind Shell XAML; remove `BuildSessionButtons` and all flyout click handlers from code-behind.
11. Register all new VMs, handlers, and services in `MauiProgram.cs`.
12. Run mobile UI tests and fix regressions.

**Exit criteria:** No business logic in `MainPage.xaml.cs`, `McpRegistryPage.xaml.cs`, or `AppShell.xaml.cs`; all mobile unit and UI tests pass.

---

## 6. Phase 3: Test coverage (TR-18.2, TR-18.4)

**Goal:** Exhaustive unit tests for every CQRS command and query handler — every known happy path and every known failure path. UI tests with mocked dispatchers.

**Estimated effort:** 4–6 sessions.

### 6.1 Test policy

Every CQRS handler MUST have:
- **All known happy paths:** Each distinct success scenario is a separate test. Multi-step handlers (e.g., connect: mode → server info → agent → capacity → connect) have happy-path tests for each branch variant (server mode, direct mode, existing session, new session).
- **All known failure paths:** Every input validation failure, every dependency failure (API returns null, API returns false, API throws, timeout), and every business-rule rejection is a separate test.
- **Assertion specificity:** Each test asserts the return type (`CommandResult.Success`, `.ErrorMessage`, `.Data`), AND verifies the correct mock methods were called (or not called) to prove the handler took the right code path.

### 6.2 Test count summary

| Category | Tests |
|----------|-------|
| Infrastructure (dispatcher + logging + CorrelationId validation) | 9 |
| `ConnectionSettingsDialogViewModel` | 9 |
| Desktop handlers (24 handlers) | 133 |
| Request `ToString()` redaction (sensitive data) | 5 |
| CorrelationId contract + propagation tests | 8 |
| Mobile handlers (14 unique handlers) | 65 |
| UI tests with mock dispatcher | 8 |
| **Total** | **~237** |

### 6.3 Infrastructure tests

**File:** `tests/RemoteAgent.App.Tests/Cqrs/ServiceProviderRequestDispatcherTests.cs`

Uses `Microsoft.Extensions.Logging.Testing` (the `FakeLogger` / `FakeLogCollector` from the `Microsoft.Extensions.Diagnostics.Testing` package) or a simple `ListLogger<T>` that captures log entries for assertion.

| # | Test name | Description |
|---|-----------|-------------|
| 1 | `SendAsync_WhenHandlerRegistered_ShouldResolveAndReturnResult` | Register a stub handler; dispatch request; assert correct result returned. |
| 2 | `SendAsync_WhenNoHandlerRegistered_ShouldThrowInvalidOperationException` | Dispatch request with no handler registered; assert `InvalidOperationException`. |
| 3 | `SendAsync_ShouldPassCancellationTokenToHandler` | Register handler that captures token; dispatch with a token; assert token received. |
| 4 | `SendAsync_WhenHandlerThrows_ShouldPropagateException` | Register handler that throws; dispatch; assert exception propagates unchanged. |
| 5 | `SendAsync_WhenCorrelationIdIsEmpty_ShouldThrowArgumentException` | Dispatch a request with `Guid.Empty` as CorrelationId; assert `ArgumentException` thrown with message containing "CorrelationId". |
| 6 | `SendAsync_ShouldLogDebugEntryWithRequestTypeCorrelationIdAndParameters` | Dispatch a request with known CorrelationId and parameters; assert logger received a Debug entry containing `"CQRS Enter"`, the request type name, the CorrelationId value, and the parameter values from `ToString()`. |
| 7 | `SendAsync_OnSuccess_ShouldLogDebugExitWithRequestTypeCorrelationIdAndResult` | Dispatch a request that succeeds; assert logger received a Debug entry containing `"CQRS Leave"`, the request type name, the CorrelationId value, and the result's `ToString()`. |
| 8 | `SendAsync_OnException_ShouldLogDebugExitWithRequestTypeCorrelationIdAndExceptionMessage` | Dispatch a request whose handler throws; assert logger received a Debug entry containing `"CQRS Leave"`, the request type name, the CorrelationId value, `"boom"`, and the exception object. |
| 9 | `SendAsync_CorrelationIdInEntryLog_ShouldMatchRequestCorrelationId` | Dispatch with a specific `Guid`; assert the `[{CorrelationId}]` value in the entry log message matches exactly. |

**Logger test helper:**

```csharp
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
```

Tests use `CapturingLogger<ServiceProviderRequestDispatcher>` and assert against `Entries` filtered by `LogLevel.Debug`.

### 6.4 `ConnectionSettingsDialogViewModel` tests

**File:** `tests/RemoteAgent.Desktop.UiTests/ConnectionSettingsDialogViewModelTests.cs`

| # | Test name | Description |
|---|-----------|-------------|
| 1 | `Submit_WithValidInputs_ShouldSetIsAcceptedAndRaiseRequestClose` | Set Host, Port (valid), Mode, Agent; execute Submit; assert `IsAccepted == true`, `RequestClose` fired with `true`, `ValidationMessage` empty. |
| 2 | `Submit_WithEmptyHost_ShouldSetValidationMessageAndNotClose` | Leave Host empty; execute Submit; assert `ValidationMessage == "Host is required."`, `IsAccepted == false`, `RequestClose` NOT fired. |
| 3 | `Submit_WithPortZero_ShouldSetValidationMessage` | Set Port to "0"; execute Submit; assert `ValidationMessage == "Port must be 1-65535."`. |
| 4 | `Submit_WithPortAbove65535_ShouldSetValidationMessage` | Set Port to "99999"; assert same validation message. |
| 5 | `Submit_WithNonNumericPort_ShouldSetValidationMessage` | Set Port to "abc"; assert same validation message. |
| 6 | `Submit_WithEmptyMode_ShouldSetValidationMessage` | Leave Mode blank; assert `ValidationMessage == "Mode is required."`. |
| 7 | `Submit_WithEmptyAgent_ShouldSetValidationMessage` | Leave Agent blank; assert `ValidationMessage == "Agent is required."`. |
| 8 | `Cancel_ShouldSetIsAcceptedFalseAndRaiseRequestClose` | Execute Cancel; assert `IsAccepted == false`, `RequestClose` fired with `false`. |
| 9 | `ToResult_AfterValidSubmit_ShouldReturnCorrectRecord` | Submit valid data; call `ToResult()`; assert all fields match VM properties. |

### 6.5 Desktop handler tests (24 handlers, 133 tests + sensitive-data redaction tests)

**File:** `tests/RemoteAgent.Desktop.UiTests/Handlers/` (one test class per handler)

**Logging note:** Individual handlers do NOT log entry/exit — the dispatcher handles this (Section 2.6). Handler tests do not need to assert logging. However, any request record that carries sensitive fields (`ApiKey`, `Password`, `Token`, `Secret`) MUST have a `ToString()` redaction test (see Section 6.5.31).

#### 6.5.1 `SetManagementSectionHandler` (2 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidSectionKey_ShouldReturnUnit` | Happy |
| 2 | `Handle_WithNullOrEmptySectionKey_ShouldReturnUnit` | Happy (handler is pass-through; validation is VM-side) |

#### 6.5.2 `ExpandStatusLogPanelHandler` (1 test)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_ShouldReturnUnit` | Happy |

#### 6.5.3 `OpenNewSessionHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenDialogAccepted_ShouldApplySettingsToWorkspaceAndReturnOk` | Happy |
| 2 | `Handle_WhenDialogAccepted_ShouldDispatchCreateDesktopSessionRequest` | Happy |
| 3 | `Handle_WhenDialogCancelled_ShouldReturnFailCancelled` | Fail |
| 4 | `Handle_WhenWorkspaceIsNull_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenCreateSessionDispatchFails_ShouldReturnFailWithInnerError` | Fail |

#### 6.5.4 `SaveServerRegistrationHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidHostAndPort_ShouldUpsertAndReturnOkWithRegistration` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenStoreThrows_ShouldReturnFail` | Fail |

#### 6.5.5 `RemoveServerRegistrationHandler` (3 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithExistingServerId_ShouldDeleteAndReturnOk` | Happy |
| 2 | `Handle_WhenStoreReturnsFalse_ShouldReturnFail` | Fail |
| 3 | `Handle_WithNullServerId_ShouldReturnFail` | Fail |

#### 6.5.6 `CheckLocalServerHandler` (3 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenServerRunning_ShouldReturnProbeWithIsRunningTrue` | Happy |
| 2 | `Handle_WhenServerStopped_ShouldReturnProbeWithIsRunningFalse` | Happy |
| 3 | `Handle_WhenProbeThrows_ShouldReturnFail` | Fail |

#### 6.5.7 `ApplyLocalServerActionHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenServerStopped_ShouldStartAndReturnOk` | Happy |
| 2 | `Handle_WhenServerRunning_ShouldStopAndReturnOk` | Happy |
| 3 | `Handle_WhenStartFails_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenStopFails_ShouldReturnFail` | Fail |

#### 6.5.8 `CreateDesktopSessionHandler` (6 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_ServerModeWithCapacityAvailable_ShouldCreateAndConnectSession` | Happy |
| 2 | `Handle_DirectMode_ShouldSkipCapacityCheckAndCreateSession` | Happy |
| 3 | `Handle_ServerModeCapacityExhausted_ShouldReturnFailWithReason` | Fail |
| 4 | `Handle_ServerModeCapacityReturnsNull_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenConnectThrows_ShouldReturnFailWithConnectionError` | Fail |
| 6 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |

#### 6.5.9 `CheckSessionCapacityHandler` (7 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenCapacityAvailable_ShouldReturnOkWithSnapshot` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiThrowsInvalidOperationException_ShouldReturnFailWithMessage` | Fail |
| 5 | `Handle_WhenApiThrowsHttpRequestException_ShouldReturnFail` | Fail |
| 6 | `Handle_WhenApiThrowsTaskCanceledException_ShouldReturnFailTimedOut` | Fail |
| 7 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |

#### 6.5.10 `RefreshOpenSessionsHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsSessions_ShouldReturnOkWithList` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.5.11 `TerminateOpenServerSessionHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullSession_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.12 `TerminateDesktopSessionHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenSessionConnected_ShouldStopDisconnectRemoveAndReturnOk` | Happy |
| 2 | `Handle_WhenSessionNotConnected_ShouldRemoveAndReturnOk` | Happy |
| 3 | `Handle_WithNullSession_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenStopThrows_ShouldStillDisconnectAndRemoveAndReturnOk` | Fail (graceful) |

#### 6.5.13 `SendDesktopMessageHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenConnectedWithText_ShouldSendAndAddToMessagesAndReturnOk` | Happy |
| 2 | `Handle_WhenNotConnected_ShouldAutoConnectThenSend` | Happy |
| 3 | `Handle_WithNullSession_ShouldReturnFail` | Fail |
| 4 | `Handle_WithEmptyMessage_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenSendThrows_ShouldReturnFail` | Fail |

#### 6.5.14 `RefreshSecurityDataHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenAllApiCallsSucceed_ShouldReturnOkWithCombinedData` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenAnyApiCallThrows_ShouldReturnFail` | Fail |

#### 6.5.15 `BanPeerHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullPeer_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.16 `UnbanPeerHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullPeer_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.17 `RefreshAuthUsersHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsUsersAndRoles_ShouldReturnOkWithData` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.5.18 `SaveAuthUserHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidUser_ShouldUpsertAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |

#### 6.5.19 `DeleteAuthUserHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullUser_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.20 `RefreshPluginsHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsSnapshot_ShouldReturnOkWithAssembliesAndRunnerIds` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.5.21 `SavePluginsHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidAssemblies_ShouldUpdateAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |

#### 6.5.22 `RefreshMcpRegistryHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsServersAndMapping_ShouldReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.5.23 `SaveMcpServerHandler` — desktop (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidDefinition_ShouldUpsertAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |

#### 6.5.24 `DeleteMcpServerHandler` — desktop (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullServer_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.25 `SaveAgentMcpMappingHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidIds_ShouldSetAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.26 `RefreshPromptTemplatesHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTemplates_ShouldReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.5.27 `SavePromptTemplateHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidTemplate_ShouldUpsertAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |

#### 6.5.28 `DeletePromptTemplateHandler` (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsTrue_ShouldReturnOk` | Happy |
| 2 | `Handle_WithNullTemplate_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.29 `SeedSessionContextHandler` (6 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidSessionIdAndContent_ShouldSeedAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WithEmptySessionId_ShouldReturnFail` | Fail |
| 5 | `Handle_WithEmptyContent_ShouldReturnFail` | Fail |
| 6 | `Handle_WhenApiReturnsFalse_ShouldReturnFail` | Fail |

#### 6.5.30 `StartLogMonitoringHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidHostAndPort_ShouldFetchSnapshotAndReturnOkWithOffset` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenSnapshotApiThrows_ShouldReturnFail` | Fail |

#### 6.5.31 Request `ToString()` sensitive-data redaction tests

**File:** `tests/RemoteAgent.App.Tests/Cqrs/RequestRedactionTests.cs`

Every request record that carries `ApiKey`, `Password`, `Token`, or `Secret` fields MUST override `ToString()` to redact those values (see Section 2.6). These tests verify that the dispatcher's Debug log output never leaks credentials.

| # | Test | Description |
|---|------|-------------|
| 1 | `RefreshOpenSessionsRequest_ToString_ShouldRedactApiKey` | Create with `ApiKey="real-key"`; assert `ToString()` contains `"***"` and does NOT contain `"real-key"`. |
| 2 | `CheckSessionCapacityRequest_ToString_ShouldRedactApiKey` | Same pattern. |
| 3 | `CreateDesktopSessionRequest_ToString_ShouldRedactApiKey` | Same pattern. |
| 4 | `ConnectMobileSessionRequest_ToString_ShouldRedactApiKey` | Same pattern. |
| 5 | `AllRequestsWithApiKey_ToString_ShouldNotContainRawValue` | Reflectively discover all `IRequest<>` implementations with an `ApiKey` property; instantiate each; assert `ToString()` does not contain the raw value. |

The reflective test (#5) serves as a safety net — if a new request record with `ApiKey` is added but `ToString()` is not overridden, this test fails.

#### 6.5.32 CorrelationId contract tests

**File:** `tests/RemoteAgent.App.Tests/Cqrs/CorrelationIdTests.cs`

These tests verify the CorrelationId convention is upheld across all request types and that multi-step handlers correctly propagate the CorrelationId to child requests.

| # | Test | Description |
|---|------|-------------|
| 1 | `AllRequestTypes_ShouldHaveCorrelationIdAsFirstConstructorParameter` | Reflectively discover all `IRequest<>` implementations; assert each has a constructor whose first parameter is `Guid CorrelationId`. |
| 2 | `AllRequestTypes_ToString_ShouldIncludeCorrelationId` | Reflectively instantiate each request with a known `Guid`; assert `ToString()` output contains that Guid value. |
| 3 | `OpenNewSessionHandler_ShouldPropagateCorrelationIdToCreateDesktopSessionRequest` | Mock dispatcher captures dispatched requests; invoke handler with known CorrelationId; assert child `CreateDesktopSessionRequest.CorrelationId` matches parent. |
| 4 | `CreateDesktopSessionHandler_ShouldPropagateCorrelationIdToCheckSessionCapacityRequest` | Same pattern — assert child capacity request carries parent's CorrelationId. |
| 5 | `ConnectMobileSessionHandler_ShouldPropagateCorrelationIdToCreateMobileSessionRequest` | Same pattern — when no current session, handler creates one and passes CorrelationId. |
| 6 | `UsePromptTemplateHandler_ShouldPropagateCorrelationIdToSendMobileMessageRequest` | Same pattern — after template rendering, child send request carries parent's CorrelationId. |
| 7 | `BanPeerHandler_ShouldPropagateCorrelationIdToRefreshSecurityDataRequest` | Same pattern — after ban API call, refresh request carries parent's CorrelationId. |
| 8 | `UnbanPeerHandler_ShouldPropagateCorrelationIdToRefreshSecurityDataRequest` | Same pattern — after unban API call, refresh request carries parent's CorrelationId. |

Tests #3–8 use a `CapturingRequestDispatcher` that records all dispatched requests, enabling assertion of CorrelationId propagation without executing the child handler.

```csharp
internal sealed class CapturingRequestDispatcher : IRequestDispatcher
{
    public List<object> DispatchedRequests { get; } = new();

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        DispatchedRequests.Add(request);
        return Task.FromResult(default(TResponse)!);
    }
}
```

### 6.6 Mobile handler tests (14 unique handlers, 65 tests + sensitive-data redaction tests)

**File:** `tests/RemoteAgent.App.Tests/Handlers/` (one test class per handler)

**Logging note:** Same as desktop — handlers do not log; dispatcher handles it. Mobile request records with sensitive fields share the same redaction test class (`RequestRedactionTests.cs`) since request records live in shared code.

#### 6.6.1 `ConnectMobileSessionHandler` (12 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_ServerModeWithCapacityAvailable_ShouldConnectAndReturnOk` | Happy |
| 2 | `Handle_DirectMode_ShouldSkipServerInfoAndCapacityAndConnect` | Happy |
| 3 | `Handle_NoCurrentSession_ShouldCreateNewSessionThenConnect` | Happy |
| 4 | `Handle_SessionAlreadyHasAgentId_ShouldSkipAgentSelection` | Happy |
| 5 | `Handle_WhenModeSelectionCancelled_ShouldReturnFailCancelled` | Fail |
| 6 | `Handle_WhenEmptyHostInServerMode_ShouldReturnFail` | Fail |
| 7 | `Handle_WhenInvalidPort_ShouldReturnFail` | Fail |
| 8 | `Handle_WhenServerInfoUnreachable_ShouldReturnFail` | Fail |
| 9 | `Handle_WhenAgentSelectionCancelled_ShouldReturnFailCancelled` | Fail |
| 10 | `Handle_WhenCapacityExhausted_ShouldReturnFailWithReason` | Fail |
| 11 | `Handle_WhenCapacityReturnsNull_ShouldReturnFail` | Fail |
| 12 | `Handle_WhenConnectThrows_ShouldReturnFailWithExceptionMessage` | Fail |

#### 6.6.2 `DisconnectMobileSessionHandler` (2 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenConnected_ShouldDisconnectAndReturnOk` | Happy |
| 2 | `Handle_WhenAlreadyDisconnected_ShouldReturnOkNoOp` | Happy |

#### 6.6.3 `CreateMobileSessionHandler` (2 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_ShouldCreateSessionWithNewIdAndAddToStoreAndReturnOk` | Happy |
| 2 | `Handle_ShouldSetDefaultTitleAndServerMode` | Happy |

#### 6.6.4 `TerminateMobileSessionHandler` (6 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenConfirmedAndConnected_ShouldStopDisconnectDeleteAndReturnOk` | Happy |
| 2 | `Handle_WhenConfirmedAndNotConnected_ShouldDeleteAndReturnOk` | Happy |
| 3 | `Handle_WhenConfirmedNonCurrentSession_ShouldDeleteWithoutDisconnect` | Happy |
| 4 | `Handle_WithNullSession_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenConfirmationRejected_ShouldReturnFailCancelled` | Fail |
| 6 | `Handle_WhenStopThrows_ShouldStillDisconnectAndDeleteAndReturnOk` | Fail (graceful) |

#### 6.6.5 `SendMobileMessageHandler` (6 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenConnectedWithText_ShouldSendTextAndAddUserMessageAndReturnOk` | Happy |
| 2 | `Handle_WhenTextMatchesScriptPattern_ShouldSendAsScriptRequest` | Happy |
| 3 | `Handle_WhenFirstMessage_ShouldAutoSetSessionTitleToFirst60Chars` | Happy |
| 4 | `Handle_WhenEmptyText_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenNotConnected_ShouldReturnFail` | Fail |
| 6 | `Handle_WhenSendThrows_ShouldReturnFailAndAddErrorMessage` | Fail |

#### 6.6.6 `SendMobileAttachmentHandler` (4 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenConnectedAndFilePicked_ShouldSendMediaAndReturnOk` | Happy |
| 2 | `Handle_WhenNotConnected_ShouldReturnFail` | Fail |
| 3 | `Handle_WhenPickerCancelled_ShouldReturnFailCancelled` | Fail |
| 4 | `Handle_WhenSendThrows_ShouldReturnFail` | Fail |

#### 6.6.7 `ArchiveMessageHandler` (2 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidMessage_ShouldSetIsArchivedAndCallStoreAndReturnOk` | Happy |
| 2 | `Handle_WithNullMessage_ShouldReturnFail` | Fail |

#### 6.6.8 `UsePromptTemplateHandler` (7 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenTemplateSelectedAndAllVariablesProvided_ShouldRenderAndSendAndReturnOk` | Happy |
| 2 | `Handle_WhenSingleTemplateAvailable_ShouldAutoSelectWithoutPrompting` | Happy |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenNoTemplatesAvailable_ShouldReturnFail` | Fail |
| 6 | `Handle_WhenTemplateSelectionCancelled_ShouldReturnFailCancelled` | Fail |
| 7 | `Handle_WhenVariableInputCancelled_ShouldReturnFailCancelled` | Fail |

#### 6.6.9 `NotifyMessageHandler` (2 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenNotifyPriority_ShouldCallNotificationServiceAndReturnOk` | Happy |
| 2 | `Handle_WhenNonNotifyPriority_ShouldNotCallServiceAndReturnOk` | Happy |

#### 6.6.10 `LoadMcpServersHandler` — mobile (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WhenApiReturnsServers_ShouldReturnOkWithOrderedList` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiThrows_ShouldReturnFail` | Fail |

#### 6.6.11 `SaveMcpServerHandler` — mobile (5 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidServer_ShouldUpsertAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 3 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 4 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiResponseSuccessIsFalse_ShouldReturnFailWithMessage` | Fail |

#### 6.6.12 `DeleteMcpServerHandler` — mobile (6 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidServerId_ShouldDeleteAndReturnOk` | Happy |
| 2 | `Handle_WithEmptyServerId_ShouldReturnFail` | Fail |
| 3 | `Handle_WithEmptyHost_ShouldReturnFail` | Fail |
| 4 | `Handle_WithInvalidPort_ShouldReturnFail` | Fail |
| 5 | `Handle_WhenApiReturnsNull_ShouldReturnFail` | Fail |
| 6 | `Handle_WhenApiResponseSuccessIsFalse_ShouldReturnFail` | Fail |

#### 6.6.13 `SelectMobileSessionHandler` (3 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidSessionId_ShouldSelectAndReturnOk` | Happy |
| 2 | `Handle_WithNullOrEmptySessionId_ShouldReturnFail` | Fail |
| 3 | `Handle_WhenSessionIdNotFound_ShouldReturnFail` | Fail |

#### 6.6.14 `NavigateToRouteHandler` (3 tests)

| # | Test | Happy/Fail |
|---|------|-----------|
| 1 | `Handle_WithValidRoute_ShouldNavigateCloseFlyoutAndReturnOk` | Happy |
| 2 | `Handle_WithNullOrEmptyRoute_ShouldReturnFail` | Fail |
| 3 | `Handle_WhenNavigationThrows_ShouldReturnFail` | Fail |

### 6.7 UI tests with mocked handlers (TR-18.4)

#### Desktop (5 tests)

**File:** `tests/RemoteAgent.Desktop.UiTests/CqrsMockDispatcherUiTests.cs`

Uses `MockRequestDispatcher`:

```csharp
private sealed class MockRequestDispatcher : IRequestDispatcher
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();

    public void Setup<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = (req, ct) => Task.FromResult<object>(handler((TRequest)req)!);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        if (_handlers.TryGetValue(request.GetType(), out var handler))
            return (TResponse)(await handler(request, ct));
        return default!;
    }
}
```

| # | Test | Description |
|---|------|-------------|
| 1 | `SetManagementSection_ViaDispatcher_ShouldUpdateSelectedSection` | Mock handler returns Unit; assert VM's `SelectedManagementSection` updated. |
| 2 | `StartSession_WhenMockHandlerReturnsOk_ShouldUpdateStatusText` | Mock `OpenNewSessionHandler` returns Ok; assert `StatusText` reflects success. |
| 3 | `StartSession_WhenMockHandlerReturnsFail_ShouldDisplayErrorInStatusText` | Mock returns Fail("No capacity"); assert `StatusText` contains error. |
| 4 | `RefreshOpenSessions_WhenMockReturnsData_ShouldPopulateCollection` | Mock returns session list; assert `OpenServerSessions` collection populated. |
| 5 | `CheckLocalServer_WhenMockReturnsRunning_ShouldUpdateActionLabel` | Mock returns probe with `IsRunning=true`; assert `LocalServerActionLabel` is "Stop Local Server". |

#### Mobile (3 tests)

**File:** `tests/RemoteAgent.Mobile.UiTests/CqrsMockDispatcherMobileTests.cs` (or added to existing test class if Appium available)

| # | Test | Description |
|---|------|-------------|
| 1 | `Connect_WhenMockHandlerReturnsOk_ShouldShowConnectedStatus` | Mock `ConnectMobileSessionHandler` returns Ok; assert status label shows "Connected". |
| 2 | `Connect_WhenMockHandlerReturnsFail_ShouldShowErrorStatus` | Mock returns Fail; assert status label shows error message. |
| 3 | `SendMessage_WhenMockHandlerReturnsOk_ShouldClearPendingMessage` | Mock `SendMobileMessageHandler` returns Ok; assert pending message cleared. |

### 6.8 Tasks — Phase 3

1. Create `CapturingLogger<T>` and `CapturingRequestDispatcher` test helpers in shared test infrastructure.
2. Create `tests/RemoteAgent.App.Tests/Cqrs/ServiceProviderRequestDispatcherTests.cs` (9 tests — 4 functional + 1 CorrelationId validation + 4 logging with CorrelationId).
3. Create `tests/RemoteAgent.App.Tests/Cqrs/RequestRedactionTests.cs` (5 tests — per-request redaction + reflective safety net).
4. Create `tests/RemoteAgent.App.Tests/Cqrs/CorrelationIdTests.cs` (8 tests — 2 reflective contract tests + 6 propagation tests for multi-step handlers).
5. Create `tests/RemoteAgent.Desktop.UiTests/ConnectionSettingsDialogViewModelTests.cs` (9 tests).
6. Create desktop handler test classes — one per handler in `tests/RemoteAgent.Desktop.UiTests/Handlers/` (133 tests across 24 classes).
7. Create mobile handler test classes — one per handler in `tests/RemoteAgent.App.Tests/Handlers/` (65 tests across 14 classes).
8. Create shared mock types: `MockRequestDispatcher`, `MockServerCapacityClient`, `MockLocalServerManager`, `MockConnectionSettingsDialogService`, `MockServerApiClient`, `MockAgentGatewayClient`, `MockSessionStore`, `MockAppPreferences`, and 7 mock platform UI abstractions.
9. Create desktop UI tests with mock dispatcher in `tests/RemoteAgent.Desktop.UiTests/CqrsMockDispatcherUiTests.cs` (5 tests).
10. Create mobile UI tests with mock dispatcher (3 tests) — skippable if Appium not configured.
11. Ensure no `NoWarn` or suppressions for new code; fix nullability and style per AGENTS.md.
12. Run full test suite; fix failures.

**Exit criteria:** ~237 new tests pass; every handler has exhaustive happy-path and failure-path coverage; dispatcher entry/exit logging with CorrelationId verified; CorrelationId propagation verified for all multi-step handlers; sensitive-data redaction verified; UI tests validate key flows with mocked dispatchers; zero new warnings.

---

## 7. Phase 4: Full validation and cleanup

**Goal:** Full build/test, documentation update, final pass.

**Estimated effort:** 1–2 sessions.

### 7.1 Build and test

- `./scripts/build-dotnet10.sh Release` (MAUI + service).
- `./scripts/build-desktop-dotnet9.sh Release` (Avalonia).
- `./scripts/test-integration.sh Release` (service integration tests — should be unaffected by UI refactor but verify no regressions).
- Fix any regressions; address flaky UI tests if needed.

### 7.2 Documentation and matrix

- Update `docs/requirements-completion-summary.md`: mark TR-18.1, TR-18.2, TR-18.3, TR-18.4 as **Done** with notes referencing the CQRS types, handler test project, and mock-dispatcher UI tests.
- Update or replace session handoff file with a "MVVM/CQRS refactor complete" note.

### 7.3 Final code-behind audit

Grep for code-behind violations across all `.xaml.cs` files. Verify each remaining method is either:
- Constructor (`InitializeComponent`, `DataContext`, `BindingContext`)
- View-adapter (documented with `// View-adapter:` comment)
- MAUI lifecycle (`OnAppearing` that invokes a VM command)

### 7.4 CQRS logging and redaction audit

1. **Verify dispatcher logging is active:** Set log level to `Debug` in test configuration; run a request end-to-end; confirm entry and exit messages appear in log output.
2. **Verify sensitive-data redaction:** Review every `IRequest<>` implementation. Any record with properties named `ApiKey`, `Password`, `Token`, or `Secret` MUST override `ToString()` with redacted output. The reflective test in `RequestRedactionTests.cs` catches omissions, but manual review provides defense-in-depth.
3. **Verify log format consistency:** All entry logs must match `"CQRS Enter {RequestType}: {Request}"` and all exit logs must match `"CQRS Leave {RequestType}: {Result}"` (or `"with exception"` variant). No handler should contain its own entry/exit logging — that is the dispatcher's responsibility.

### 7.5 Tasks — Phase 4

1. Run full build scripts and fix failures.
2. Run integration tests and fix failures.
3. Final code-behind audit (grep + manual review).
4. CQRS logging and redaction audit (Section 7.4).
5. Update requirements completion matrix.
6. Create summary note in docs.

**Exit criteria:** All builds and tests pass; TR-18.x reflected as complete; no business logic in code-behind; every remaining code-behind line documented as view-adapter; dispatcher entry/exit logging verified end-to-end; no sensitive data leaked in log output.

---

## 8. Dependency order

```
Phase 0 (CQRS foundation, ~1 session)
    → Phase 1 (Desktop, ~3-4 sessions)
        → Phase 3a (Desktop tests — 133 handler + 9 dialog VM + 5 UI = 147, ~3-4 sessions)
    → Phase 2 (Mobile, ~4-6 sessions)  [starts after Phase 0, not parallel with Phase 1]
        → Phase 3b (Mobile tests — 65 handler + 3 UI = 68, ~2-3 sessions)
    → Phase 4 (validation, ~1-2 sessions)  [after 1, 2, 3a, 3b all complete]
```

**Why NOT parallel Phase 1 and Phase 2:**
- Phase 0 establishes the CQRS interfaces, but the real patterns (error handling, DI scoping, thread model) are only proven during Phase 1.
- The Desktop project is simpler (fewer platform abstractions needed) and makes a better proving ground.
- Patterns discovered in Phase 1 (e.g., how dialog services work, how sub-VMs decompose, how DI scoping resolves) directly inform Phase 2's mobile approach.
- Running Phase 2 after Phase 1 avoids interface churn: shared types in `App.Logic` stabilize before mobile handlers consume them.

**Recommended sequence:** **0 → 1 → 3a → 2 → 3b → 4**.

**Total estimated effort:** 15–24 sessions depending on complexity (increased from prior estimate to account for exhaustive test coverage).

---

## 9. Incremental migration strategy

### 9.1 Coexistence during partial migration

During Phase 1 and Phase 2, old code-behind and new CQRS patterns coexist temporarily:

- Commands are migrated one at a time. A VM can have some commands that dispatch requests and some that still call methods directly.
- Each migrated command is tested individually before moving to the next.
- XAML bindings are updated per-command. Event handlers are removed only after their replacement command binding is confirmed working.

### 9.2 Git branching

- Create a long-lived feature branch `feature/cqrs-refactor` from `develop`.
- Each phase gets sub-branches: `feature/cqrs-refactor/phase-0`, `feature/cqrs-refactor/phase-1`, etc.
- Phase sub-branches are merged into the feature branch upon phase completion.
- The feature branch is merged into `develop` only after Phase 4 completes and all tests pass.

### 9.3 Rollback

- Each phase sub-branch can be abandoned without affecting `develop`.
- If Phase 1 proves the CQRS pattern is wrong or too costly, the approach can be re-evaluated before starting Phase 2.
- The feature branch is never force-pushed to `develop`.

---

## 10. Risk and mitigation

| Risk | Severity | Mitigation |
|------|----------|------------|
| CQRS scope is large (~41 handlers) and effort overruns | High | Strict priority ordering: migrate API-calling commands first (highest testability gain); trivial commands last. Accept phased delivery. |
| Avalonia behavior limitations (NavigationView, PointerPressed) | Medium | Custom attached behaviors (specified in 3.3, 3.4). If blocked, keep single-line code-behind call to VM command (view-adapter). |
| Dialog-from-handler coupling to `Window` reference | Medium | `IConnectionSettingsDialogService` abstraction (specified in 4.2). Handler stays unit-testable; test mock returns fixed result. |
| Shell/MainPage coupling (Mobile) replacement complexity | High | `ISessionCommandBus` pattern (specified in 5.2). Simple interface; no messaging framework dependency. |
| `ServerWorkspaceViewModel` decomposition breaks existing UI | High | Decompose incrementally (one sub-VM at a time). Run UI tests after each extraction. Keep parent VM as coordinator. |
| DI scoping conflicts for server-workspace handlers | Medium | Handlers registered as transient; dispatcher resolved per scope (specified in 4.7). Proven in Phase 0/1 before Phase 2. |
| Static `ServerApiClient` / `AgentGatewayClientService` refactoring breaks callers | Medium | Add interface; make existing class implement it; change only constructor parameter types. Behavioral change is zero. |
| Thread-marshaling bugs with ObservableCollection | Medium | Handlers return data, VMs apply on UI thread via `await` (specified in 2.6). No `Dispatcher.UIThread.Post` in handlers. |
| Test count (~100) takes longer than expected | Medium | Write tests incrementally during Phase 1/2 (test each handler as it's written). Phase 3 is for filling remaining gaps and UI-test updates. |
| Interface churn if Phase 1 + 2 run in parallel | Medium | Mitigated by sequential execution: Phase 1 stabilizes interfaces before Phase 2 starts. |

---

## 11. File checklist (summary)

### New in `RemoteAgent.App.Logic`

- `Cqrs/IRequest.cs`
- `Cqrs/IRequestHandler.cs`
- `Cqrs/IRequestDispatcher.cs`
- `Cqrs/ServiceProviderRequestDispatcher.cs`
- `Cqrs/Unit.cs`
- `Cqrs/CommandResult.cs`
- `IServerApiClient.cs` (interface wrapping `ServerApiClient` static methods)

### New/updated in Desktop

- `Behaviors/NavigationViewItemInvokedBehavior.cs`
- `Behaviors/DoubleTapBehavior.cs`
- `Infrastructure/IConnectionSettingsDialogService.cs` + `AvaloniaConnectionSettingsDialogService.cs`
- `Infrastructure/IStructuredLogClient.cs` + `ServerApiClientStructuredLogAdapter.cs`
- `ViewModels/ConnectionSettingsDialogViewModel.cs`
- `ViewModels/SessionManagementViewModel.cs`
- `ViewModels/OpenServerSessionsViewModel.cs`
- `ViewModels/SecurityViewModel.cs`
- `ViewModels/AuthUsersViewModel.cs`
- `ViewModels/PluginsViewModel.cs`
- `ViewModels/McpRegistryViewModel.cs` (desktop)
- `ViewModels/PromptTemplatesViewModel.cs`
- `ViewModels/StructuredLogsViewModel.cs`
- `Requests/` — ~24 request record files
- `Handlers/` — ~24 handler files
- Updated: `MainWindowViewModel.cs` (takes `IRequestDispatcher`, exposes new commands, delegates to sub-VMs)
- Updated: `ServerWorkspaceViewModel.cs` (decomposed, delegates to sub-VMs)
- Updated: `MainWindow.axaml` (behavior bindings replace event handlers)
- Updated: `MainWindow.axaml.cs` (only constructor + view-adapter)
- Updated: `ConnectionSettingsDialog.axaml` (all bindings, no x:Name)
- Updated: `ConnectionSettingsDialog.axaml.cs` (only constructor + RequestClose)
- Updated: `App.axaml.cs` (DI registrations for all handlers, dispatcher, services)

### New/updated in Mobile (App)

- `Infrastructure/IAgentGatewayClient.cs` (interface)
- `Infrastructure/IAppPreferences.cs` + `MauiAppPreferences.cs`
- `Infrastructure/IConnectionModeSelector.cs` + `MauiConnectionModeSelector.cs`
- `Infrastructure/IAgentSelector.cs` + `MauiAgentSelector.cs`
- `Infrastructure/IAttachmentPicker.cs` + `MauiAttachmentPicker.cs`
- `Infrastructure/IPromptTemplateSelector.cs` + `MauiPromptTemplateSelector.cs`
- `Infrastructure/IPromptVariableProvider.cs` + `MauiPromptVariableProvider.cs`
- `Infrastructure/ISessionTerminationConfirmation.cs` + `MauiSessionTerminationConfirmation.cs`
- `Infrastructure/INotificationService.cs` + `PlatformNotificationServiceAdapter.cs`
- `Infrastructure/ISessionCommandBus.cs`
- `Infrastructure/INavigationService.cs` + `MauiNavigationService.cs`
- `ViewModels/McpRegistryPageViewModel.cs`
- `ViewModels/AppShellViewModel.cs`
- `Requests/` — ~17 request record files
- `Handlers/` — ~17 handler files
- Updated: `MainPageViewModel.cs` (takes `IRequestDispatcher`; removes delegates; adds title-editing VM state)
- Updated: `MainPage.xaml.cs` (only constructor + Windows keyboard hook)
- Updated: `McpRegistryPage.xaml.cs` (only constructor + OnAppearing trigger)
- Updated: `AppShell.xaml.cs` (only constructor + ShellContent registration)
- Updated: `MauiProgram.cs` (DI registrations)

### Tests (~237 new tests)

- `tests/RemoteAgent.App.Tests/Cqrs/ServiceProviderRequestDispatcherTests.cs` — 9 tests (4 functional + 1 CorrelationId validation + 4 logging with CorrelationId)
- `tests/RemoteAgent.App.Tests/Cqrs/RequestRedactionTests.cs` — 5 tests (sensitive-data `ToString()` redaction verification + reflective safety net)
- `tests/RemoteAgent.App.Tests/Cqrs/CorrelationIdTests.cs` — 8 tests (2 reflective contract + 6 multi-step handler propagation)
- `tests/RemoteAgent.App.Tests/Cqrs/CapturingLogger.cs` — test helper for capturing `ILogger` entries
- `tests/RemoteAgent.App.Tests/Cqrs/CapturingRequestDispatcher.cs` — test helper for capturing dispatched requests (CorrelationId propagation verification)
- `tests/RemoteAgent.Desktop.UiTests/ConnectionSettingsDialogViewModelTests.cs` — 9 dialog VM tests
- `tests/RemoteAgent.Desktop.UiTests/Handlers/` — 133 desktop handler tests (24 classes, exhaustive happy + failure paths)
- `tests/RemoteAgent.App.Tests/Handlers/` — 65 mobile handler tests (14 classes, exhaustive happy + failure paths)
- `tests/RemoteAgent.Desktop.UiTests/CqrsMockDispatcherUiTests.cs` — 5 desktop UI tests with mock dispatcher
- `tests/RemoteAgent.Mobile.UiTests/CqrsMockDispatcherMobileTests.cs` — 3 mobile UI tests with mock dispatcher
- Shared mock types: `MockRequestDispatcher`, `MockServerCapacityClient`, `MockLocalServerManager`, `MockConnectionSettingsDialogService`, `MockServerApiClient`, `MockAgentGatewayClient`, `MockSessionStore`, `MockAppPreferences`, + 7 platform UI abstraction mocks

### Docs

- Updated: `docs/requirements-completion-summary.md` (TR-18.x → Done)
- Updated: `docs/implementation-plan-todo.md` (granular checklist)
