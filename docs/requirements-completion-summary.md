# Requirements completion summary

Status of all functional (FR) and technical (TR) requirements as of the current codebase. **Done** = implemented; **Partial** = partly done or only in docs/config; **Not started** = no implementation.

Requirement IDs in the tables link to the corresponding section in [Functional requirements](functional-requirements.md) or [Technical requirements](technical-requirements.md); those documents also cross-link FR↔TR where they relate.

*Refreshed to reflect: agent strategy (process / copilot-windows, OS default RunnerId), GetServerInfo, CI unit-test-only step, integration tests environment-agnostic with optional "did not start" handling.*

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
| [**TR-2.2**](technical-requirements.md#2-technology-stack) | Service: .NET, Linux/WSL | **Done** | ASP.NET Core, Docker Linux |
| [**TR-2.3**](technical-requirements.md#2-technology-stack) | gRPC duplex streaming | **Done** | `Connect(stream ClientMessage) returns (stream ServerMessage)` |
| [**TR-2.4**](technical-requirements.md#2-technology-stack) | .NET 10 SDK, Android workload for build | **Done** | Workflow installs maui-android |
| [**TR-3.1**](technical-requirements.md#3-service-architecture) | Service listen host/port | **Done** | Kestrel, configurable (e.g. 5243) |
| [**TR-3.2**](technical-requirements.md#3-service-architecture) | Spawn configurable agent process | **Done** | AgentOptions.Command, RunnerId; process / copilot-windows runners; Command "none" = no agent; empty = runner default |
| [**TR-3.3**](technical-requirements.md#3-service-architecture) | Forward messages to agent stdin | **Done** | Line-oriented write to process |
| [**TR-3.4**](technical-requirements.md#3-service-architecture) | Stream stdout/stderr to app | **Done** | ServerMessage.output / error |
| [**TR-3.5**](technical-requirements.md#3-service-architecture) | Optional message priority on ServerMessage | **Done** | MessagePriority in proto and service |
| [**TR-3.6**](technical-requirements.md#3-service-architecture) | Session log file per connection | **Done** | LogDirectory, remote-agent-{sessionId}.log |
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
| [**TR-6.1**](technical-requirements.md#6-docker-and-containerization) | Service containerizable (Dockerfile) | **Done** | Multi-stage Dockerfile |
| [**TR-6.2**](technical-requirements.md#6-docker-and-containerization) | Container port + env config | **Done** | 5243, Agent__Command, Agent__LogDirectory |
| [**TR-6.3**](technical-requirements.md#6-docker-and-containerization) | Pipeline build and publish image to registry | **Done** | GHCR in build-deploy.yml |
| [**TR-7.1**](technical-requirements.md#7-cicd-and-deployment) | GitHub Actions build APK | **Done** | android job |
| [**TR-7.2**](technical-requirements.md#7-cicd-and-deployment) | Workflow build solution and APK | **Done** | build + android jobs |
| [**TR-7.3**](technical-requirements.md#7-cicd-and-deployment) | GitHub Actions build container, GHCR | **Done** | docker job, push to ghcr.io |
| [**TR-7.3.1**](technical-requirements.md#7-cicd-and-deployment) | Build and publish Docker image | **Done** | docker/build-push-action |
| [**TR-7.3.2**](technical-requirements.md#7-cicd-and-deployment) | APK as F-Droid repo on Pages | **Done** | fdroid update, deploy artifact |
| [**TR-7.3.3**](technical-requirements.md#7-cicd-and-deployment) | Update F-Droid–style repo, deploy Pages | **Done** | fdroid-pages job, deploy-pages |
| [**TR-7.3.4**](technical-requirements.md#7-cicd-and-deployment) | Pages source = GitHub Actions | **Done** | Configured (workflow) |
| [**TR-7.3.5**](technical-requirements.md#7-cicd-and-deployment) | DocFX docs on Pages | **Done** | DocFX build, merged into artifact |
| [**TR-8.1**](technical-requirements.md#8-testing) | Unit and integration tests | **Done** | App.Tests + Service.Tests; CI runs unit tests only (filter excludes IntegrationTests) |
| [**TR-8.2**](technical-requirements.md#8-testing) | Unit: markdown, chat, priority/archive, config | **Done** | MarkdownFormatTests, ChatMessageTests, AgentOptionsTests |
| [**TR-8.3**](technical-requirements.md#8-testing) | Integration: no command → error; echo; start/stop | **Done** | NoCommand, Echo, Stop, GetServerInfo; strategy default agent; tests accept "did not start" when agent unavailable |
| [**TR-8.4**](technical-requirements.md#8-testing) | Tests runnable via solution | **Done** | dotnet test RemoteAgent.slnx |
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

---

## Summary counts

| Category | Done | Partial | Not started |
|----------|------|--------|-------------|
| **Functional (FR)** | 34 | 0 | 0 |
| **Technical (TR)** | 56 | 0 | 0 |

**Not started (FR):** None.

**Not started (TR):** None.

---

*Generated from `docs/functional-requirements.md`, `docs/technical-requirements.md`, and the current codebase. Last refreshed for TR-4.5 (correlation_id on ClientMessage and ServerMessage; client sets on every request; server echoes on all responses).*
