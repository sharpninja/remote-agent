# Testing strategy

**Traceability:** [Functional requirements](functional-requirements.md) (FR) and [Technical requirements](technical-requirements.md) (TR).

---

You are a principal .NET test architect and QA engineer with deep expertise in .NET 10, .NET MAUI, Avalonia UI, gRPC bidirectional streaming, xUnit, FluentAssertions, NSubstitute, LiteDB, and TDD for cross-platform apps (mobile + desktop + services).

## 1. Principles

Tests must be:

- **Behavior-focused** — Black-box where possible; verify observable outcomes, not implementation details.
- **Traceable** — Each test (or test class) documents which FR/TR item(s) it verifies via comments.
- **Comprehensive** — Happy paths, boundaries, negatives, errors; concurrency/thread-safety and async/streaming where relevant.
- **Clean** — Descriptive names, AAA pattern with section comments, no duplication; use `[Theory]`/`[InlineData]` when inputs vary.
- **Isolated** — Mock external dependencies: processes, gRPC channels, file system, LiteDB, platform services (e.g. `INotificationManager`).
- **Platform-aware** — MAUI main-thread, Android notification channels/priorities, Avalonia headless-friendly patterns.

---

## 2. Test layout

| Area | Project | Scope |
|------|---------|--------|
| App logic (chat, markdown, templates) | `RemoteAgent.App.Tests` | Unit; references `RemoteAgent.App.Logic` |
| Service logic (options, capacity, protection, storage, plugins, prompts) | `RemoteAgent.Service.Tests` | Unit; mocks/stubs only |
| gRPC service (streaming, lifecycle, correlation, scripts) | `RemoteAgent.Service.IntegrationTests` | In-memory host; explicit entrypoint (FR-16.1, TR-8.4.1) |
| Mobile UI | `RemoteAgent.Mobile.UiTests` | Connection screen, core flows (TR-8.5) |
| Desktop UI | `RemoteAgent.Desktop.UiTests` | Menu/toolbar/tabs, terminate, headless (TR-8.6) |

---

## 3. FR/TR → test coverage matrix

### 3.1 gRPC bidirectional streaming (FR-2, TR-2.3, TR-4)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-4.1, TR-4.4 | Duplex stream Connect(stream ClientMessage) returns (stream ServerMessage) | Integration: `AgentGatewayServiceIntegrationTests_Echo`, `_Stop`, `_NoCommand` |
| TR-4.2 | ClientMessage: text, control (start/stop) | Integration: Echo, Stop, NoCommand |
| TR-4.3 | ServerMessage: output, error, event, priority | Integration: Echo (output), CorrelationId (error/event), priority in proto |
| TR-4.5 | Correlation ID on request echoed on response(s) | Integration: `AgentGatewayServiceIntegrationTests_CorrelationId` |
| FR-2.2, FR-7.2 | Real-time streaming; session lifecycle events in chat | Integration + app ChatMessage/event handling |

### 3.2 Chat UI logic (FR-2, FR-4, TR-5)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-5.1 | Observable collection of chat messages | `ChatMessageTests` (binding/DisplayText/RenderedHtml) |
| FR-2.3, TR-5.3 | Markdown rendering for agent output | `MarkdownFormatTests`, `ChatMessageTests` (RenderedHtml) |
| FR-2.5, TR-5.6 | Multi-line input; Enter = newline | UI / manual; logic tests for message text preservation |
| FR-2.6, TR-5.7 | Ctrl+Enter submit (desktop) | Desktop UI tests |
| FR-4.1, FR-4.2, TR-5.5 | Swipe to archive; archived hidden from list | `ChatMessageTests` (IsArchived, PropertyChanged) |
| FR-11.1.3.x, TR-12.2.2 | Session title inline edit (tap to edit, tap off to commit) | UI / session store behavior |

### 3.3 Notifications (FR-3, TR-5.4)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| FR-3.1 | Priority: normal, high, notify | `ChatMessageTests` (Priority default); proto/server |
| FR-3.2, FR-3.3 | Notify → system notification; tap opens chat to message | Mock `INotificationManager` in app logic tests; Android channel in platform tests |

### 3.4 Persistence (TR-11, FR-11, TR-12)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-11.1 | LiteDB for requests/results (app and server) | `LiteDbLocalStorageTests` (service); session/message stores in App (see §4) |
| TR-11.2 | Media stored alongside LiteDB on server | Service storage/media tests |
| TR-11.3 | Images to app in DCIM/Remote Agent | Platform `MediaSaveService` tests if extracted |
| TR-12.1.3, FR-11.1 | Session list: session_id, title, agent_id; persisted | `LocalSessionStore` (in App) — see §4 |
| FR-11.1.3, TR-12.2.1 | Title default to first request; user-editable | Session store UpdateTitle behavior |

### 3.5 Service behavior (FR-1, FR-7, FR-9, TR-3, TR-10)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-3.2, FR-7.1 | Spawn agent on session start; stop on end | Integration: Echo, Stop, NoCommand |
| TR-3.3, TR-3.4 | Forward stdin; stream stdout/stderr | Integration: Echo |
| TR-3.5 | Message priority (normal/high/notify) | Proto + integration |
| TR-3.6 | Session log file per connection | Structured log service / integration |
| TR-3.7, TR-3.8, FR-13.7, FR-13.8 | Server-wide and per-agent session caps | `SessionCapacityServiceTests` |
| FR-9.1, FR-9.2 | Script run (bash/pwsh); stdout/stderr on completion | Integration (script request/response) |
| TR-10.1, TR-10.2 | Plugin strategy; discovery via config, runtime load | `PluginConfigurationServiceTests` |

### 3.6 Desktop / Avalonia (FR-12, TR-14)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| FR-12.1.x | Tabbed sessions, menu/toolbar, terminate | `RemoteAgent.Desktop.UiTests` |
| FR-12.2, TR-13.4, TR-13.5 | Structured log viewer; filtering (time, level, session/correlation id, server) | Desktop log store/filter tests |
| FR-12.8 | Per-request context editor | ViewModel/command tests |
| FR-12.9, FR-12.10 | Multiple servers; concurrent connections | Desktop infrastructure tests |
| TR-14.1.x | DI, command bindings, server scoping | Desktop unit/UI tests |

### 3.7 Multiple sessions / agent selection (FR-11, TR-12)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| FR-11.1.1 | session_id routing; one agent per session | Integration (session control + agent_id) |
| FR-11.1.2 | Agent picker; ListAgents / available_agents | Integration: GetServerInfo, management APIs |
| FR-11.1.3 | Session title default + editable | Session store (App); UI |

### 3.8 Prompt templates (FR-14, TR-17)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| FR-14.2, TR-17.3 | Handlebars placeholders; variable extraction | `PromptTemplateEngineTests` (ExtractVariables, Render) |
| FR-14.3, FR-14.4 | Variable input UI → render → submit | Client flow (UI or ViewModel tests) |
| TR-17.1, TR-17.2 | list/upsert/delete templates; LiteDB | `PromptTemplateServiceTests` |

### 3.9 Extensibility (FR-8, TR-10)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-10.2 | Plugin discovery via config; assembly load | `PluginConfigurationServiceTests` |

### 3.10 Observability (TR-13)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-13.1, TR-13.2 | Structured JSONL logs; session_id, correlation_id | `StructuredLogServiceTests` |
| TR-13.5 | Filtering: time, level, event, session/correlation, component, search | `StructuredLogFilter` (App) / desktop filter tests |

### 3.11 Connection protection (FR-15, TR-15.5, TR-15.6)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| FR-15.1, TR-15.5 | Rate limiting (connection attempts, client messages) | `ConnectionProtectionServiceTests` |
| FR-15.2, TR-15.6 | DoS detection; temporary block | `ConnectionProtectionServiceTests` |
| Ban/unban | `ConnectionProtectionServiceTests` (BanPeer, UnbanPeer, GetConnectionHistory) |

### 3.12 Management APIs (FR-13, TR-15)

| Requirement | Description | Test location |
|-------------|-------------|---------------|
| TR-15.8, TR-15.9 | Sessions open/abandoned/terminate; peers, history, ban | `AgentGatewayServiceIntegrationTests_ManagementApis` |
| FR-13.7, FR-13.8 | Session capacity; agent-level caps | `SessionCapacityServiceTests` |

---

## 4. Gaps and recommendations

- **LocalSessionStore** (App): Lives in `RemoteAgent.App` (MAUI). To unit test without referencing the multi-targeted app, either (1) extract session persistence to `RemoteAgent.App.Logic` (or a shared library) and add `LocalSessionStoreTests`, or (2) add a test project that references the App with a single TFM (e.g. `net10.0`) for session/store tests only.
- **StructuredLogFilter** (App): Same as above; add `StructuredLogFilterTests` when the type is in a testable assembly (e.g. filter logic in shared lib for TR-13.5).
- **AgentSessionClient**: Streaming client in App.Logic; consider tests with a mock gRPC channel or in-memory server to verify Connect/Send/Receive/Disconnect and correlation handling (FR-2.4, TR-4.5).
- **Notification flow**: Unit tests that mock `IPlatformNotificationService` and assert “notify” priority triggers show/tap (FR-3.2, FR-3.3).
- **Concurrency**: Add tests for `SessionCapacityService` under concurrent `TryRegisterSession`/`UnregisterSession`; `ConnectionProtectionService` already covers concurrent connection limits.

---

## 5. Running tests

- **Unit + integration (default):** `dotnet test RemoteAgent.slnx` (exclude integration if desired via filter).
- **Integration only (on demand):** Use repo script e.g. `./scripts/test-integration.sh` (FR-16.1, TR-8.4.1).
- **By track:** `./scripts/build-dotnet10.sh` (MAUI + service); `./scripts/build-desktop-dotnet9.sh` (Avalonia) (TR-8.4).

---

## 6. Naming and structure

- **Test files:** `*Tests.cs` (e.g. `SessionCapacityServiceTests.cs`).
- **Test classes:** Suffix `Tests`; one per production type or feature area.
- **Test methods:** Descriptive behavior names, e.g. `TryOpenConnection_Denies_WhenConcurrentLimitReached` (Given_When_Then style optional).
- **FR/TR in code:** Prefer class-level `/// <summary>` or method-level `// FR-x.y, TR-x.y` so traceability is visible in IDE and reports.
