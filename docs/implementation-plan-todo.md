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
- [ ] Verify desktop build (`./scripts/build-desktop-dotnet9.sh Release`)
- [ ] Verify existing desktop UI tests still pass

---

## Phase 1: Desktop code-behind removal (~3–4 sessions)

### 1a: ConnectionSettingsDialog refactor

- [x] Add `IConnectionSettingsDialogService` interface
- [x] Add `AvaloniaConnectionSettingsDialogService` implementation
- [x] Add `ConnectionSettingsDialogViewModel` with properties, validation, Submit/Cancel commands, `RequestClose` event
- [ ] Refactor `ConnectionSettingsDialog.axaml` to use bindings (remove all `x:Name` control access)
- [ ] Refactor `ConnectionSettingsDialog.axaml.cs` to constructor + `RequestClose` subscription only
- [x] Add `OpenNewSessionRequest` and `OpenNewSessionHandler`
- [x] Expose `StartSessionCommand` on `MainWindowViewModel`; bind in menu + toolbar XAML
- [x] Remove `OnStartSessionClick` from `MainWindow.axaml.cs`
- [ ] Run desktop UI tests

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

- [ ] Extract `SessionManagementViewModel` (NewSession, TerminateCurrent, TerminateSession, SendMessage, CheckCapacity)
- [ ] Extract `OpenServerSessionsViewModel` (RefreshOpenSessions, TerminateOpenServerSession)
- [ ] Extract `SecurityViewModel` (RefreshSecurityData, BanPeer, UnbanPeer)
- [ ] Extract `AuthUsersViewModel` (RefreshAuthUsers, SaveAuthUser, DeleteAuthUser)
- [ ] Extract `PluginsViewModel` (RefreshPlugins, SavePlugins)
- [ ] Extract `McpRegistryViewModel` (desktop) (RefreshMcp, SaveMcpServer, DeleteMcpServer, SaveAgentMcpMapping)
- [ ] Extract `PromptTemplatesViewModel` (RefreshPromptTemplates, SavePromptTemplate, DeletePromptTemplate, SeedContext)
- [ ] Extract `StructuredLogsViewModel` (StartLogMonitoring, StopLogMonitoring, ApplyLogFilter, ClearLogFilter)
- [ ] Update `MainWindow.axaml` DataContext paths for sub-VMs
- [ ] Run desktop UI tests after each extraction

### 1e: Interface wrappers

- [ ] Add `IStructuredLogClient` interface; implement wrapper for static `ServerApiClient` log methods
- [ ] Register `IStructuredLogClient` in Desktop DI

### 1f: ServerWorkspaceViewModel CQRS handlers (24 total)

**Convention:** Every request record has `Guid CorrelationId` as its first parameter. VMs generate `Guid.NewGuid()` at the command boundary. Handlers that dispatch child requests MUST propagate `request.CorrelationId` (see Section 2.8 of the implementation plan).

- [ ] `CreateDesktopSessionHandler` (replaces `NewSessionAsync`)
- [x] `CheckSessionCapacityHandler`
- [x] `RefreshOpenSessionsHandler`
- [x] `TerminateOpenServerSessionHandler`
- [ ] `TerminateDesktopSessionHandler`
- [ ] `SendDesktopMessageHandler`
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
- [ ] `StartLogMonitoringHandler`
- [ ] Register all handlers in Desktop DI *(22 of 26 registered; blocked on 4 missing handlers above)*

### 1g: Phase 1 validation

- [ ] Delete all unused event handlers from `MainWindow.axaml.cs`
- [ ] Full desktop build (`./scripts/build-desktop-dotnet9.sh Release`)
- [ ] Run desktop UI tests; fix regressions

### 1h: Management App Log view (FR-12.12)

- [ ] Create `AppLogEntry` record (`Timestamp`, `Level`, `Category`, `Message`, `Exception?`)
- [ ] Create `IAppLogStore` interface with `Add(AppLogEntry)`, `GetAll()`, `Clear()` methods
- [ ] Create `InMemoryAppLogStore` (thread-safe, bounded ring buffer or unbounded list)
- [ ] Create `AppLoggerProvider` / `AppLoggerCategory` implementing `ILoggerProvider` + `ILogger`; each log call adds to `IAppLogStore`
- [ ] Register `AppLoggerProvider` in `App.axaml.cs` via `ILoggerFactory.AddProvider(...)`
- [ ] Create `AppLogViewModel` (ObservableCollection of visible entries, filter text, `ClearCommand`, `SaveCommand`)
- [ ] `ClearCommand` → `ClearAppLogRequest` / `ClearAppLogHandler` (clears store, updates VM)
- [ ] `SaveCommand` → `SaveAppLogRequest` / `SaveAppLogHandler` (exports to txt/json/csv via format selector)
- [ ] Add Management App Log navigation item + panel to `MainWindow.axaml`
- [ ] Wire `AppLogViewModel` into `MainWindowViewModel`; bind via CQRS dispatcher
- [ ] Unit tests: `AppLoggerProvider_ShouldCaptureLogEntries`, `ClearAppLogHandler_ShouldEmptyCollection`, `SaveAppLogHandler_ShouldWriteAllThreeFormats`

---

## Phase 2: Mobile code-behind removal (~4–6 sessions)

### 2a: Interface wrappers for mobile dependencies

- [ ] Add `IAgentGatewayClient` interface; make `AgentGatewayClientService` implement it
- [ ] Change `MainPageViewModel` constructor to accept `IAgentGatewayClient` (not concrete)
- [ ] Add `IServerApiClient` interface in `App.Logic`; implement wrapper in `App`
- [ ] Add `IAppPreferences` interface + `MauiAppPreferences` implementation
- [ ] Register all interface wrappers in `MauiProgram.cs`

### 2b: Platform UI abstractions (7 interfaces + 7 implementations)

- [ ] `IConnectionModeSelector` + `MauiConnectionModeSelector`
- [ ] `IAgentSelector` + `MauiAgentSelector`
- [ ] `IAttachmentPicker` + `MauiAttachmentPicker`
- [ ] `IPromptTemplateSelector` + `MauiPromptTemplateSelector`
- [ ] `IPromptVariableProvider` + `MauiPromptVariableProvider`
- [ ] `ISessionTerminationConfirmation` + `MauiSessionTerminationConfirmation`
- [ ] `INotificationService` + `PlatformNotificationServiceAdapter`
- [ ] Register all platform abstractions in `MauiProgram.cs`

### 2c: Session command bus and navigation

- [ ] Add `ISessionCommandBus` interface
- [ ] Make `MainPageViewModel` implement `ISessionCommandBus`
- [ ] Add `INavigationService` interface + `MauiNavigationService` implementation
- [ ] Register in `MauiProgram.cs`

### 2d: MainPageViewModel CQRS handlers (9 total)

**Convention:** Every request record has `Guid CorrelationId` as its first parameter. VMs generate `Guid.NewGuid()` at the command boundary. Handlers that dispatch child requests MUST propagate `request.CorrelationId` (see Section 2.8 of the implementation plan).

- [ ] `ConnectMobileSessionHandler`
- [ ] `DisconnectMobileSessionHandler`
- [ ] `CreateMobileSessionHandler`
- [ ] `TerminateMobileSessionHandler`
- [ ] `SendMobileMessageHandler`
- [ ] `SendMobileAttachmentHandler`
- [ ] `ArchiveMessageHandler`
- [ ] `UsePromptTemplateHandler`
- [ ] `NotifyMessageHandler` (or wire into gateway event)
- [ ] Remove all delegate properties from `MainPageViewModel`
- [ ] Add `IsEditingTitle` / `BeginEditTitleCommand` / `CommitTitleCommand` to VM

### 2e: MainPage code-behind cleanup

- [ ] Remove delegate assignments from `MainPage.xaml.cs`
- [ ] Remove `ShowNotificationForMessage` event subscription
- [ ] Remove session title focus/tap/unfocus handlers (replaced by VM + behaviors)
- [ ] Remove `StartNewSessionFromShell`, `SelectSessionFromShell`, `TerminateSessionFromShellAsync`, `GetCurrentSessionId`
- [ ] Keep only constructor (`BindingContext = _vm`) and Windows keyboard hook (view-adapter)

### 2f: McpRegistryPage refactor

- [ ] Add `McpRegistryPageViewModel` (Host, Port, Servers, form fields, commands)
- [ ] Add `LoadMcpServersHandler` (mobile)
- [ ] Add `SaveMcpServerHandler` (mobile)
- [ ] Add `DeleteMcpServerHandler` (mobile)
- [ ] Bind `McpRegistryPage.xaml` to VM; remove all logic from code-behind
- [ ] Register VM and handlers in `MauiProgram.cs`

### 2g: AppShell refactor

- [ ] Add `AppShellViewModel` (SessionItems, StartSessionCommand, SelectSessionCommand, TerminateSessionCommand, navigation commands)
- [ ] Add `SelectMobileSessionRequest` / handler
- [ ] Add `NavigateToRouteRequest` / handler
- [ ] Refactor `AppShell.xaml` to use BindableLayout for session list
- [ ] Remove `BuildSessionButtons` and all flyout click handlers from code-behind
- [ ] Keep only constructor + ShellContent registration in code-behind

### 2h: Phase 2 validation

- [ ] Full mobile build (`./scripts/build-dotnet10.sh Release`)
- [ ] Run mobile UI tests; fix regressions

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

- [ ] Submit with valid host, port, mode, agent → `IsAccepted=true`, `RequestClose` fires
- [ ] Submit with empty host → `ValidationMessage="Host is required."`, does not close
- [ ] Submit with port "0" → `ValidationMessage="Port must be 1-65535."`
- [ ] Submit with port "99999" → same validation message
- [ ] Submit with non-numeric port → same validation message
- [ ] Submit with empty mode → `ValidationMessage="Mode is required."`
- [ ] Submit with empty agent → `ValidationMessage="Agent is required."`
- [ ] Cancel → `IsAccepted=false`, `RequestClose` fires with `false`
- [ ] `ToResult()` after valid submit → returns record matching VM properties

### 3c: Desktop handler tests (133 tests across 24 handlers)

#### `SetManagementSectionHandler` (2 tests)
- [x] Valid section key → returns `Unit`
- [x] Null/empty section key → returns `Unit` (pass-through)

#### `ExpandStatusLogPanelHandler` (1 test)
- [x] Returns `Unit`

#### `OpenNewSessionHandler` (5 tests)
- [x] Dialog accepted → applies settings to workspace, returns Ok
- [ ] Dialog accepted → dispatches `CreateDesktopSessionRequest`
- [x] Dialog cancelled (returns null) → returns Fail("Cancelled")
- [x] Workspace is null → returns Fail
- [ ] `CreateDesktopSession` dispatch fails → returns Fail with inner error

#### `SaveServerRegistrationHandler` (4 tests)
- [x] Valid host + port → upserts to store, returns Ok with registration
- [x] Empty host → returns Fail("Host required")
- [x] Invalid port (0, >65535, non-numeric) → returns Fail
- [ ] Store throws → returns Fail

#### `RemoveServerRegistrationHandler` (3 tests)
- [x] Existing server ID → deletes, returns Ok
- [x] Store returns false → returns Fail
- [x] Null server ID → returns Fail

#### `CheckLocalServerHandler` (3 tests)
- [x] Server running → returns probe with `IsRunning=true`
- [x] Server stopped → returns probe with `IsRunning=false`
- [ ] Probe throws → returns Fail

#### `ApplyLocalServerActionHandler` (4 tests)
- [x] Server stopped → starts, re-probes, returns Ok
- [x] Server running → stops, re-probes, returns Ok
- [x] Start fails → returns Fail
- [x] Stop fails → returns Fail

#### `CreateDesktopSessionHandler` (6 tests)
- [ ] Server mode + capacity available → creates session, connects, returns Ok
- [ ] Direct mode → skips capacity check, creates, connects, returns Ok
- [ ] Server mode + capacity exhausted → returns Fail with reason
- [ ] Capacity returns null → returns Fail
- [ ] Connect throws → returns Fail with connection error
- [ ] Invalid port → returns Fail

#### `CheckSessionCapacityHandler` (7 tests)
- [ ] Capacity available → returns Ok with snapshot
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API throws `InvalidOperationException` → returns Fail with message
- [ ] API throws `HttpRequestException` → returns Fail
- [ ] API throws `TaskCanceledException` → returns Fail("timed out")
- [ ] API returns null → returns Fail

#### `RefreshOpenSessionsHandler` (4 tests)
- [ ] API returns sessions → returns Ok with list
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API throws → returns Fail

#### `TerminateOpenServerSessionHandler` (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null session → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `TerminateDesktopSessionHandler` (4 tests)
- [ ] Session connected → stops, disconnects, removes, returns Ok
- [ ] Session not connected → removes, returns Ok
- [ ] Null session → returns Fail
- [ ] Stop throws → still disconnects and removes (best effort), returns Ok

#### `SendDesktopMessageHandler` (5 tests)
- [ ] Connected + text → sends, adds to messages, clears pending, returns Ok
- [ ] Not connected → auto-connects, then sends
- [ ] Null session → returns Fail
- [ ] Empty message → returns Fail
- [ ] Send throws → returns Fail

#### `RefreshSecurityDataHandler` (4 tests)
- [ ] All 4 API calls succeed → returns Ok with combined data
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] Any API call throws → returns Fail

#### `BanPeerHandler` (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null peer → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `UnbanPeerHandler` (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null peer → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `RefreshAuthUsersHandler` (4 tests)
- [ ] API returns users and roles → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API throws → returns Fail

#### `SaveAuthUserHandler` (4 tests)
- [ ] Valid user → API returns saved user → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail

#### `DeleteAuthUserHandler` (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null user → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `RefreshPluginsHandler` (5 tests)
- [ ] API returns snapshot → returns Ok with assemblies and runner IDs
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail
- [ ] API throws → returns Fail

#### `SavePluginsHandler` (4 tests)
- [ ] Valid assemblies → API returns updated snapshot → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail

#### `RefreshMcpRegistryHandler` (4 tests)
- [ ] API returns servers + mapping → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API throws → returns Fail

#### `SaveMcpServerHandler` — desktop (4 tests)
- [ ] Valid definition → API returns saved → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail

#### `DeleteMcpServerHandler` — desktop (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null server → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `SaveAgentMcpMappingHandler` (4 tests)
- [ ] Valid IDs → API returns true → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `RefreshPromptTemplatesHandler` (4 tests)
- [ ] API returns templates → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API throws → returns Fail

#### `SavePromptTemplateHandler` (4 tests)
- [ ] Valid template → API returns saved → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail

#### `DeletePromptTemplateHandler` (5 tests)
- [ ] API returns true → returns Ok
- [ ] Null template → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns false → returns Fail

#### `SeedSessionContextHandler` (6 tests)
- [ ] Valid session ID + content → API returns true → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] Empty session ID → returns Fail
- [ ] Empty content → returns Fail
- [ ] API returns false → returns Fail

#### `StartLogMonitoringHandler` (4 tests)
- [ ] Valid host/port → fetches snapshot, returns Ok with offset
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] Snapshot API throws → returns Fail

### 3d: Mobile handler tests (65 tests across 14 handlers)

#### `ConnectMobileSessionHandler` (12 tests)
- [ ] Server mode + capacity available → connects, returns Ok
- [ ] Direct mode → skips server info and capacity, connects
- [ ] No current session → creates new session, then connects
- [ ] Session already has agent ID → skips agent selection
- [ ] Mode selection cancelled → returns Fail("Connect cancelled")
- [ ] Empty host in server mode → returns Fail
- [ ] Invalid port → returns Fail
- [ ] Server info unreachable → returns Fail
- [ ] Agent selection cancelled → returns Fail("Connect cancelled")
- [ ] Capacity exhausted → returns Fail with reason
- [ ] Capacity returns null → returns Fail
- [ ] Connect throws → returns Fail with exception message

#### `DisconnectMobileSessionHandler` (2 tests)
- [ ] Connected → disconnects, returns Ok
- [ ] Already disconnected → returns Ok (no-op)

#### `CreateMobileSessionHandler` (2 tests)
- [ ] Creates session with new ID, adds to store, returns Ok
- [ ] Default title "New chat", connection mode "server"

#### `TerminateMobileSessionHandler` (6 tests)
- [ ] Confirmed + connected → stops, disconnects, deletes, returns Ok
- [ ] Confirmed + not connected → deletes, returns Ok
- [ ] Confirmed + non-current session → deletes without disconnect
- [ ] Null session → returns Fail
- [ ] Confirmation rejected → returns Fail("Terminate cancelled")
- [ ] Stop throws → still disconnects and deletes (best effort), returns Ok

#### `SendMobileMessageHandler` (6 tests)
- [ ] Connected + text → sends text, adds user message, clears pending, returns Ok
- [ ] Text matches script pattern → sends as script request
- [ ] First message → auto-sets session title to first 60 chars
- [ ] Empty text → returns Fail
- [ ] Not connected → returns Fail
- [ ] Send throws → returns Fail and adds error message

#### `SendMobileAttachmentHandler` (4 tests)
- [ ] Connected + file picked → sends media, adds user message, returns Ok
- [ ] Not connected → returns Fail
- [ ] Picker cancelled → returns Fail
- [ ] Send throws → returns Fail

#### `ArchiveMessageHandler` (2 tests)
- [ ] Valid message → sets `IsArchived=true`, calls store, returns Ok
- [ ] Null message → returns Fail

#### `UsePromptTemplateHandler` (7 tests)
- [ ] Template selected + all variables provided → renders, sends, returns Ok
- [ ] Single template available → auto-selects without prompting
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] No templates available → returns Fail
- [ ] Template selection cancelled → returns Fail
- [ ] Variable input cancelled → returns Fail

#### `NotifyMessageHandler` (2 tests)
- [ ] Notify priority → calls `INotificationService`, returns Ok
- [ ] Non-notify priority → no-op, returns Ok

#### `LoadMcpServersHandler` — mobile (5 tests)
- [ ] API returns servers → returns Ok with ordered list
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail
- [ ] API throws → returns Fail

#### `SaveMcpServerHandler` — mobile (5 tests)
- [ ] Valid server → API returns success → returns Ok
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail
- [ ] API response `Success=false` → returns Fail with message

#### `DeleteMcpServerHandler` — mobile (6 tests)
- [ ] Valid server ID → API returns success → returns Ok
- [ ] Empty server ID → returns Fail
- [ ] Empty host → returns Fail
- [ ] Invalid port → returns Fail
- [ ] API returns null → returns Fail
- [ ] API response `Success=false` → returns Fail

#### `SelectMobileSessionHandler` (3 tests)
- [ ] Valid session ID found → selects, returns Ok
- [ ] Null/empty session ID → returns Fail
- [ ] Session ID not found → returns Fail

#### `NavigateToRouteHandler` (3 tests)
- [ ] Valid route → navigates, closes flyout, returns Ok
- [ ] Null/empty route → returns Fail
- [ ] Navigation throws → returns Fail

### 3e: Request `ToString()` sensitive-data redaction tests (5 tests)

- [ ] `RefreshOpenSessionsRequest.ToString()` redacts `ApiKey`
- [ ] `CheckSessionCapacityRequest.ToString()` redacts `ApiKey`
- [ ] `CreateDesktopSessionRequest.ToString()` redacts `ApiKey`
- [ ] `ConnectMobileSessionRequest.ToString()` redacts `ApiKey`
- [ ] Reflective safety net: all `IRequest<>` implementations with `ApiKey` property → `ToString()` does not contain raw value

### 3f: CorrelationId contract and propagation tests (8 tests)

- [ ] Reflective: all `IRequest<>` implementations have `CorrelationId` as first constructor parameter
- [ ] Reflective: all `IRequest<>` implementations include CorrelationId in `ToString()` output
- [ ] `OpenNewSessionHandler` propagates CorrelationId to `CreateDesktopSessionRequest`
- [ ] `CreateDesktopSessionHandler` propagates CorrelationId to `CheckSessionCapacityRequest`
- [ ] `ConnectMobileSessionHandler` propagates CorrelationId to `CreateMobileSessionRequest`
- [ ] `UsePromptTemplateHandler` propagates CorrelationId to `SendMobileMessageRequest`
- [ ] `BanPeerHandler` propagates CorrelationId to `RefreshSecurityDataRequest`
- [ ] `UnbanPeerHandler` propagates CorrelationId to `RefreshSecurityDataRequest`

### 3g: Shared mock and test helper types

- [x] `CapturingLogger<T>` test helper (captures `ILogger` entries for assertion)
- [ ] `CapturingRequestDispatcher` test helper (records dispatched requests for CorrelationId propagation verification)
- [ ] `MockRequestDispatcher` (configurable responses per request type)
- [ ] `MockServerCapacityClient` (desktop)
- [ ] `MockLocalServerManager` (desktop)
- [ ] `MockConnectionSettingsDialogService` (desktop)
- [ ] `MockServerApiClient` (mobile)
- [ ] `MockAgentGatewayClient` (mobile)
- [ ] `MockSessionStore` (mobile)
- [ ] `MockAppPreferences` (mobile)
- [ ] `MockConnectionModeSelector`
- [ ] `MockAgentSelector`
- [ ] `MockAttachmentPicker`
- [ ] `MockPromptTemplateSelector`
- [ ] `MockPromptVariableProvider`
- [ ] `MockSessionTerminationConfirmation`
- [ ] `MockNotificationService`

### 3h: UI tests with mocked dispatcher (TR-18.4, 8 tests)

#### Desktop (5 tests)
- [ ] `SetManagementSection` via dispatcher → asserts `SelectedManagementSection` updated
- [ ] `StartSession` mock returns Ok → asserts `StatusText` reflects success
- [ ] `StartSession` mock returns Fail → asserts `StatusText` shows error
- [ ] `RefreshOpenSessions` mock returns data → asserts collection populated
- [ ] `CheckLocalServer` mock returns running → asserts action label is "Stop Local Server"

#### Mobile (3 tests)
- [ ] `Connect` mock returns Ok → asserts status shows "Connected"
- [ ] `Connect` mock returns Fail → asserts status shows error message
- [ ] `SendMessage` mock returns Ok → asserts pending message cleared

### 3i: Phase 3 validation

- [ ] Zero new `NoWarn` or suppression attributes
- [ ] Full test suite passes (all projects)
- [ ] Verify ~237 new tests are accounted for

---

## Phase 4: Validation and docs (~1–2 sessions)

- [ ] Full build: `./scripts/build-dotnet10.sh Release`
- [ ] Full build: `./scripts/build-desktop-dotnet9.sh Release`
- [ ] Integration tests: `./scripts/test-integration.sh Release`
- [ ] Final code-behind audit: grep all `.xaml.cs` for non-adapter logic
- [ ] CQRS logging audit: verify dispatcher Debug entry/exit logging is active end-to-end
- [ ] CQRS redaction audit: verify all request records with `ApiKey`/`Password`/`Token`/`Secret` override `ToString()` with redaction
- [ ] CQRS log format audit: verify no handler contains its own entry/exit logging (dispatcher responsibility only)
- [ ] Update `docs/requirements-completion-summary.md`: TR-18.1, TR-18.2, TR-18.3, TR-18.4 → Done
- [ ] Create refactor-complete summary note in docs
- [ ] Remove or update session handoff file
