# Requirements completion summary

Status of all functional (FR) and technical (TR) requirements as of the current codebase. **Done** = implemented; **Partial** = partly done or only in docs/config; **Not started** = no implementation.

---

## Functional requirements

| ID | Requirement | State | Notes |
|----|-------------|--------|------|
| **FR-1.1** | Android app communicating with Linux service | **Done** | MAUI Android app + ASP.NET Core service |
| **FR-1.2** | Service spawns Cursor agent and establishes communication | **Done** | Configurable agent process (e.g. Cursor), stdin/stdout |
| **FR-1.3** | Service forwards user messages to the agent | **Done** | `ClientMessage.text` → agent stdin |
| **FR-1.4** | Service sends agent output to app in real time | **Done** | `ServerMessage.output` / `error` streamed |
| **FR-1.5** | Service logs all interaction | **Done** | Session log file per connection (`LogDirectory`, `remote-agent-{sessionId}.log`) |
| **FR-1.6** | Chat-based UI for all interaction | **Done** | MainPage chat list + input |
| **FR-2.1** | User can send messages from app to agent | **Done** | Text input → gRPC → agent |
| **FR-2.2** | Agent output streamed in real time and displayed | **Done** | Stream bound to observable collection, WebView |
| **FR-2.3** | Agent output formatted with markdown | **Done** | Markdig in App.Logic, WebView render |
| **FR-2.4** | Connect / disconnect; session start/stop | **Done** | Connect/Disconnect UI; `SessionControl` START/STOP |
| **FR-3.1** | Server supports message priority (normal, high, notify) | **Done** | `MessagePriority` in proto and server |
| **FR-3.2** | Notify priority → system notification | **Done** | Android `PlatformNotificationService`, channel |
| **FR-3.3** | Tap notification → open app, message visible | **Done** | Notification opens app; chat shows message |
| **FR-3.4** | Normal/high supported in protocol and model | **Done** | Enum and binding in app |
| **FR-4.1** | Swipe message to archive | **Done** | SwipeView left/right, Archive command |
| **FR-4.2** | Archived hidden from main list | **Done** | `IsArchived`; filtered in UI |
| **FR-5.1** | Material Design UI norms | **Done** | M3 tokens, MaterialTheme.xaml, ThemeColors |
| **FR-5.2** | Font Awesome for iconography | **Done** | `fa-solid-900`, Icons.xaml |
| **FR-5.3** | Light and dark mode | **Done** | AppThemeBinding, MaterialTheme light/dark |
| **FR-6.1** | APK deployable via F-Droid–style site on Pages | **Done** | F-Droid repo on Pages, fdroid update in pipeline |
| **FR-6.2** | Pipeline updates repo and deploys to Pages | **Done** | build-deploy.yml: fdroid-pages + deploy-pages |
| **FR-7.1** | Connection = session; agent start/stop with session | **Done** | One stream = one session; START/STOP control |
| **FR-7.2** | Session events visible in chat | **Done** | SessionEvent (started/stopped/error) as chat messages |
| **FR-8.1** | Additional CLI agents via plugins | **Done** | IAgentRunner strategy; plugin assemblies in Plugins:Assemblies |
| **FR-9.1** | User can run bash/pwsh script from chat | **Done** | ScriptRequest in proto; /run bash &lt;path&gt; or /run pwsh &lt;path&gt; in app |
| **FR-9.2** | Script stdout/stderr on completion | **Done** | ScriptRunner runs script; server sends output/error messages |
| **FR-10.1** | User can send images or video as agent context | **Not started** | No media in proto or app send path |

---

## Technical requirements

| ID | Requirement | State | Notes |
|----|-------------|--------|------|
| **TR-1.1** | Single repo, Git | **Done** | remote-agent repo |
| **TR-1.2** | Git ignore (VS/Cursor defaults) | **Done** | .gitignore present |
| **TR-1.3** | .NET 10, slnx | **Done** | RemoteAgent.slnx, net10.0 |
| **TR-1.4** | Hosted on GitHub | **Done** | sharpninja/remote-agent |
| **TR-1.5** | Suitable for GitHub workflows | **Done** | Actions, Pages |
| **TR-2.1** | Client: .NET MAUI Android | **Done** | net10.0-android |
| **TR-2.2** | Service: .NET, Linux/WSL | **Done** | ASP.NET Core, Docker Linux |
| **TR-2.3** | gRPC duplex streaming | **Done** | `Connect(stream ClientMessage) returns (stream ServerMessage)` |
| **TR-2.4** | .NET 10 SDK, Android workload for build | **Done** | Workflow installs maui-android |
| **TR-3.1** | Service listen host/port | **Done** | Kestrel, configurable (e.g. 5243) |
| **TR-3.2** | Spawn configurable agent process | **Done** | AgentOptions.Command, Process |
| **TR-3.3** | Forward messages to agent stdin | **Done** | Line-oriented write to process |
| **TR-3.4** | Stream stdout/stderr to app | **Done** | ServerMessage.output / error |
| **TR-3.5** | Optional message priority on ServerMessage | **Done** | MessagePriority in proto and service |
| **TR-3.6** | Session log file per connection | **Done** | LogDirectory, remote-agent-{sessionId}.log |
| **TR-4.1** | Proto contract, C# compile | **Done** | AgentGateway.proto, RemoteAgent.Proto |
| **TR-4.2** | ClientMessage: text, control | **Done** | text + SessionControl (START/STOP) |
| **TR-4.3** | ServerMessage: output, error, event, priority | **Done** | All present in proto |
| **TR-4.4** | Duplex streaming RPC | **Done** | Single Connect stream |
| **TR-5.1** | Chat UI, observable collection | **Done** | Messages collection, bindings |
| **TR-5.2** | Connect to host/port | **Done** | Settings/preferences, AgentGatewayClientService |
| **TR-5.3** | Markdown render in chat | **Done** | Markdig, ChatMessageToHtmlSourceConverter, WebView |
| **TR-5.4** | Notify → system notification; tap → open and show | **Done** | Android notification + open app |
| **TR-5.5** | Swipe to archive; archived hidden | **Done** | SwipeView + IsArchived filtering |
| **TR-6.1** | Service containerizable (Dockerfile) | **Done** | Multi-stage Dockerfile |
| **TR-6.2** | Container port + env config | **Done** | 5243, Agent__Command, Agent__LogDirectory |
| **TR-6.3** | Pipeline build and publish image to registry | **Done** | GHCR in build-deploy.yml |
| **TR-7.1** | GitHub Actions build APK | **Done** | android job |
| **TR-7.2** | Workflow build solution and APK | **Done** | build + android jobs |
| **TR-7.3** | GitHub Actions build container, GHCR | **Done** | docker job, push to ghcr.io |
| **TR-7.3.1** | Build and publish Docker image | **Done** | docker/build-push-action |
| **TR-7.3.2** | APK as F-Droid repo on Pages | **Done** | fdroid update, deploy artifact |
| **TR-7.3.3** | Update F-Droid–style repo, deploy Pages | **Done** | fdroid-pages job, deploy-pages |
| **TR-7.3.4** | Pages source = GitHub Actions | **Done** | Configured (workflow) |
| **TR-7.3.5** | DocFX docs on Pages | **Done** | DocFX build, merged into artifact |
| **TR-8.1** | Unit and integration tests | **Done** | App.Tests + Service.Tests |
| **TR-8.2** | Unit: markdown, chat, priority/archive, config | **Done** | MarkdownFormatTests, ChatMessageTests, AgentOptionsTests |
| **TR-8.3** | Integration: no command → error; /bin/cat echo; start/stop | **Done** | NoCommand, Echo, Stop integration tests |
| **TR-8.4** | Tests runnable via solution | **Done** | dotnet test RemoteAgent.slnx |
| **TR-9.1** | Font Awesome iconography | **Done** | fa-solid-900, Icons.xaml |
| **TR-9.2** | Material Design norms | **Done** | M3 tokens, MaterialTheme, ThemeColors |
| **TR-9.3** | Light/dark theming | **Done** | AppThemeBinding, ThemeColors |
| **TR-10.1** | Plugins via strategy pattern (FR-8.1) | **Done** | IAgentSession, IAgentRunner, ProcessAgentRunner; gateway uses IAgentRunnerFactory |
| **TR-10.2** | Plugin discovery via appsettings (assemblies) | **Done** | Plugins:Assemblies; PluginLoader.BuildRunnerRegistry; Agent:RunnerId |
| **TR-11.1** | App and server use LiteDB for requests/results | **Not started** | No LiteDB references in code |
| **TR-11.2** | Uploaded images/videos stored alongside LiteDB on server | **Not started** | No media storage (depends on TR-11.1, FR-10.1) |
| **TR-11.3** | Images to app stored in DCIM/Remote Agent | **Not started** | No image receive/save (depends on FR-10.1) |

---

## Summary counts

| Category | Done | Partial | Not started |
|----------|------|--------|-------------|
| **Functional (FR)** | 27 | 0 | 2 |
| **Technical (TR)** | 44 | 0 | 3 |

**Not started (FR):** FR-10.1 (send images/video as agent context).

**Not started (TR):** TR-11.1 (LiteDB), TR-11.2 (server media storage), TR-11.3 (DCIM/Remote Agent for received images).

---

*Generated from `docs/functional-requirements.md`, `docs/technical-requirements.md`, and the current codebase.*
