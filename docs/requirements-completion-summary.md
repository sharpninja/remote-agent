# Requirements completion summary

Status of all functional (FR) and technical (TR) requirements as of the current codebase. **Done** = implemented; **Partial** = partly done or only in docs/config; **Not started** = no implementation.

Requirement IDs in the tables link to the corresponding section in [Functional requirements](functional-requirements.md) or [Technical requirements](technical-requirements.md); those documents also cross-link FR↔TR where they relate.

*Refreshed to reflect: desktop/management requirements (functional section 12+ and technical section 13+), prompt template system, connection protection and policy controls, and isolated integration-test execution policy.*

---

## Functional requirements

| ID | Requirement | State | Notes |
|----|-------------|--------|------|
| [**FR-1.1**](functional-requirements.md#1-product-purpose) | Android app communicating with Linux service | **Done** | MAUI Android app + ASP.NET Core service |
| [**FR-1.2**](functional-requirements.md#1-product-purpose) | Service spawns Cursor agent and establishes communication | **Done** | Configurable agent (Agent:Command, RunnerId); process or copilot-windows runner; stdin/stdout |
| [**FR-1.3**](functional-requirements.md#1-product-purpose) | Service forwards user messages to the agent | **Done** | `ClientMessage.text` → agent stdin |
| [**FR-1.4**](functional-requirements.md#1-product-purpose) | Service sends agent output to app in real time | **Done** | `ServerMessage.output` / `error` streamed |
| [**FR-1.5**](functional-requirements.md#1-product-purpose) | Service logs all interaction | **Done** | Session log file per connection (`LogDirectory`, `remote-agent-{sessionId}.log`) |
| [**FR-1.6**](functional-requirements.md#1-product-purpose) | Chat-based UI for all interaction | **Done** | MainPage chat list + input |
| [**FR-2.1**](functional-requirements.md#2-chat-and-messaging) | User can send messages from app to agent | **Done** | Text input → gRPC → agent |
| [**FR-2.2**](functional-requirements.md#2-chat-and-messaging) | Agent output streamed in real time and displayed | **Done** | Stream bound to observable collection, WebView |
| [**FR-2.3**](functional-requirements.md#2-chat-and-messaging) | Agent output formatted with markdown | **Done** | Markdig in App.Logic, WebView render |
| [**FR-2.4**](functional-requirements.md#2-chat-and-messaging) | Connect / disconnect; session start/stop | **Done** | Connect/Disconnect UI; `SessionControl` START/STOP |
| [**FR-2.5**](functional-requirements.md#2-chat-and-messaging) | Chat text entry supports multi-line input (Enter inserts newline) | **Done** | Multi-line editor/input on mobile and desktop; Enter newline behavior documented in desktop watermark |
| [**FR-2.6**](functional-requirements.md#2-chat-and-messaging) | Desktop Ctrl+Enter submits request | **Done** | Desktop chat textbox binds `Ctrl+Enter` to send command |
| [**FR-2.7**](functional-requirements.md#2-chat-and-messaging) | Mobile connection-first view then chat view | **Done** | MainPage has dedicated connect surface before session-established chat workspace |
| [**FR-3.1**](functional-requirements.md#3-message-priority-and-notifications) | Server supports message priority (normal, high, notify) | **Done** | `MessagePriority` in proto and server |
| [**FR-3.2**](functional-requirements.md#3-message-priority-and-notifications) | Notify priority → system notification | **Done** | Android `PlatformNotificationService`, channel |
| [**FR-3.3**](functional-requirements.md#3-message-priority-and-notifications) | Tap notification → open app, message visible | **Done** | Notification opens app; chat shows message |
| [**FR-3.4**](functional-requirements.md#3-message-priority-and-notifications) | Normal/high supported in protocol and model | **Done** | Enum and binding in app |
| [**FR-4.1**](functional-requirements.md#4-archive) | Swipe message to archive | **Done** | SwipeView left/right, Archive command |
| [**FR-4.2**](functional-requirements.md#4-archive) | Archived hidden from main list | **Done** | `IsArchived`; filtered in UI |
| [**FR-5.1**](functional-requirements.md#5-user-interface-and-presentation) | Material Design UI norms | **Done** | M3 tokens, MaterialTheme.xaml, ThemeColors |
| [**FR-5.2**](functional-requirements.md#5-user-interface-and-presentation) | Font Awesome for iconography | **Done** | `fa-solid-900`, Icons.xaml |
| [**FR-5.3**](functional-requirements.md#5-user-interface-and-presentation) | Light and dark mode | **Done** | AppThemeBinding, MaterialTheme light/dark |
| [**FR-6.1**](functional-requirements.md#6-deployment-and-distribution-user-facing) | APK deployable via F-Droid–style site on Pages | **Done** | F-Droid repo on Pages, fdroid update in pipeline |
| [**FR-6.2**](functional-requirements.md#6-deployment-and-distribution-user-facing) | Pipeline updates repo and deploys to Pages | **Done** | build-deploy.yml: fdroid-pages + deploy-pages |
| [**FR-7.1**](functional-requirements.md#7-session-and-lifecycle) | Connection = session; agent start/stop with session | **Done** | One stream = one session; START/STOP control |
| [**FR-7.2**](functional-requirements.md#7-session-and-lifecycle) | Session events visible in chat | **Done** | SessionEvent (started/stopped/error) as chat messages |
| [**FR-8.1**](functional-requirements.md#8-extensibility-plugins) | Additional CLI agents via plugins | **Done** | IAgentRunner strategy; ProcessAgentRunner, CopilotWindowsAgentRunner; default RunnerId by OS (Linux→process, Windows→copilot-windows); plugin assemblies in Plugins:Assemblies |
| [**FR-9.1**](functional-requirements.md#9-run-scripts-from-chat) | User can run bash/pwsh script from chat | **Done** | ScriptRequest in proto; /run bash &lt;path&gt; or /run pwsh &lt;path&gt; in app |
| [**FR-9.2**](functional-requirements.md#9-run-scripts-from-chat) | Script stdout/stderr on completion | **Done** | ScriptRunner runs script; server sends output/error messages |
| [**FR-10.1**](functional-requirements.md#10-media-as-agent-context) | User can send images or video as agent context | **Done** | MediaUpload in proto; Attach button, FilePicker; server saves to media/, forwards path to agent |
| [**FR-11.1**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | Client supports multiple sessions with session-id | **Done** | Session list (LiteDB); each session has session_id, title, agent_id; Connect sends session_id and agent_id |
| [**FR-11.1.1**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | Server uses session-id to manage interactions with specific agents | **Done** | Server accepts client session_id on START; resolves runner by agent_id; logs by session_id |
| [**FR-11.1.2**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | App asks which agent to use from list when starting session | **Done** | GetServerInfo returns AvailableAgents; agent picker (DisplayActionSheet) before Connect |
| [**FR-11.1.3**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | Sessions have user-definable title, default first request text | **Done** | Session title in UI; default to first message text; persisted in LocalSessionStore |
| [**FR-11.1.3.1**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | Tap session title → editor, text highlighted, keyboard open | **Done** | Tap SessionTitleLabel → SessionTitleEntry visible and focused; selection on focus |
| [**FR-11.1.3.2**](functional-requirements.md#11-multiple-sessions-and-agent-selection) | Tap off editor → commit updated session title | **Done** | Unfocused/Completed → CommitSessionTitle; UpdateTitle in store |
| [**FR-12.1**](functional-requirements.md#12-desktop-management-app) | Desktop management app replicates core client flows | **Done** | Avalonia desktop app supports connect/start-stop/send/receive flows |
| [**FR-12.1.1**](functional-requirements.md#12-desktop-management-app) | Same chat UI for direct and server modes | **Done** | Unified desktop session/chat surface with mode-specific connection handling |
| [**FR-12.1.2**](functional-requirements.md#12-desktop-management-app) | Desktop prompts for Direct vs Server mode on connect | **Done** | Connection mode selector in desktop workspace/session creation flow |
| [**FR-12.1.3**](functional-requirements.md#12-desktop-management-app) | Tabbed desktop session interface | **Done** | `TabControl` session switcher in desktop shell |
| [**FR-12.1.4**](functional-requirements.md#12-desktop-management-app) | Desktop menu bar, toolbar, command actions | **Done** | MainWindow includes menu + toolbar bound to commands |
| [**FR-12.1.5**](functional-requirements.md#12-desktop-management-app) | Direct terminate action on desktop session tabs | **Done** | Per-tab terminate buttons + command-bound terminate actions |
| [**FR-12.2**](functional-requirements.md#12-desktop-management-app) | Structured log viewer with real-time monitoring/filtering | **Done** | Snapshot + monitor APIs consumed by desktop with filter fields and live updates |
| [**FR-12.3**](functional-requirements.md#12-desktop-management-app) | Desktop ingests structured logs into LiteDB | **Done** | `DesktopStructuredLogStore` persists logs locally |
| [**FR-12.4**](functional-requirements.md#12-desktop-management-app) | Desktop manages server plugin configuration | **Done** | Desktop plugin panel calls list/update plugin APIs |
| [**FR-12.5**](functional-requirements.md#12-desktop-management-app) | Desktop can select/reuse server-side agent plugins | **Done** | Agent selection populated from server runner/plugin IDs |
| [**FR-12.6**](functional-requirements.md#12-desktop-management-app) | Shared interaction library for request/seed/MCP behaviors | **Done** | `RemoteAgent.App.Logic.AgentInteraction*` used by service and desktop |
| [**FR-12.7**](functional-requirements.md#12-desktop-management-app) | Operator panels for peers/ban/history/abandoned/auth management | **Done** | Desktop tabs expose sessions, security, history, and auth management actions |
| [**FR-12.8**](functional-requirements.md#12-desktop-management-app) | Editable per-request context attached to outbound desktop requests | **Done** | Desktop workspace includes request-context editor and request-context dispatch |
| [**FR-12.9**](functional-requirements.md#12-desktop-management-app) | Multiple registered servers with add/update/remove UI | **Done** | Server registration store + desktop selector and CRUD commands |
| [**FR-12.10**](functional-requirements.md#12-desktop-management-app) | Concurrent active connections across different servers | **Done** | Server-scoped workspace leasing/DI supports concurrent server workspaces |
| [**FR-12.11**](functional-requirements.md#12-desktop-management-app) | Structured logs include server identity and default filter to selected server | **Done** | Desktop structured logs persist `ServerId`/display metadata and support server filtering |
| [**FR-12.12**](functional-requirements.md#12-desktop-management-app) | Management App Log view (ILogger capture, clear, export to txt/json/csv) | **Done** | `AppLoggerProvider` + `InMemoryAppLogStore` capture all desktop log messages; `AppLogViewModel` with `ClearAppLogHandler` / `SaveAppLogHandler` (txt/json/csv export via `IFileSaveDialogService`) |
| [**FR-13.1**](functional-requirements.md#13-sessiondeviceadmin-operations) | Query open and abandoned sessions | **Done** | HTTP APIs: `/api/sessions/open`, `/api/sessions/abandoned`; desktop client integration |
| [**FR-13.2**](functional-requirements.md#13-sessiondeviceadmin-operations) | Query connected devices and connection history | **Done** | HTTP APIs: `/api/connections/peers`, `/api/connections/history` |
| [**FR-13.3**](functional-requirements.md#13-sessiondeviceadmin-operations) | Cancel active sessions | **Done** | HTTP API: `/api/sessions/{id}/terminate` |
| [**FR-13.4**](functional-requirements.md#13-sessiondeviceadmin-operations) | Ban/unban specific devices | **Done** | HTTP APIs: `/api/devices/{peer}/ban` POST/DELETE |
| [**FR-13.5**](functional-requirements.md#13-sessiondeviceadmin-operations) | Auth user and permission management | **Done** | Auth-user APIs (`/api/auth/users`, `/api/auth/permissions`) and desktop controls |
| [**FR-13.6**](functional-requirements.md#13-sessiondeviceadmin-operations) | MCP mapping changes notify active sessions | **Done** | AgentGateway emits MCP update notifications to affected sessions |
| [**FR-13.7**](functional-requirements.md#13-sessiondeviceadmin-operations) | Enforce server-wide max concurrent sessions | **Done** | `SessionCapacityService` server cap checks and endpoint reporting |
| [**FR-13.8**](functional-requirements.md#13-sessiondeviceadmin-operations) | Enforce per-agent max sessions without exceeding server cap | **Done** | Effective per-agent capacity bounded by global server capacity |
| [**FR-14.1**](functional-requirements.md#14-prompt-templates) | Clients can access reusable prompt templates | **Done** | gRPC list prompt templates + mobile/desktop consumers |
| [**FR-14.2**](functional-requirements.md#14-prompt-templates) | Prompt templates support Handlebars placeholders | **Done** | `PromptTemplateEngine` variable extraction + Handlebars rendering |
| [**FR-14.3**](functional-requirements.md#14-prompt-templates) | Template submission prompts for required variables | **Done** | Mobile template flow requests each extracted variable via callback UI |
| [**FR-14.4**](functional-requirements.md#14-prompt-templates) | Rendered prompt is submitted via normal send flow | **Done** | Template render sets `PendingMessage` then calls `SendMessageAsync` |
| [**FR-15.1**](functional-requirements.md#15-connection-protection) | Configurable connection/message rate limiting | **Done** | `ConnectionProtectionService` enforces sliding-window limits |
| [**FR-15.2**](functional-requirements.md#15-connection-protection) | Detect DoS patterns and temporarily throttle/block peers | **Done** | DoS detection cooldown + blocked-peer behavior with structured events |
| [**FR-16.1**](functional-requirements.md#16-test-execution-policy) | Integration tests run on-demand and remain isolated from default pipeline | **Done** | `scripts/test-integration.sh` + isolated workflow (`integration-tests.yml`) |

---

## Technical requirements

| ID | Requirement | State | Notes |
|----|-------------|--------|------|
| [**TR-1.1**](technical-requirements.md#1-solution-and-repository) | Single repo, Git | **Done** | remote-agent repo |
| [**TR-1.2**](technical-requirements.md#1-solution-and-repository) | Git ignore (VS/Cursor defaults) | **Done** | .gitignore present |
| [**TR-1.3**](technical-requirements.md#1-solution-and-repository) | .NET 10, slnx | **Done** | RemoteAgent.slnx, net10.0 |
| [**TR-1.4**](technical-requirements.md#1-solution-and-repository) | Hosted on GitHub | **Done** | sharpninja/remote-agent |
| [**TR-1.5**](technical-requirements.md#1-solution-and-repository) | Suitable for GitHub workflows | **Done** | Actions, Pages |
| [**TR-2.1**](technical-requirements.md#2-technology-stack) | Client: .NET MAUI Android | **Done** | net10.0-android |
| [**TR-2.1.1**](technical-requirements.md#2-technology-stack) | Desktop management client: Avalonia UI | **Done** | `src/RemoteAgent.Desktop` (Avalonia) |
| [**TR-2.1.2**](technical-requirements.md#2-technology-stack) | Split SDK orchestration (.NET 10 + .NET 9 tracks) | **Done** | `scripts/build-dotnet10.sh` and `scripts/build-desktop-dotnet9.sh` |
| [**TR-2.2**](technical-requirements.md#2-technology-stack) | Service: .NET, Linux/WSL | **Done** | ASP.NET Core, Docker Linux |
| [**TR-2.3**](technical-requirements.md#2-technology-stack) | gRPC duplex streaming | **Done** | `Connect(stream ClientMessage) returns (stream ServerMessage)` |
| [**TR-2.4**](technical-requirements.md#2-technology-stack) | .NET 10 SDK, Android workload for build | **Done** | Workflow installs maui-android |
| [**TR-3.1**](technical-requirements.md#3-service-architecture) | Service listen host/port | **Done** | Kestrel, configurable (e.g. 5243) |
| [**TR-3.2**](technical-requirements.md#3-service-architecture) | Spawn configurable agent process | **Done** | AgentOptions.Command, RunnerId; process / copilot-windows runners; Command "none" = no agent; empty = runner default |
| [**TR-3.3**](technical-requirements.md#3-service-architecture) | Forward messages to agent stdin | **Done** | Line-oriented write to process |
| [**TR-3.4**](technical-requirements.md#3-service-architecture) | Stream stdout/stderr to app | **Done** | ServerMessage.output / error |
| [**TR-3.5**](technical-requirements.md#3-service-architecture) | Optional message priority on ServerMessage | **Done** | MessagePriority in proto and service |
| [**TR-3.6**](technical-requirements.md#3-service-architecture) | Session log file per connection | **Done** | LogDirectory, remote-agent-{sessionId}.log |
| [**TR-3.7**](technical-requirements.md#3-service-architecture) | Server-wide concurrent session cap | **Done** | Capacity checks and active-session tracking in `SessionCapacityService` |
| [**TR-3.8**](technical-requirements.md#3-service-architecture) | Per-agent session caps bounded by server cap | **Done** | Agent-specific limits computed with server-cap ceiling |
| [**TR-4.1**](technical-requirements.md#4-protocol-grpc) | Proto contract, C# compile | **Done** | AgentGateway.proto, RemoteAgent.Proto |
| [**TR-4.2**](technical-requirements.md#4-protocol-grpc) | ClientMessage: text, control | **Done** | text + SessionControl (START/STOP) |
| [**TR-4.3**](technical-requirements.md#4-protocol-grpc) | ServerMessage: output, error, event, priority | **Done** | All present in proto |
| [**TR-4.4**](technical-requirements.md#4-protocol-grpc) | Duplex streaming RPC | **Done** | Single Connect stream |
| [**TR-4.5**](technical-requirements.md#4-protocol-grpc) | Correlation ID on each request; server echoes on response(s) | **Done** | correlation_id on ClientMessage and ServerMessage; client sets on send; server echoes on all responses and agent stdout/stderr |
| [**TR-5.1**](technical-requirements.md#5-app-architecture) | Chat UI, observable collection | **Done** | Messages collection, bindings |
| [**TR-5.2**](technical-requirements.md#5-app-architecture) | Connect to host/port | **Done** | Settings/preferences, AgentGatewayClientService |
| [**TR-5.3**](technical-requirements.md#5-app-architecture) | Markdown render in chat | **Done** | Markdig, ChatMessageToHtmlSourceConverter, WebView |
| [**TR-5.4**](technical-requirements.md#5-app-architecture) | Notify → system notification; tap → open and show | **Done** | Android notification + open app |
| [**TR-5.5**](technical-requirements.md#5-app-architecture) | Swipe to archive; archived hidden | **Done** | SwipeView + IsArchived filtering |
| [**TR-5.6**](technical-requirements.md#5-app-architecture) | Multi-line chat input preserving newlines | **Done** | Multi-line chat editors on mobile/desktop |
| [**TR-5.7**](technical-requirements.md#5-app-architecture) | Desktop Ctrl+Enter submit / Enter newline | **Done** | Desktop key binding sends on `Ctrl+Enter` |
| [**TR-5.8**](technical-requirements.md#5-app-architecture) | Mobile connection-first UX before chat workspace | **Done** | MainPage toggles connect-first surface prior to active session |
| [**TR-6.1**](technical-requirements.md#6-docker-and-containerization) | Service containerizable (Dockerfile) | **Done** | Multi-stage Dockerfile |
| [**TR-6.2**](technical-requirements.md#6-docker-and-containerization) | Container port + env config | **Done** | 5243, Agent__Command, Agent__LogDirectory |
| [**TR-6.3**](technical-requirements.md#6-docker-and-containerization) | Pipeline build and publish image to registry | **Done** | GHCR in build-deploy.yml |
| [**TR-7.1**](technical-requirements.md#7-cicd-and-deployment) | GitHub Actions build APK | **Done** | android job |
| [**TR-7.2**](technical-requirements.md#7-cicd-and-deployment) | Workflow build solution and APK | **Done** | build + android jobs |
| [**TR-7.3**](technical-requirements.md#7-cicd-and-deployment) | GitHub Actions build container, GHCR | **Done** | docker job, push to ghcr.io |
| [**TR-7.3.1**](technical-requirements.md#7-cicd-and-deployment) | Build and publish Docker image | **Done** | docker/build-push-action |
| [**TR-7.3.2**](technical-requirements.md#7-cicd-and-deployment) | APK as F-Droid repo on Pages | **Done** | fdroid update, deploy artifact; pipeline verifies index APK hash |
| [**TR-7.3.3**](technical-requirements.md#7-cicd-and-deployment) | Update F-Droid–style repo, deploy Pages | **Done** | fdroid-pages job, deploy-pages |
| [**TR-7.3.4**](technical-requirements.md#7-cicd-and-deployment) | Pages source = GitHub Actions | **Done** | Configured (workflow) |
| [**TR-7.3.5**](technical-requirements.md#7-cicd-and-deployment) | DocFX docs on Pages | **Done** | DocFX build, merged into artifact |
| [**TR-8.1**](technical-requirements.md#8-testing) | Unit and integration tests | **Done** | App.Tests + Service.Tests (unit); RemoteAgent.Service.IntegrationTests (integration, separate project); CI runs unit test projects only |
| [**TR-8.2**](technical-requirements.md#8-testing) | Unit: markdown, chat, priority/archive, config | **Done** | MarkdownFormatTests, ChatMessageTests, AgentOptionsTests |
| [**TR-8.3**](technical-requirements.md#8-testing) | Integration: no command → error; echo; start/stop | **Done** | NoCommand, Echo, Stop, GetServerInfo; strategy default agent; tests accept "did not start" when agent unavailable |
| [**TR-8.4**](technical-requirements.md#8-testing) | Tests runnable via solution | **Done** | dotnet test RemoteAgent.slnx |
| [**TR-8.4.1**](technical-requirements.md#8-testing) | Integration tests isolated from default CI entrypoint | **Done** | `scripts/test-integration.sh` and isolated integration workflow |
| [**TR-8.5**](technical-requirements.md#8-testing) | Automated mobile UI tests for core connection screen | **Done** | `RemoteAgent.Mobile.UiTests` (Appium) validates host/port/connect controls |
| [**TR-8.6**](technical-requirements.md#8-testing) | Automated desktop UI tests for shell controls | **Done** | `RemoteAgent.Desktop.UiTests` (Avalonia.Headless) covers shell/session controls |
| [**TR-9.1**](technical-requirements.md#9-ui-and-assets) | Font Awesome iconography | **Done** | fa-solid-900, Icons.xaml |
| [**TR-9.2**](technical-requirements.md#9-ui-and-assets) | Material Design norms | **Done** | M3 tokens, MaterialTheme, ThemeColors |
| [**TR-9.3**](technical-requirements.md#9-ui-and-assets) | Light/dark theming | **Done** | AppThemeBinding, ThemeColors |
| [**TR-10.1**](technical-requirements.md#10-extensibility-plugins--fr-81) | Plugins via strategy pattern (FR-8.1) | **Done** | IAgentSession, IAgentRunner; ProcessAgentRunner, CopilotWindowsAgentRunner; DefaultAgentRunnerFactory (OS default RunnerId); gateway uses IAgentRunnerFactory |
| [**TR-10.2**](technical-requirements.md#10-extensibility-plugins--fr-81) | Plugin discovery via appsettings (assemblies) | **Done** | Plugins:Assemblies; PluginLoader (process + copilot-windows built-in); Agent:RunnerId |
| [**TR-11.1**](technical-requirements.md#11-local-storage-litedb) | App and server use LiteDB for requests/results | **Done** | Server: LiteDbLocalStorage, request/response log; App: LocalMessageStore, load/add/archive |
| [**TR-11.2**](technical-requirements.md#11-local-storage-litedb) | Uploaded images/videos stored alongside LiteDB on server | **Done** | MediaStorageService saves to DataDirectory/media/ |
| [**TR-11.3**](technical-requirements.md#11-local-storage-litedb) | Images to app stored in DCIM/Remote Agent | **Done** | MediaSaveService.SaveToDcimRemoteAgent (Android DCIM/Remote Agent); ServerMessage.media handled |
| [**TR-12.1**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | Protocol: SessionControl carries session_id and agent_id (START) | **Done** | SessionControl.session_id and agent_id in proto and generated C# |
| [**TR-12.1.1**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | Server: map session_id → agent session; route messages by session_id | **Done** | One session per stream; client session_id used for logging; runner selected by agent_id |
| [**TR-12.1.2**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | Server: expose list of configured agents to client | **Done** | GetServerInfo.AvailableAgents (runner registry keys) |
| [**TR-12.1.3**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | App: session list; persist session_id, title, agent_id in local storage | **Done** | LocalSessionStore (LiteDB); Sessions list; messages per session (SessionId in StoredMessageRecord) |
| [**TR-12.2**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | App: agent picker at session start; send START with session_id and agent_id | **Done** | GetServerInfoAsync then DisplayActionSheet; ConnectAsync(sessionId, agentId) |
| [**TR-12.2.1**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | App: session title (user-definable, default first request); stored and displayed | **Done** | Session title in header; default from first message; UpdateTitle in store |
| [**TR-12.2.2**](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111) | App: tap title → inline editor (selected, keyboard); tap off → commit | **Done** | OnSessionTitleLabelTapped; CommitSessionTitle on Unfocused/Completed |
| [**TR-13.1**](technical-requirements.md#13-observability-and-structured-logging) | Server emits append-only structured JSONL logs | **Done** | `StructuredLogService` writes JSONL records |
| [**TR-13.2**](technical-requirements.md#13-observability-and-structured-logging) | Structured logs include session_id and correlation_id fields | **Done** | Structured log records include both fields with empty/null semantics when unavailable |
| [**TR-13.3**](technical-requirements.md#13-observability-and-structured-logging) | Structured logs queryable by snapshot and real-time stream | **Done** | gRPC snapshot + monitor APIs for structured logs |
| [**TR-13.4**](technical-requirements.md#13-observability-and-structured-logging) | Desktop ingests structured logs into LiteDB collections | **Done** | Desktop structured log store persists ingested records |
| [**TR-13.5**](technical-requirements.md#13-observability-and-structured-logging) | Desktop log viewer supports robust filtering | **Done** | Desktop filter fields include level/event/session/correlation/component/server/text/time |
| [**TR-13.6**](technical-requirements.md#13-observability-and-structured-logging) | Desktop structured logs include server identity metadata | **Done** | `DesktopStructuredLogRecord` includes server id/display/host/port metadata |
| [**TR-14.1**](technical-requirements.md#14-desktop-management-capabilities) | Desktop supports mobile-equivalent connection/session controls | **Done** | Desktop supports connect/start-stop/send/receive flows |
| [**TR-14.1.0**](technical-requirements.md#14-desktop-management-capabilities) | Desktop hosted in `src/RemoteAgent.Desktop` with Avalonia + DI VMs | **Done** | Project and DI registrations are in place |
| [**TR-14.1.1**](technical-requirements.md#14-desktop-management-capabilities) | One shared chat surface for direct/server modes | **Done** | Shared `TabControl` session/chat content for both modes |
| [**TR-14.1.2**](technical-requirements.md#14-desktop-management-capabilities) | Connect flow requires Direct/Server mode selection | **Done** | Session creation/connection mode surfaced in desktop UI/view model |
| [**TR-14.1.3**](technical-requirements.md#14-desktop-management-capabilities) | Session switcher implemented as tabs | **Done** | Desktop session tabs implemented via `TabControl` |
| [**TR-14.1.4**](technical-requirements.md#14-desktop-management-capabilities) | Desktop includes menu bar + toolbar command bindings | **Done** | MainWindow menu/toolbar command wiring |
| [**TR-14.1.5**](technical-requirements.md#14-desktop-management-capabilities) | View models resolved/lifecycle-managed by DI container | **Done** | DI setup in `App.axaml.cs`; pages do not directly construct VMs |
| [**TR-14.1.6**](technical-requirements.md#14-desktop-management-capabilities) | Session tabs include terminate actions | **Done** | Global and per-tab terminate commands/buttons implemented |
| [**TR-14.1.7**](technical-requirements.md#14-desktop-management-capabilities) | Desktop includes per-request context editor bound to VM state | **Done** | Request-context field in desktop workspace and outbound request handling |
| [**TR-14.1.8**](technical-requirements.md#14-desktop-management-capabilities) | Desktop supports server registration management | **Done** | Add/edit/remove server registrations and selector controls |
| [**TR-14.1.9**](technical-requirements.md#14-desktop-management-capabilities) | Server-specific state scoped via DI/context | **Done** | `CurrentServerContext` and scoped workspace factory pattern |
| [**TR-14.1.10**](technical-requirements.md#14-desktop-management-capabilities) | Multiple server workspaces remain concurrently active | **Done** | Server workspace leasing keeps server-scoped state concurrently available |
| [**TR-14.2**](technical-requirements.md#14-desktop-management-capabilities) | Desktop retrieves/updates plugin assembly configuration APIs | **Done** | Desktop plugin management calls list/update plugin endpoints |
| [**TR-14.3**](technical-requirements.md#14-desktop-management-capabilities) | Desktop uses server-provided runner/plugin IDs for agent selection | **Done** | Agent IDs sourced from server info/runner registry |
| [**TR-14.4**](technical-requirements.md#14-desktop-management-capabilities) | Plugin updates surface restart-required feedback | **Done** | Update plugin response/status indicates restart needed for new plugin load |
| [**TR-14.5**](technical-requirements.md#14-desktop-management-capabilities) | Desktop session metadata includes connection mode | **Done** | Session VM/factory tracks and persists connection mode metadata |
| [**TR-15.1**](technical-requirements.md#15-management-apis-and-policy-controls) | Management APIs for sessions/devices/history | **Done** | Session/device/history HTTP endpoints implemented |
| [**TR-15.2**](technical-requirements.md#15-management-apis-and-policy-controls) | Control APIs for terminate + ban/unban | **Done** | Terminate and ban/unban endpoints implemented |
| [**TR-15.3**](technical-requirements.md#15-management-apis-and-policy-controls) | Auth user/permission APIs with server-side authorization controls | **Done** | Auth user service and permission endpoints integrated with API-key validation |
| [**TR-15.4**](technical-requirements.md#15-management-apis-and-policy-controls) | Management actions logged as structured audit events | **Done** | Management endpoints and services write structured audit log events |
| [**TR-15.5**](technical-requirements.md#15-management-apis-and-policy-controls) | Rate limits for connection attempts and inbound messages | **Done** | Connection/message rate limiting in `ConnectionProtectionService` |
| [**TR-15.6**](technical-requirements.md#15-management-apis-and-policy-controls) | Repeat violations trigger DoS detection and temporary blocking | **Done** | DoS detection and cooldown blocking with structured events |
| [**TR-15.7**](technical-requirements.md#15-management-apis-and-policy-controls) | Session-capacity preflight endpoint | **Done** | `/api/sessions/capacity` endpoint and desktop consumption |
| [**TR-15.8**](technical-requirements.md#15-management-apis-and-policy-controls) | HTTP endpoints for open/abandoned/terminate sessions | **Done** | `/api/sessions/open`, `/api/sessions/abandoned`, `/api/sessions/{id}/terminate` |
| [**TR-15.9**](technical-requirements.md#15-management-apis-and-policy-controls) | HTTP endpoints for peers/history/ban lifecycle | **Done** | `/api/connections/peers`, `/api/connections/history`, `/api/devices/{peer}/ban` |
| [**TR-15.10**](technical-requirements.md#15-management-apis-and-policy-controls) | HTTP endpoints for auth users/permissions with audit logging | **Done** | `/api/auth/users`, `/api/auth/permissions` endpoints with structured logs |
| [**TR-16.1**](technical-requirements.md#16-shared-agent-interaction-library) | Shared synthetic request/seed/MCP input envelope library | **Done** | `AgentInteractionProtocol` defines shared envelope messages |
| [**TR-16.2**](technical-requirements.md#16-shared-agent-interaction-library) | Shared transport-agnostic session interface/dispatcher | **Done** | `IAgentInteractionSession` + `AgentInteractionDispatcher` abstractions |
| [**TR-16.3**](technical-requirements.md#16-shared-agent-interaction-library) | Server delegates request-context/seed/MCP behavior via shared library | **Done** | AgentGateway routes these behaviors through shared dispatcher |
| [**TR-16.4**](technical-requirements.md#16-shared-agent-interaction-library) | MCP updates compute deltas and notify affected active sessions | **Done** | MCP mapping update path computes changes and notifies bound sessions |
| [**TR-16.5**](technical-requirements.md#16-shared-agent-interaction-library) | Desktop/server actions encapsulated via reusable shared contracts | **Done** | Desktop and service use shared interaction/session abstractions |
| [**TR-16.6**](technical-requirements.md#16-shared-agent-interaction-library) | Mobile/desktop use shared server-access library for gRPC APIs | **Done** | Shared `ServerApiClient` used by app and desktop infrastructure |
| [**TR-16.7**](technical-requirements.md#16-shared-agent-interaction-library) | Mobile/desktop share session-stream client implementation | **Done** | Shared `AgentSessionClient` consumed by both platforms |
| [**TR-17.1**](technical-requirements.md#17-prompt-template-system) | Service exposes prompt-template list/upsert/delete gRPC APIs | **Done** | AgentGateway prompt-template RPCs implemented and tested |
| [**TR-17.2**](technical-requirements.md#17-prompt-template-system) | Prompt templates persisted in LiteDB with metadata/timestamps | **Done** | `PromptTemplateService` persists full template records |
| [**TR-17.3**](technical-requirements.md#17-prompt-template-system) | Client rendering uses Handlebars-compatible eval + variable extraction | **Done** | `PromptTemplateEngine` extraction + render behavior |
| [**TR-17.4**](technical-requirements.md#17-prompt-template-system) | Client UX collects variables then sends rendered template text | **Done** | Mobile template flow prompts variables then submits rendered message |
| [**TR-18.1**](technical-requirements.md#18-ui-commandevent-cqrs-testability) | UI commands/events use CQRS split with handler components | **Done** | Every Desktop and Mobile UI command dispatches through `IRequestDispatcher`; 32 Desktop handlers + 17 Mobile handlers; `ServerWorkspaceViewModel` decomposed into 6 focused sub-VMs |
| [**TR-18.2**](technical-requirements.md#18-ui-commandevent-cqrs-testability) | Command/query/event handlers are unit-testable independent of UI frameworks | **Done** | 218 handler unit tests across Desktop and Mobile; handlers tested with stub dependencies, no UI framework required |
| [**TR-18.3**](technical-requirements.md#18-ui-commandevent-cqrs-testability) | UI pipelines support mockable behavior injection for known outcomes/failures | **Done** | `IRequestDispatcher` is the sole pipeline entry point; `ServiceProviderRequestDispatcher` provides Debug-level entry/exit logging with CorrelationId tracing; all dependencies are interface-backed |
| [**TR-18.4**](technical-requirements.md#18-ui-commandevent-cqrs-testability) | UI tests substitute mocked handlers and validate success/failure UI behavior | **Done** | Handler tests use `StubCapacityClient`, `NullDispatcher`, `TestRequestDispatcher`, and other injectable stubs to validate both success and failure paths; 240 tests covering all handler paths |

---

## Summary counts

| Category | Done | Partial | Not started |
|----------|------|--------|-------------|
| **Functional (FR)** | 67 | 0 | 0 |
| **Technical (TR)** | 112 | 0 | 0 |

**Not started (FR):** None.

**Not started (TR):** None.

---

*Generated from `docs/functional-requirements.md`, `docs/technical-requirements.md`, and the current codebase. Last refreshed: TR-18.1–TR-18.4 marked Done following completion of the MVVM + CQRS refactor (32 Desktop handlers, 17 Mobile handlers, 218 unit tests, ServerWorkspaceViewModel decomposed into 6 sub-VMs, IRequestDispatcher with Debug-level CorrelationId tracing).*
