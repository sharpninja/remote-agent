# Implementation Plan TODO (MVVM + CQRS Refactor)

Track progress for [implementation-plan-mvvm-cqrs-refactor.md](implementation-plan-mvvm-cqrs-refactor.md).

---

## Phase 0: Shared CQRS foundation (~1 session)

- [x] Add `IRequest<TResponse>` interface with required `Guid CorrelationId` property to `App.Logic/Cqrs/`
- [x] Add `IRequestHandler<TRequest, TResponse>` interface to `App.Logic/Cqrs/`
- [x] Add `IRequestDispatcher` interface to `App.Logic/Cqrs/`
- [x] Add `Unit` result type to `App.Logic/Cqrs/`
- [x] Add `CommandResult` and `CommandResult<T>` result types to `App.Logic/Cqrs/`
- [x] Implement `ServiceProviderRequestDispatcher` in `App.Logic/Cqrs/` with `ILogger` for Debug-level entry/exit logging including `[{CorrelationId}]` in every log message
- [x] Implement `Guid.Empty` CorrelationId validation in dispatcher (`ArgumentException` if empty)
- [x] Add `SetManagementSectionRequest(Guid CorrelationId, string SectionKey)` and `SetManagementSectionHandler`
- [x] Add `NavigationViewItemInvokedBehavior` attached behavior (Desktop)
- [x] Add `DoubleTapBehavior` attached behavior (Desktop)
- [x] Wire `IRequestDispatcher` into `MainWindowViewModel`; expose `SetManagementSectionCommand` that generates `Guid.NewGuid()` CorrelationId
- [x] Replace `ItemInvoked="OnManagementNavItemInvoked"` in XAML with behavior binding
- [x] Register dispatcher and handler in Desktop DI (`App.axaml.cs`)
- [x] Add unit tests for `ServiceProviderRequestDispatcher` (resolve, invoke, missing-handler, CancellationToken passthrough, CorrelationId validation)
- [x] Add unit tests for dispatcher Debug entry/exit logging with CorrelationId (entry log, success exit log, exception exit log, CorrelationId match)
- [x] Add unit test for `SetManagementSectionHandler`
- [x] Verify desktop build (`./scripts/build-desktop-dotnet9.sh Release`)
- [x] Verify existing desktop UI tests still pass

---

## Phase 1: Desktop code-behind removal (~3–4 sessions)

### 1a: ConnectionSettingsDialog refactor

- [x] Add `IConnectionSettingsDialogService` interface
- [x] Add `AvaloniaConnectionSettingsDialogService` implementation
- [x] Add `ConnectionSettingsDialogViewModel` with properties, validation, Submit/Cancel commands, `RequestClose` event
- [x] Refactor `ConnectionSettingsDialog.axaml` to use bindings (remove all `x:Name` control access)
- [x] Refactor `ConnectionSettingsDialog.axaml.cs` to constructor + `RequestClose` subscription only
- [x] Add `OpenNewSessionRequest` and `OpenNewSessionHandler`
- [x] Expose `StartSessionCommand` on `MainWindowViewModel`; bind in menu + toolbar XAML
- [x] Remove `OnStartSessionClick` from `MainWindow.axaml.cs`
- [x] Run desktop UI tests

### 1b: Remaining MainWindow code-behind

- [x] Bind `SetManagementSectionCommand` via behavior in XAML; remove `OnManagementNavItemInvoked`
- [x] Add `ExpandStatusLogPanelRequest` / handler; bind `ExpandStatusLogCommand` via `DoubleTapBehavior`; remove `OnStatusBarPointerPressed`
- [x] Document `OnOpened` / `TrySetOpenPaneLength` as view-adapter exception
- [x] Verify `MainWindow.axaml.cs` has only constructor + view-adapter

### 1c: MainWindowViewModel handlers

- [x] Add `SaveServerRegistrationRequest` / handler; wire `SaveServerCommand`
- [x] Add `RemoveServerRegistrationRequest` / handler; wire `RemoveServerCommand`
- [x] Add `CheckLocalServerRequest` / handler; wire `CheckLocalServerCommand`
- [x] Add `ApplyLocalServerActionRequest` / handler; wire `ApplyLocalServerActionCommand`

### 1d: ServerWorkspaceViewModel decomposition

- [x] Extract `SessionManagementViewModel` (NewSession, TerminateCurrent, TerminateSession, SendMessage, CheckCapacity)
- [x] Extract `OpenServerSessionsViewModel` (RefreshOpenSessions, TerminateOpenServerSession)
- [x] Extract `SecurityViewModel` (RefreshSecurityData, BanPeer, UnbanPeer)
- [x] Extract `AuthUsersViewModel` (RefreshAuthUsers, SaveAuthUser, DeleteAuthUser)
- [x] Extract `PluginsViewModel` (RefreshPlugins, SavePlugins)
- [x] Extract `McpRegistryViewModel` (desktop) (RefreshMcp, SaveMcpServer, DeleteMcpServer, SaveAgentMcpMapping)
- [x] Extract `PromptTemplatesViewModel` (RefreshPromptTemplates, SavePromptTemplate, DeletePromptTemplate, SeedContext)
- [x] Extract `StructuredLogsViewModel` (StartLogMonitoring, StopLogMonitoring, ApplyLogFilter, ClearLogFilter)
- [x] Update `MainWindow.axaml` DataContext paths for sub-VMs
- [x] Run desktop UI tests after each extraction

### 1e: Interface wrappers

- [x] Add `IStructuredLogClient` interface; implement wrapper for static `ServerApiClient` log methods
- [x] Register `IStructuredLogClient` in Desktop DI

### 1f: ServerWorkspaceViewModel CQRS handlers (24 total)

**Convention:** Every request record has `Guid CorrelationId` as its first parameter. VMs generate `Guid.NewGuid()` at the command boundary. Handlers that dispatch child requests MUST propagate `request.CorrelationId` (see Section 2.8 of the implementation plan).

- [x] `CreateDesktopSessionHandler` (replaces `NewSessionAsync`)
- [x] `CheckSessionCapacityHandler`
- [x] `RefreshOpenSessionsHandler`
- [x] `TerminateOpenServerSessionHandler`
- [x] `TerminateDesktopSessionHandler`
- [x] `SendDesktopMessageHandler`
- [x] `RefreshSecurityDataHandler`
- [x] `BanPeerHandler`
- [x] `UnbanPeerHandler`
- [x] `RefreshAuthUsersHandler`
- [x] `SaveAuthUserHandler`
- [x] `DeleteAuthUserHandler`
- [x] `RefreshPluginsHandler`
- [x] `SavePluginsHandler`
- [x] `RefreshMcpRegistryHandler`
- [x] `SaveMcpServerHandler` (desktop)
- [x] `DeleteMcpServerHandler` (desktop)
- [x] `SaveAgentMcpMappingHandler`
- [x] `RefreshPromptTemplatesHandler`
- [x] `SavePromptTemplateHandler`
- [x] `DeletePromptTemplateHandler`
- [x] `SeedSessionContextHandler`
- [x] `StartLogMonitoringHandler`
- [x] Register all handlers in Desktop DI *(22 of 26 registered; blocked on 4 missing handlers above)*

### 1g: Phase 1 validation

- [x] Delete all unused event handlers from `MainWindow.axaml.cs`
- [x] Full desktop build (`./scripts/build-desktop-dotnet9.sh Release`)
- [x] Run desktop UI tests; fix regressions

### 1h: Management App Log view (FR-12.12)

- [x] Create `AppLogEntry` record (`Timestamp`, `Level`, `Category`, `Message`, `Exception?`)
- [x] Create `IAppLogStore` interface with `Add(AppLogEntry)`, `GetAll()`, `Clear()` methods
- [x] Create `InMemoryAppLogStore` (thread-safe, bounded ring buffer or unbounded list)
- [x] Create `AppLoggerProvider` / `AppLoggerCategory` implementing `ILoggerProvider` + `ILogger`; each log call adds to `IAppLogStore`
- [x] Register `AppLoggerProvider` in `App.axaml.cs` via `ILoggerFactory.AddProvider(...)`
- [x] Create `AppLogViewModel` (ObservableCollection of visible entries, filter text, `ClearCommand`, `SaveCommand`)
- [x] `ClearCommand` → `ClearAppLogRequest` / `ClearAppLogHandler` (clears store, updates VM)
- [x] `SaveCommand` → `SaveAppLogRequest` / `SaveAppLogHandler` (exports to txt/json/csv via format selector)
- [x] Add Management App Log navigation item + panel to `MainWindow.axaml`
- [x] Wire `AppLogViewModel` into `MainWindowViewModel`; bind via CQRS dispatcher
- [x] Unit tests: `AppLoggerProvider_ShouldCaptureLogEntries`, `ClearAppLogHandler_ShouldEmptyCollection`, `SaveAppLogHandler_ShouldWriteAllThreeFormats`

---

## Phase 2: Mobile code-behind removal (~4–6 sessions)

### 2a: Interface wrappers for mobile dependencies

- [x] Add `IAgentGatewayClient` interface; make `AgentGatewayClientService` implement it
- [x] Change `MainPageViewModel` constructor to accept `IAgentGatewayClient` (not concrete)
- [x] Add `IServerApiClient` interface in `App.Logic`; implement wrapper in `App`
- [x] Add `IAppPreferences` interface + `MauiAppPreferences` implementation
- [x] Register all interface wrappers in `MauiProgram.cs`

### 2b: Platform UI abstractions (7 interfaces + 7 implementations)

- [x] `IConnectionModeSelector` + `MauiConnectionModeSelector`
- [x] `IAgentSelector` + `MauiAgentSelector`
- [x] `IAttachmentPicker` + `MauiAttachmentPicker`
- [x] `IPromptTemplateSelector` + `MauiPromptTemplateSelector`
- [x] `IPromptVariableProvider` + `MauiPromptVariableProvider`
- [x] `ISessionTerminationConfirmation` + `MauiSessionTerminationConfirmation`
- [x] `INotificationService` + `PlatformNotificationServiceAdapter`
- [x] Register all platform abstractions in `MauiProgram.cs`

### 2c: Session command bus and navigation

- [x] Add `ISessionCommandBus` interface
- [x] Make `MainPageViewModel` implement `ISessionCommandBus`
- [x] Add `INavigationService` interface + `MauiNavigationService` implementation
- [x] Register in `MauiProgram.cs`

### 2d: MainPageViewModel CQRS handlers (9 total)

**Convention:** Every request record has `Guid CorrelationId` as its first parameter. VMs generate `Guid.NewGuid()` at the command boundary. Handlers that dispatch child requests MUST propagate `request.CorrelationId` (see Section 2.8 of the implementation plan).

- [x] `ConnectMobileSessionHandler`
- [x] `DisconnectMobileSessionHandler`
- [x] `CreateMobileSessionHandler`
- [x] `TerminateMobileSessionHandler`
- [x] `SendMobileMessageHandler`
- [x] `SendMobileAttachmentHandler`
- [x] `ArchiveMessageHandler`
- [x] `UsePromptTemplateHandler`
- [x] `NotifyMessageHandler` (or wire into gateway event)
- [x] Remove all delegate properties from `MainPageViewModel`
- [x] Add `IsEditingTitle` / `BeginEditTitleCommand` / `CommitTitleCommand` to VM

### 2e: MainPage code-behind cleanup

- [x] Remove delegate assignments from `MainPage.xaml.cs`
- [x] Remove `ShowNotificationForMessage` event subscription
- [x] Remove session title focus/tap/unfocus handlers (replaced by VM + behaviors)
- [x] Remove `StartNewSessionFromShell`, `SelectSessionFromShell`, `TerminateSessionFromShellAsync`, `GetCurrentSessionId`
- [x] Keep only constructor (`BindingContext = _vm`) and Windows keyboard hook (view-adapter)

### 2f: McpRegistryPage refactor

- [x] Add `McpRegistryPageViewModel` (Host, Port, Servers, form fields, commands)
- [x] Add `LoadMcpServersHandler` (mobile)
- [x] Add `SaveMcpServerHandler` (mobile)
- [x] Add `DeleteMcpServerHandler` (mobile)
- [x] Bind `McpRegistryPage.xaml` to VM; remove all logic from code-behind
- [x] Register VM and handlers in `MauiProgram.cs`

### 2g: AppShell refactor

- [x] Add `AppShellViewModel` (SessionItems, StartSessionCommand, SelectSessionCommand, TerminateSessionCommand, navigation commands)
- [x] Add `SelectMobileSessionRequest` / handler
- [x] Add `NavigateToRouteRequest` / handler
- [x] Refactor `AppShell.xaml` to use BindableLayout for session list
- [x] Remove `BuildSessionButtons` and all flyout click handlers from code-behind
- [x] Keep only constructor + ShellContent registration in code-behind

### 2h: Phase 2 validation

- [x] Full mobile build (`./scripts/build-dotnet10.sh Release`)
- [x] Run mobile UI tests; fix regressions

---

## Phase 3: Test coverage (~4–6 sessions, ~237 tests)

All CQRS commands and queries must have exhaustive unit tests with known happy paths and known failure paths. All CQRS commands and queries must write Debug-level log messages upon entering and leaving the command/query, including parameters, results, and CorrelationId. Every request must carry a required CorrelationId that propagates through handler chains. See implementation plan Section 6 for the full test matrix.

### 3a: Infrastructure tests (9 tests)

- [x] `ServiceProviderRequestDispatcher`: registered handler resolves and returns result
- [x] `ServiceProviderRequestDispatcher`: no handler registered → throws `InvalidOperationException`
- [x] `ServiceProviderRequestDispatcher`: passes `CancellationToken` through to handler
- [x] `ServiceProviderRequestDispatcher`: handler throws → exception propagates
- [x] `ServiceProviderRequestDispatcher`: `Guid.Empty` CorrelationId → throws `ArgumentException`
- [x] `ServiceProviderRequestDispatcher`: logs Debug entry with `"CQRS Enter"`, request type name, CorrelationId, and parameter values
- [x] `ServiceProviderRequestDispatcher`: logs Debug exit with `"CQRS Leave"`, request type name, CorrelationId, and result on success
- [x] `ServiceProviderRequestDispatcher`: logs Debug exit with `"CQRS Leave"`, request type name, CorrelationId, and exception message on handler failure
- [x] `ServiceProviderRequestDispatcher`: CorrelationId value in entry log matches request's `CorrelationId` exactly

### 3b: `ConnectionSettingsDialogViewModel` tests (9 tests)

- [x] Submit with valid host, port, mode, agent → `IsAccepted=true`, `RequestClose` fires
- [x] Submit with empty host → `ValidationMessage="Host is required."`, does not close
- [x] Submit with port "0" → `ValidationMessage="Port must be 1-65535."`
- [x] Submit with port "99999" → same validation message
- [x] Submit with non-numeric port → same validation message
- [x] Submit with empty mode → `ValidationMessage="Mode is required."`
- [x] Submit with empty agent → `ValidationMessage="Agent is required."`
- [x] Cancel → `IsAccepted=false`, `RequestClose` fires with `false`
- [x] `ToResult()` after valid submit → returns record matching VM properties

### 3c: Desktop handler tests (133 tests across 24 handlers)

#### `SetManagementSectionHandler` (2 tests)
- [x] Valid section key → returns `Unit`
- [x] Null/empty section key → returns `Unit` (pass-through)

#### `ExpandStatusLogPanelHandler` (1 test)
- [x] Returns `Unit`

#### `OpenNewSessionHandler` (5 tests)
- [x] Dialog accepted → applies settings to workspace, returns Ok
- [x] Dialog accepted → dispatches `CreateDesktopSessionRequest`
- [x] Dialog cancelled (returns null) → returns Fail("Cancelled")
- [x] Workspace is null → returns Fail
- [x] `CreateDesktopSession` dispatch fails → returns Fail with inner error

#### `SaveServerRegistrationHandler` (4 tests)
- [x] Valid host + port → upserts to store, returns Ok with registration
- [x] Empty host → returns Fail("Host required")
- [x] Invalid port (0, >65535, non-numeric) → returns Fail
- [x] Store throws → returns Fail

#### `RemoveServerRegistrationHandler` (3 tests)
- [x] Existing server ID → deletes, returns Ok
- [x] Store returns false → returns Fail
- [x] Null server ID → returns Fail

#### `CheckLocalServerHandler` (3 tests)
- [x] Server running → returns probe with `IsRunning=true`
- [x] Server stopped → returns probe with `IsRunning=false`
- [x] Probe throws → returns Fail

#### `ApplyLocalServerActionHandler` (4 tests)
- [x] Server stopped → starts, re-probes, returns Ok
- [x] Server running → stops, re-probes, returns Ok
- [x] Start fails → returns Fail
- [x] Stop fails → returns Fail

#### `CreateDesktopSessionHandler` (6 tests)
- [x] Server mode + capacity available → creates session, connects, returns Ok
- [x] Direct mode → skips capacity check, creates, connects, returns Ok
- [x] Server mode + capacity exhausted → returns Fail with reason
- [x] Capacity returns null → returns Fail
- [x] Connect throws → returns Fail with connection error
- [x] Invalid port → returns Fail

#### `CheckSessionCapacityHandler` (7 tests)
- [x] Capacity available → returns Ok with snapshot
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API throws `InvalidOperationException` → returns Fail with message
- [x] API throws `HttpRequestException` → returns Fail
- [x] API throws `TaskCanceledException` → returns Fail("timed out")
- [x] API returns null → returns Fail

#### `RefreshOpenSessionsHandler` (4 tests)
- [x] API returns sessions → returns Ok with list
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API throws → returns Fail

#### `TerminateOpenServerSessionHandler` (5 tests)
- [x] API returns true → returns Ok
- [x] Null session → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `TerminateDesktopSessionHandler` (4 tests)
- [x] Session connected → stops, disconnects, removes, returns Ok
- [x] Session not connected → removes, returns Ok
- [x] Null session → returns Fail
- [x] Stop throws → still disconnects and removes (best effort), returns Ok

#### `SendDesktopMessageHandler` (5 tests)
- [x] Connected + text → sends, adds to messages, clears pending, returns Ok
- [x] Not connected → auto-connects, then sends
- [x] Null session → returns Fail
- [x] Empty message → returns Fail
- [x] Send throws → returns Fail

#### `RefreshSecurityDataHandler` (4 tests)
- [x] All 4 API calls succeed → returns Ok with combined data
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] Any API call throws → returns Fail

#### `BanPeerHandler` (5 tests)
- [x] API returns true → returns Ok
- [x] Null peer → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `UnbanPeerHandler` (5 tests)
- [x] API returns true → returns Ok
- [x] Null peer → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `RefreshAuthUsersHandler` (4 tests)
- [x] API returns users and roles → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API throws → returns Fail

#### `SaveAuthUserHandler` (4 tests)
- [x] Valid user → API returns saved user → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail

#### `DeleteAuthUserHandler` (5 tests)
- [x] API returns true → returns Ok
- [x] Null user → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `RefreshPluginsHandler` (5 tests)
- [x] API returns snapshot → returns Ok with assemblies and runner IDs
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail
- [x] API throws → returns Fail

#### `SavePluginsHandler` (4 tests)
- [x] Valid assemblies → API returns updated snapshot → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail

#### `RefreshMcpRegistryHandler` (4 tests)
- [x] API returns servers + mapping → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API throws → returns Fail

#### `SaveMcpServerHandler` — desktop (4 tests)
- [x] Valid definition → API returns saved → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail

#### `DeleteMcpServerHandler` — desktop (5 tests)
- [x] API returns true → returns Ok
- [x] Null server → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `SaveAgentMcpMappingHandler` (4 tests)
- [x] Valid IDs → API returns true → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `RefreshPromptTemplatesHandler` (4 tests)
- [x] API returns templates → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API throws → returns Fail

#### `SavePromptTemplateHandler` (4 tests)
- [x] Valid template → API returns saved → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail

#### `DeletePromptTemplateHandler` (5 tests)
- [x] API returns true → returns Ok
- [x] Null template → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns false → returns Fail

#### `SeedSessionContextHandler` (6 tests)
- [x] Valid session ID + content → API returns true → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] Empty session ID → returns Fail
- [x] Empty content → returns Fail
- [x] API returns false → returns Fail

#### `StartLogMonitoringHandler` (4 tests)
- [x] Valid host/port → fetches snapshot, returns Ok with offset
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] Snapshot API throws → returns Fail

### 3d: Mobile handler tests (65 tests across 14 handlers)

#### `ConnectMobileSessionHandler` (12 tests)
- [x] Server mode + capacity available → connects, returns Ok
- [x] Direct mode → skips server info and capacity, connects
- [x] No current session → creates new session, then connects
- [x] Session already has agent ID → skips agent selection
- [x] Mode selection cancelled → returns Fail("Connect cancelled")
- [x] Empty host in server mode → returns Fail
- [x] Invalid port → returns Fail
- [x] Server info unreachable → returns Fail
- [x] Agent selection cancelled → returns Fail("Connect cancelled")
- [x] Capacity exhausted → returns Fail with reason
- [x] Capacity returns null → returns Fail
- [x] Connect throws → returns Fail with exception message

#### `DisconnectMobileSessionHandler` (2 tests)
- [x] Connected → disconnects, returns Ok
- [x] Already disconnected → returns Ok (no-op)

#### `CreateMobileSessionHandler` (2 tests)
- [x] Creates session with new ID, adds to store, returns Ok
- [x] Default title "New chat", connection mode "server"

#### `TerminateMobileSessionHandler` (6 tests)
- [x] Confirmed + connected → stops, disconnects, deletes, returns Ok
- [x] Confirmed + not connected → deletes, returns Ok
- [x] Confirmed + non-current session → deletes without disconnect
- [x] Null session → returns Fail
- [x] Confirmation rejected → returns Fail("Terminate cancelled")
- [x] Stop throws → still disconnects and deletes (best effort), returns Ok

#### `SendMobileMessageHandler` (6 tests)
- [x] Connected + text → sends text, adds user message, clears pending, returns Ok
- [x] Text matches script pattern → sends as script request
- [x] First message → auto-sets session title to first 60 chars
- [x] Empty text → returns Fail
- [x] Not connected → returns Fail
- [x] Send throws → returns Fail and adds error message

#### `SendMobileAttachmentHandler` (4 tests)
- [x] Connected + file picked → sends media, adds user message, returns Ok
- [x] Not connected → returns Fail
- [x] Picker cancelled → returns Fail
- [x] Send throws → returns Fail

#### `ArchiveMessageHandler` (2 tests)
- [x] Valid message → sets `IsArchived=true`, calls store, returns Ok
- [x] Null message → returns Fail

#### `UsePromptTemplateHandler` (7 tests)
- [x] Template selected + all variables provided → renders, sends, returns Ok
- [x] Single template available → auto-selects without prompting
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] No templates available → returns Fail
- [x] Template selection cancelled → returns Fail
- [x] Variable input cancelled → returns Fail

#### `NotifyMessageHandler` (2 tests)
- [x] Notify priority → calls `INotificationService`, returns Ok
- [x] Non-notify priority → no-op, returns Ok

#### `LoadMcpServersHandler` — mobile (5 tests)
- [x] API returns servers → returns Ok with ordered list
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail
- [x] API throws → returns Fail

#### `SaveMcpServerHandler` — mobile (5 tests)
- [x] Valid server → API returns success → returns Ok
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail
- [x] API response `Success=false` → returns Fail with message

#### `DeleteMcpServerHandler` — mobile (6 tests)
- [x] Valid server ID → API returns success → returns Ok
- [x] Empty server ID → returns Fail
- [x] Empty host → returns Fail
- [x] Invalid port → returns Fail
- [x] API returns null → returns Fail
- [x] API response `Success=false` → returns Fail

#### `SelectMobileSessionHandler` (3 tests)
- [x] Valid session ID found → selects, returns Ok
- [x] Null/empty session ID → returns Fail
- [x] Session ID not found → returns Fail

#### `NavigateToRouteHandler` (3 tests)
- [x] Valid route → navigates, closes flyout, returns Ok
- [x] Null/empty route → returns Fail
- [x] Navigation throws → returns Fail

### 3e: Request `ToString()` sensitive-data redaction tests (5 tests)

- [x] `RefreshOpenSessionsRequest.ToString()` redacts `ApiKey`
- [x] `CheckSessionCapacityRequest.ToString()` redacts `ApiKey`
- [x] `CreateDesktopSessionRequest.ToString()` redacts `ApiKey`
- [x] `ConnectMobileSessionRequest.ToString()` redacts `ApiKey`
- [x] Reflective safety net: all `IRequest<>` implementations with `ApiKey` property → `ToString()` does not contain raw value

### 3f: CorrelationId contract and propagation tests (8 tests)

- [x] Reflective: all `IRequest<>` implementations have `CorrelationId` as first constructor parameter
- [x] Reflective: all `IRequest<>` implementations include CorrelationId in `ToString()` output
- [x] `OpenNewSessionHandler` propagates CorrelationId to `CreateDesktopSessionRequest`
- [x] `CreateDesktopSessionHandler` propagates CorrelationId to `CheckSessionCapacityRequest`
- [x] `ConnectMobileSessionHandler` propagates CorrelationId to `CreateMobileSessionRequest`
- [x] `UsePromptTemplateHandler` propagates CorrelationId to `SendMobileMessageRequest`
- [x] `BanPeerHandler` propagates CorrelationId to `RefreshSecurityDataRequest`
- [x] `UnbanPeerHandler` propagates CorrelationId to `RefreshSecurityDataRequest`

### 3g: Shared mock and test helper types

- [x] `CapturingLogger<T>` test helper (captures `ILogger` entries for assertion)
- [x] `CapturingRequestDispatcher` test helper (records dispatched requests for CorrelationId propagation verification)
- [x] `MockRequestDispatcher` (configurable responses per request type)
- [x] `MockServerCapacityClient` (desktop)
- [x] `MockLocalServerManager` (desktop)
- [x] `MockConnectionSettingsDialogService` (desktop)
- [x] `MockServerApiClient` (mobile)
- [x] `MockAgentGatewayClient` (mobile)
- [x] `MockSessionStore` (mobile)
- [x] `MockAppPreferences` (mobile)
- [x] `MockConnectionModeSelector`
- [x] `MockAgentSelector`
- [x] `MockAttachmentPicker`
- [x] `MockPromptTemplateSelector`
- [x] `MockPromptVariableProvider`
- [x] `MockSessionTerminationConfirmation`
- [x] `MockNotificationService`

### 3h: UI tests with mocked dispatcher (TR-18.4, 8 tests)

#### Desktop (5 tests)
- [x] `SetManagementSection` via dispatcher → asserts `SelectedManagementSection` updated
- [x] `StartSession` mock returns Ok → asserts `StatusText` reflects success
- [x] `StartSession` mock returns Fail → asserts `StatusText` shows error
- [x] `RefreshOpenSessions` mock returns data → asserts collection populated
- [x] `CheckLocalServer` mock returns running → asserts action label is "Stop Local Server"

#### Mobile (3 tests)
- [x] `Connect` mock returns Ok → asserts status shows "Connected"
- [x] `Connect` mock returns Fail → asserts status shows error message
- [x] `SendMessage` mock returns Ok → asserts pending message cleared

### 3i: Phase 3 validation

- [x] Zero new `NoWarn` or suppression attributes
- [x] Full test suite passes (all projects)
- [x] Verify ~237 new tests are accounted for

---

## Phase 4: Validation and docs (~1–2 sessions)

- [x] Full build: `./scripts/build-dotnet10.sh Release`
- [x] Full build: `./scripts/build-desktop-dotnet9.sh Release`
- [x] Integration tests: `./scripts/test-integration.sh Release`
- [x] Final code-behind audit: grep all `.xaml.cs` for non-adapter logic
- [x] CQRS logging audit: verify dispatcher Debug entry/exit logging is active end-to-end
- [x] CQRS redaction audit: verify all request records with `ApiKey`/`Password`/`Token`/`Secret` override `ToString()` with redaction
- [x] CQRS log format audit: verify no handler contains its own entry/exit logging (dispatcher responsibility only)
- [x] Update `docs/requirements-completion-summary.md`: TR-18.1, TR-18.2, TR-18.3, TR-18.4 → Done
- [x] Create refactor-complete summary note in docs
- [x] Remove or update session handoff file
