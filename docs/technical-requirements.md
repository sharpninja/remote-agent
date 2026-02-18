# Technical Requirements

**Source:** Recovered from Project Initializer session (Cursor chat).
**Product:** Remote Agent — Android app and Linux service for communicating with a Cursor agent.

---

## 1. Solution and repository

- **TR-1.1** The project shall live in a **single repository** (e.g. a folder such as `remote-agent`), with **Git** version control.
- **TR-1.2** Git shall be initialized with **Visual Studio and Cursor AI gitignore defaults**.
- **TR-1.3** The codebase shall use a **.NET 10 solution**; the solution shall be in **slnx format** (e.g. `RemoteAgent.slnx`).
- **TR-1.4** The repository shall be **hosted on GitHub**.
- **TR-1.5** The repository shall be suitable for GitHub workflows (e.g. structure, CI/CD, and deployment aligned with GitHub Actions and GitHub Pages).

*See:* [FR-1](functional-requirements.md#1-product-purpose).

---

## 2. Technology stack

- **TR-2.1** The **client** shall be built with **.NET MAUI** targeting **Android** (e.g. `net10.0-android`).
- **TR-2.1.1** The **desktop management client** shall be built with **Avalonia UI** (e.g. project `src/RemoteAgent.Desktop`).
- **TR-2.1.2** Build automation shall support **split SDK orchestration**: MAUI/service stacks built with **.NET 10**, Avalonia desktop stack built with **.NET 9**.
- **TR-2.2** The **service** shall be a **.NET** application (e.g. ASP.NET Core) targeting **Linux** (and runnable on **WSL** or native Linux).
- **TR-2.3** Communication between the app and the service shall use **gRPC** with **persistent bidirectional streaming** for requests and responses (e.g. a single `Connect(stream ClientMessage) returns (stream ServerMessage)` RPC).
- **TR-2.4** The build environment shall include the **.NET 10 SDK** (MAUI/service/shared) and **.NET 9 SDK** (Avalonia desktop), and the **Android workload** (`maui-android`) plus **Android SDK** for mobile builds.

*See:* [FR-1](functional-requirements.md#1-product-purpose), [FR-2](functional-requirements.md#2-chat-and-messaging).

---

## 3. Service architecture

- **TR-3.1** The service shall **listen** for client connections (e.g. on a configurable host/port, such as `http://0.0.0.0:5243` for gRPC over HTTP/2).
- **TR-3.2** The service shall **spawn a configurable agent process** (e.g. Cursor agent or a test process like `/bin/cat`) when a session starts.
- **TR-3.3** The service shall **forward** app messages (e.g. `ClientMessage.text`) to the agent’s **stdin** (line-oriented).
- **TR-3.4** The service shall **stream** the agent’s **stdout** and **stderr** back to the app (e.g. as `ServerMessage.output` and `ServerMessage.error`).
- **TR-3.5** The service shall support an optional **message priority** on each `ServerMessage` (e.g. `NORMAL`, `HIGH`, `NOTIFY`) so that the app can implement notifications and prioritization.
- **TR-3.6** The service shall **write a session log file** per connection (e.g. under a configurable `LogDirectory`, with a name such as `remote-agent-{sessionId}.log`), logging session lifecycle and message flow.
- **TR-3.7** The service shall enforce a configurable **server-wide concurrent session cap**.
- **TR-3.8** The service shall support configurable **per-agent concurrent session caps**; effective per-agent capacity shall never exceed the server-wide cap.

*See:* [FR-1](functional-requirements.md#1-product-purpose), [FR-2](functional-requirements.md#2-chat-and-messaging), [FR-3](functional-requirements.md#3-message-priority-and-notifications), [FR-7](functional-requirements.md#7-session-and-lifecycle).

---

## 4. Protocol (gRPC)

- **TR-4.1** The **contract** shall be defined in a shared **Protocol Buffers** file (e.g. `AgentGateway.proto`) and compiled for C# (e.g. in a `RemoteAgent.Proto` project).
- **TR-4.2** **ClientMessage** shall support: (a) **text** — user message to forward to the agent; (b) **control** — start or stop the session.
- **TR-4.3** **ServerMessage** shall support: (a) **output** — agent stdout line; (b) **error** — agent stderr line; (c) **event** — session lifecycle (e.g. session started, stopped, error); (d) **priority** — optional level (e.g. normal, high, notify) for the message.
- **TR-4.4** The RPC shall be **duplex streaming** so that the client can send messages and receive server messages over the same connection without opening multiple calls per message.
- **TR-4.5** A **correlation ID** shall be added to **each request** (e.g. on `ClientMessage` or on each payload that expects a response); the **server** shall **echo or carry** the same correlation ID on the **corresponding asynchronous response(s)** (e.g. on `ServerMessage`) so the client can **match responses to requests**.

*See:* [FR-2](functional-requirements.md#2-chat-and-messaging), [FR-3](functional-requirements.md#3-message-priority-and-notifications), [FR-7](functional-requirements.md#7-session-and-lifecycle), [FR-9](functional-requirements.md#9-run-scripts-from-chat).

---

## 5. App architecture

- **TR-5.1** The app shall use a **chat UI** (e.g. a list of messages and an input field) bound to an **observable collection** of chat messages.
- **TR-5.2** The app shall **connect** to the service using the configured host and port (e.g. emulator: `10.0.2.2:5243`; device: host machine IP and port).
- **TR-5.3** Agent output (and errors) shall be **rendered in the chat** using a **markdown parser** (e.g. Markdig or equivalent) to produce HTML or formatted text for display (e.g. in a WebView or rich control).
- **TR-5.4** When the app receives a message with **notify** priority, it shall **show a system notification** (e.g. on Android, using a notification channel and `NotificationManager`/`NotificationCompat`); tapping the notification shall open the app so the message is visible in the chat.
- **TR-5.5** The app shall support **swipe gestures** (e.g. left or right) on a message to **archive** it; archived messages shall be hidden from the visible list (e.g. via a property on the message and binding or filtering).
- **TR-5.6** The chat input control shall be multi-line (editor semantics), preserving newline characters in submitted requests.
- **TR-5.7** Desktop keyboard handling shall support **Ctrl+Enter** as the submit accelerator while plain Enter inserts a newline.
- **TR-5.8** Mobile UX shall use a connection-first screen (host/port/session connect) and toggle to the chat workspace only after successful connection state.

*See:* [FR-1](functional-requirements.md#1-product-purpose), [FR-2](functional-requirements.md#2-chat-and-messaging), [FR-3](functional-requirements.md#3-message-priority-and-notifications), [FR-4](functional-requirements.md#4-archive).

---

## 6. Docker and containerization

- **TR-6.1** The **service** shall be containerizable via a **Dockerfile** (e.g. multi-stage build, runtime based on a Linux image).
- **TR-6.2** The container shall expose the gRPC port (e.g. `5243`) and support configuration via **environment variables** (e.g. `Agent__Command`, `Agent__LogDirectory`).
- **TR-6.3** The pipeline shall **build** the service Docker image and **publish** it to a container registry (e.g. **GitHub Container Registry**, `ghcr.io/<owner>/<repo>/service:latest`) on successful builds.

*See:* [FR-1](functional-requirements.md#1-product-purpose) (service deployment).

---

## 7. CI/CD and deployment

- **TR-7.1** **GitHub Actions** shall be used to **build the APK** of the app.
- **TR-7.2** A GitHub Actions workflow shall **build** the solution and the **Android APK** (e.g. on push to `main` or on manual trigger).
- **TR-7.3** **GitHub Actions** shall be used to **build the container** for the server. The container shall be **placed in the GitHub Container Registry** (GHCR) by the pipeline.
  - **TR-7.3.1** The workflow shall **build** the service **Docker image** and **publish** it to the container registry (e.g. GHCR).
  - **TR-7.3.2** The **APK** shall be **published as a static F-Droid repository** hosted by **GitHub Pages** via the **GitHub Actions** pipeline.
  - **TR-7.3.3** The workflow shall **update** an **F-Droid–style static repo** (e.g. index page, APK file, optional `index.xml` or similar for repo clients) and **deploy** it to **GitHub Pages** (e.g. using `actions/deploy-pages` and the `github-pages` environment).
  - **TR-7.3.4** **GitHub Pages** shall be configured to use **GitHub Actions** as the source for deployment.
  - **TR-7.3.5** **Documentation** shall be **generated via DocFX** and **published to GitHub Pages** so that the requirements and other docs are viewable on the project site.

*See:* [FR-6](functional-requirements.md#6-deployment-and-distribution-user-facing).

---

## 8. Testing

- **TR-8.1** The project shall include **thorough unit and integration tests**.
- **TR-8.2** Unit tests shall cover app logic (e.g. markdown formatting, chat message display, priority/archive behavior) and service options/configuration.
- **TR-8.3** Integration tests shall cover the gRPC service (e.g. in-memory test server): no command configured → session error; agent = `/bin/cat` → echo behavior; start/stop session → correct events and agent lifecycle).
- **TR-8.4** Tests shall be runnable through split build/test entrypoints for each SDK track (e.g. `./scripts/build-dotnet10.sh` and `./scripts/build-desktop-dotnet9.sh`).
- **TR-8.4.1** Service integration tests shall run through an explicit isolated entrypoint (e.g. `./scripts/test-integration.sh`) and be excluded from default CI pipeline jobs.
- **TR-8.5** Automated **mobile UI tests** shall validate core connection-screen interaction paths (e.g. host/port entry and connect action visibility) using device/emulator automation.
- **TR-8.6** Automated **desktop UI tests** shall validate management shell controls (menu/toolbar/session tabs and terminate actions) via headless UI automation.

*See:* (supports all FRs via verification).

---

## 9. UI and assets

- **TR-9.1** **Font Awesome** shall be used for **all iconography** (e.g. Font Awesome 6 Solid, `fa-solid-900` font).
- **TR-9.2** The app shall follow **Material Design** norms (e.g. M3 tokens: surfaces, outlines, typography, components such as cards and buttons).
- **TR-9.3** Theming shall support **light and dark mode** (e.g. `AppThemeBinding` or equivalent for colors and brushes).

*See:* [FR-5](functional-requirements.md#5-user-interface-and-presentation).

---

## 10. Extensibility (plugins) — FR-8.1

- **TR-10.1** **FR-8.1** (additional CLI agents via plugins) shall be implemented using a **strategy pattern**: the service shall use a common abstraction (e.g. an interface or strategy type) for agent behaviour, so that different agents (default process spawn, plugin-backed agents, etc.) can be selected and invoked uniformly.
- **TR-10.2** **Plugin discovery** shall be driven by **appsettings configuration**: the service shall read configuration (e.g. under `Agent` or a dedicated `Plugins` section) that **specifies assemblies to dynamically load** as plugins; those assemblies shall be loaded at runtime and contribute agent implementations (strategies) that the service can use.

*See:* [FR-8.1](functional-requirements.md#8-extensibility-plugins).

---

## 11. Local storage (LiteDB)

- **TR-11.1** **Both the app and the server** shall use **LiteDB** for **local storage of requests and results** (e.g. chat messages, agent requests, and agent responses persisted on device and on the server for history, replay, or offline use).
- **TR-11.2** **Uploaded images and videos** (media sent from the app to the server as agent context) shall be **stored on the server alongside the LiteDB file** (e.g. in the same data directory or a dedicated media subdirectory).
- **TR-11.3** **Images sent to the app** (e.g. from the agent or server) shall be **stored in a `Remote Agent` folder** of the **DCIM library** on the device (e.g. `DCIM/Remote Agent/` so they appear in the device gallery).

*See:* [FR-10](functional-requirements.md#10-media-as-agent-context), [FR-11](functional-requirements.md#11-multiple-sessions-and-agent-selection).

---

## 12. Multiple sessions and agent selection — FR-11.1

- **TR-12.1** **Protocol:** **SessionControl** (or equivalent) shall carry a **client-provided session_id** (string) and, for **START**, an optional **agent_id** (string) so the server can create or resume a session with that id and bind it to the chosen agent (FR-11.1, FR-11.1.1).
  - **TR-12.1.1** **Server:** The service shall **maintain a map of session_id → agent session** (e.g. in-memory or backed by storage) and **route** all messages on that stream to the agent bound to the **session_id** sent with START; each stream is treated as one logical session (FR-11.1.1).
  - **TR-12.1.2** **Server:** The service shall **expose the list of configured agents** (e.g. agent id and display name) to the client so the app can show an agent picker (e.g. gRPC method `ListAgents` or equivalent, or configuration delivered at connect) (FR-11.1.2).
  - **TR-12.1.3** **App:** The app shall maintain a **session list** (or session switcher) so the user can have **multiple sessions**; each session has a **session_id**, **title**, and **agent_id**; session list and metadata shall be **persisted** in local storage (e.g. LiteDB) (FR-11.1).
- **TR-12.2** **App:** When **starting a chat session**, the app shall **obtain the list of agents** (from server or config), **show an agent picker**, and send **SessionControl START** with the chosen **session_id** and **agent_id** (FR-11.1.2).
  - **TR-12.2.1** **App:** Each session shall have a **user-definable title**, **defaulting to the text of the first request**; the title shall be **stored** in local storage and **displayed** in the session list or header (FR-11.1.3).
  - **TR-12.2.2** **App:** **Tapping the session title** shall **swap to an inline editor** (e.g. focused Entry/Editor) with the **text selected** and the **keyboard opened**; **tapping off** (unfocus) shall **commit** the title and return to display mode (FR-11.1.3.1, FR-11.1.3.2).

*See:* [FR-11](functional-requirements.md#11-multiple-sessions-and-agent-selection).

---

## 13. Observability and structured logging

- **TR-13.1** The server shall emit **structured operational logs** to an append-only **JSON Lines** file in real time.
- **TR-13.2** Every structured log entry shall include **session_id** and **correlation_id** fields; when unavailable, the fields shall still be present with empty/null semantics.
- **TR-13.3** Structured logs shall be queryable through gRPC APIs for **snapshot replay** and **real-time streaming**.
- **TR-13.4** The desktop app shall ingest structured logs into local **LiteDB** collections dedicated to operational logs.
- **TR-13.5** The desktop log viewer shall support robust filtering by time range, level, event type, session id, correlation id, component, server id, and free-text search.
- **TR-13.6** Desktop-ingested structured log rows shall include server identity metadata (e.g. server id/display name and host/port origin).

*See:* [FR-12.2](functional-requirements.md#12-desktop-management-app), [FR-12.3](functional-requirements.md#12-desktop-management-app).

---

## 14. Desktop management capabilities

- **TR-14.1** The desktop app shall support the same core connection/session controls as mobile clients (connect, start/stop, send text/script/media, receive responses).
- **TR-14.1.0** Desktop implementation shall be hosted in `src/RemoteAgent.Desktop` and use Avalonia XAML views with DI-managed view models.
- **TR-14.1.1** The desktop app shall use one shared chat surface for both **direct** and **server** connection modes.
- **TR-14.1.2** The connect flow shall present a required mode selection (**Direct** or **Server**) before initiating a session connection.
- **TR-14.1.3** The desktop app session switcher shall be implemented as a **tabbed UI**.
- **TR-14.1.4** Desktop presentation shall include a top-level **menu bar** and **toolbar**, with key actions exposed through shared command bindings.
- **TR-14.1.5** Desktop view models shall be resolved and lifecycle-managed by the DI container; pages shall not directly instantiate view models.
- **TR-14.1.6** Session tabs shall include command-bound terminate actions (toolbar/menu/current-session terminate and per-tab terminate button).
- **TR-14.1.7** The desktop connection panel shall include a multi-line per-request context editor bound to view-model state and applied on each outbound message.
- **TR-14.1.8** The desktop shell shall provide server registration management (add/edit/remove) and server selection controls.
- **TR-14.1.9** Server-specific management/chat state shall be scoped per server selection via DI scoping, with main-shell current server tracking used to resolve server-specific view models.
- **TR-14.1.10** Multiple server-scoped workspaces shall remain active concurrently so connections/sessions on one server do not block another.
- **TR-14.2** The desktop app shall retrieve and update plugin assembly configuration via management APIs.
- **TR-14.3** The desktop app shall use server-provided runner/plugin IDs to select agents for sessions.
- **TR-14.4** Plugin configuration updates that require runtime reload shall surface explicit restart-required feedback.
- **TR-14.5** Session metadata persisted for desktop usage shall include connection mode (e.g. `direct` or `server`) in addition to session id/title/agent id.

## 15. Management APIs and policy controls

- **TR-15.1** The service shall expose management APIs for open sessions, abandoned sessions, connected devices, and connection history.
- **TR-15.2** The service shall expose control APIs for session cancel and device ban/unban operations.
- **TR-15.3** The service shall expose user/permission management APIs with server-side authorization enforcement.
- **TR-15.4** Management actions shall be logged as structured audit events.
- **TR-15.5** Connection management shall apply configurable **rate limits** for peer connection attempts and inbound client messages.
- **TR-15.6** Repeated rate-limit violations shall trigger **DoS detection** and temporary peer blocking with structured security events.
- **TR-15.7** The service shall expose a session-capacity endpoint for client preflight checks (e.g. `/api/sessions/capacity?agentId=<id>`) so UIs can warn when session limits are reached before attempting `START`.
- **TR-15.8** The service shall expose HTTP management endpoints for open/abandoned sessions and session termination (e.g. `/api/sessions/open`, `/api/sessions/abandoned`, `/api/sessions/{id}/terminate`).
- **TR-15.9** The service shall expose HTTP management endpoints for connected peers, connection history, and ban/unban lifecycle (e.g. `/api/connections/peers`, `/api/connections/history`, `/api/devices/{peer}/ban`).
- **TR-15.10** The service shall expose HTTP management endpoints for auth users and role metadata (e.g. `/api/auth/users`, `/api/auth/permissions`) with structured audit logging.

*See:* [FR-13](functional-requirements.md#13-sessiondeviceadmin-operations).

---

## 16. Shared agent interaction library

- **TR-16.1** Agent-facing synthetic inputs (request context envelope, seed context envelope, MCP update envelope) shall be defined in a shared library consumed by both server and desktop codepaths.
- **TR-16.2** The shared library shall expose a transport-agnostic session interface and dispatcher so the same interaction pipeline can target gRPC-managed sessions and direct local sessions.
- **TR-16.3** Server components shall delegate request-context injection, seed-context delivery, and MCP mapping-change notifications through the shared library, rather than duplicating ad-hoc message formatting.
- **TR-16.4** MCP mapping updates shall compute enable/disable deltas and notify active sessions bound to the affected agent using the shared interaction dispatcher.
- **TR-16.5** Desktop UI actions and server/session actions shall be encapsulated as reusable commands that target the shared interaction contracts where applicable.
- **TR-16.6** Mobile and desktop clients shall use a shared server-access library for gRPC API interactions (server info, management APIs, logs, and prompt templates).
- **TR-16.7** Mobile and desktop chat flows shall use a shared session-stream client implementation for connect/send/receive/stop behavior, with platform-specific UI wrappers only.

*See:* [FR-12.1](functional-requirements.md#12-desktop-management-app), [FR-12.4](functional-requirements.md#12-desktop-management-app), [FR-12.5](functional-requirements.md#12-desktop-management-app).

---

## 17. Prompt template system

- **TR-17.1** The service shall expose prompt-template APIs (`list`, `upsert`, `delete`) over gRPC.
- **TR-17.2** Prompt templates shall be stored persistently (LiteDB) and include template id, display name, description, template content, and timestamps.
- **TR-17.3** Client rendering shall use Handlebars-compatible evaluation and variable extraction.
- **TR-17.4** Client UX shall provide a prompt-variable input sequence before sending rendered template text.

*See:* [FR-14](functional-requirements.md#14-prompt-templates).

---

## 18. UI command/event CQRS testability

- **TR-18.1** All UI-triggered commands and UI events shall be implemented using a **CQRS-style split** between command/query request objects and handler components so UI layers do not directly embed business or transport logic.
- **TR-18.2** Every command/query/event handler used by mobile and desktop UIs shall be **unit-testable independently** of UI frameworks (MAUI/Avalonia), including deterministic success and failure-path assertions.
- **TR-18.3** UI command/event pipelines shall be designed for **mockable behavior injection** (for example via interfaces/factories/mediator abstractions) so tests can force known outcomes per endpoint and per failure mode.
- **TR-18.4** UI tests shall validate known use cases by substituting mocked CQRS handlers and asserting rendered UI state changes, status messages, and command completion/error behavior.

*See:* [FR-12](functional-requirements.md#12-desktop-management-app), [FR-2](functional-requirements.md#2-chat-and-messaging), [TR-8](#8-testing), [TR-14](#14-desktop-management-capabilities).
