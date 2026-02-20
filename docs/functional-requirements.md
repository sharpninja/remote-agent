# Functional Requirements

**Source:** Recovered from Project Initializer session (Cursor chat).
**Product:** Remote Agent — Android app and Linux service for communicating with a Cursor agent.

---

## 1. Product purpose

- **FR-1.1** The system shall provide an **Android app** that communicates with a **service hosted on a Linux system** (native or WSL).
- **FR-1.2** The service shall **spawn a Cursor agent** and **establish communication** with it.
- **FR-1.3** When the user sends a message from the app to the service, the service shall **forward that message to the Cursor agent**.
- **FR-1.4** The service shall send agent **output back to the app in real time**.
- **FR-1.5** The service shall **log all interaction** (session logs).
- **FR-1.6** The app shall use a **chat-based UI** for all interaction with the agent.

*See:* [TR-1](technical-requirements.md#1-solution-and-repository), [TR-2](technical-requirements.md#2-technology-stack), [TR-3](technical-requirements.md#3-service-architecture), [TR-5.1](technical-requirements.md#5-app-architecture).

---

## 2. Chat and messaging

- **FR-2.1** The user shall be able to **send messages** from the app to the agent via the service.
- **FR-2.2** Agent output (stdout/stderr) shall be **streamed to the app in real time** and displayed in the chat.
- **FR-2.3** Agent output in the chat shall be **formatted with a markdown parser** (e.g. bold, code, lists) so that Cursor output is readable and structured.
- **FR-2.4** The user shall be able to **connect** to the service (host/port) and **disconnect**; connecting shall start a session with the agent; disconnecting shall stop the session and the agent.
- **FR-2.5** Chat text entry shall support **multi-line input** on desktop; on mobile, Enter sends the message.
- **FR-2.6** On desktop clients, pressing **Ctrl+Enter** in the chat editor shall submit the request; on mobile clients, pressing **Enter** shall submit the request.
- **FR-2.7** On mobile clients, session establishment shall begin in a **dedicated connection view** and transition to the **chat view** after successful connection.

*See:* [TR-3](technical-requirements.md#3-service-architecture), [TR-4](technical-requirements.md#4-protocol-grpc), [TR-5](technical-requirements.md#5-app-architecture).

---

## 3. Message priority and notifications

- **FR-3.1** The server shall support **pushing chat messages with a priority**: **normal**, **high**, or **notify**.
- **FR-3.2** When a chat message with **notify** priority arrives, the app shall **register a system notification**.
- **FR-3.3** When the user **taps the notification**, the app shall open and the chat shall **make the message visible** to the user (e.g. open the chat and show the message in the list).
- **FR-3.4** Priorities **normal** and **high** may be used for future differentiation (e.g. styling or ordering); currently they are supported in the protocol and app model.

*See:* [TR-3.5](technical-requirements.md#3-service-architecture), [TR-4.3](technical-requirements.md#4-protocol-grpc), [TR-5.4](technical-requirements.md#5-app-architecture).

---

## 4. Archive

- **FR-4.1** The user shall be able to **swipe a message left or right** in the chat to **archive** it.
- **FR-4.2** Archived messages shall be **hidden from the main chat list** (they are not deleted; they are removed from view).

*See:* [TR-5.5](technical-requirements.md#5-app-architecture).

---

## 5. User interface and presentation

- **FR-5.1** The app shall use **Material Design UI norms** (layout, components, spacing, typography).
- **FR-5.2** The app shall use **Font Awesome** for all iconography.
- **FR-5.3** The app shall support **both light and dark mode** styling so that it is usable in either theme.

*See:* [TR-9](technical-requirements.md#9-ui-and-assets).

---

## 6. Deployment and distribution (user-facing)

- **FR-6.1** Built **APK packages** shall be **deployable to GitHub Pages** via an **F-Droid–style static site** (index page with app info and direct APK download; optional repo metadata for F-Droid-style clients).
- **FR-6.2** The pipeline shall **update** this F-Droid–style repo and **deploy** it to GitHub Pages on successful builds so that users can download the latest APK from the project site.

*See:* [TR-7](technical-requirements.md#7-cicd-and-deployment).

---

## 7. Session and lifecycle

- **FR-7.1** Each connection from the app to the service shall be treated as a **session**; the service shall start the agent when the session starts and stop it when the session ends (e.g. disconnect or stream closed).
- **FR-7.2** Session lifecycle events (e.g. session started, session stopped, session error) shall be **visible in the chat** so the user understands connection and agent state.

*See:* [TR-3](technical-requirements.md#3-service-architecture), [TR-4](technical-requirements.md#4-protocol-grpc).

---

## 8. Extensibility (plugins)

- **FR-8.1** **Additional CLI agents** shall be **supported via plugins** to the server: the service shall allow plugging in agents (e.g. other command-line tools or backends) so that users can use agents beyond the default Cursor agent without changing the core server code.

*See:* [TR-10](technical-requirements.md#10-extensibility-plugins--fr-81).

---

## 9. Run scripts from chat

- **FR-9.1** The user shall be able to **specify running a specific script** from the chat: either a **bash** script or a **PowerShell (pwsh)** script (e.g. by path, name, or a supported command syntax in a chat message).
- **FR-9.2** When such a script is run, the user shall receive **both stdout and stderr** from the script **upon completion** (e.g. as one or more chat messages showing the combined or separated output and error streams).

*See:* [TR-4](technical-requirements.md#4-protocol-grpc) (ClientMessage/ServerMessage for script request and output).

---

## 10. Media as agent context

- **FR-10.1** The user shall be able to **send images or video** from the app to the server as **context for the agent** (e.g. attach to a message or send as a separate context payload so the agent can use the media when responding).

*See:* [TR-11](technical-requirements.md#11-local-storage-litedb).

---

## 11. Multiple sessions and agent selection

- **FR-11.1** The **client app** shall support **multiple sessions**, each with their own **session-id** (e.g. the user can have several concurrent or saved chat sessions, each identified by a unique session-id).
  - **FR-11.1.1** The **server** shall use the **session-id** to **manage interactions with specific agents** (e.g. each session is bound to one agent; the server routes messages and agent lifecycle by session-id).
  - **FR-11.1.2** When **starting a chat session**, the app shall **ask which agent to use** from a **list of configured agents** (e.g. a picker or selection step before or at session start so the user chooses which agent to talk to).
  - **FR-11.1.3** Sessions shall have a **user-definable title**, **defaulting to the text of the first request** (e.g. the session label in the UI can be edited by the user; if not set, it is the first message sent in that session).
    - **FR-11.1.3.1** **Tapping on the session title** shall **swap to an editor control** with the **text highlighted** and the **keyboard opened** (e.g. inline edit: tap title → replace with focused text field, full text selected, soft keyboard shown).
    - **FR-11.1.3.2** **Tapping off the editor** (e.g. tapping outside the title field or dismissing focus) shall **commit the updated session title** (e.g. unfocus → save the current text as the session title and return to display mode).

*See:* [TR-11](technical-requirements.md#11-local-storage-litedb), [TR-12](technical-requirements.md#12-multiple-sessions-and-agent-selection--fr-111).

---

## 12. Desktop management app

- **FR-12.1** The system shall provide a **desktop management app** (Avalonia UI) that can connect to **remote servers** and replicate core mobile interaction flows (connect, start/stop session, send messages, receive output).
- **FR-12.1.1** The desktop app shall use the **same chat UI** for interaction whether the session is configured for **direct** agent access or **server** access.
- **FR-12.1.2** When establishing a connection, the desktop app shall display a **connection settings dialog** that prompts for connection defaults (host, port, mode, agent, optional API key, and per-request context), including selection of **Direct** or **Server** mode.
- **FR-12.1.3** The desktop app shall provide a **tabbed session interface** so users can switch sessions by selecting session tabs.
- **FR-12.1.4** The desktop app shall follow standard desktop interaction patterns including a **menu bar**, **toolbar**, and command-driven actions.
- **FR-12.1.5** Desktop session tabs shall provide a direct **terminate session** action for active/saved sessions.
- **FR-12.2** The desktop app shall include a **structured log viewer** with **real-time monitoring** and robust filtering.
- **FR-12.3** The desktop app shall ingest structured logs into local **LiteDB** storage for offline/history analysis.
- **FR-12.4** The desktop app shall manage server plugins for agents (view configured plugins, update plugin assembly configuration).
- **FR-12.5** The desktop app shall support selecting and reusing server-side agent plugins for direct agent interaction via normal session flows.
- **FR-12.6** Agent interaction behavior (request context, seed context, MCP update notifications) shall be implemented through a shared library so server and desktop follow identical interaction semantics.
- **FR-12.7** The desktop app shall provide operator panels for connected peers/devices, peer ban list management, connection history, abandoned sessions, and auth-user/role management.
- **FR-12.8** The desktop chat surface shall expose editable **per-request context** text that is attached to every outbound request for the active session.
- **FR-12.9** The desktop management app shall support **multiple registered servers**, including add/update/remove server registration UI.
- **FR-12.10** The desktop app shall support **concurrent active connections across different registered servers**.
- **FR-12.11** Structured log records in the desktop app shall include a **server identifier**, and log filtering shall default to the **currently selected server**.
- **FR-12.12** The Management UI shall include a **Management App Log view** that captures `ILogger` output from the desktop application itself (not the remote server) and displays it in a scrollable, filterable list within the management interface.
  - **FR-12.12.1** A custom `ILogger` implementation shall capture log entries (timestamp, level, category, message, exception) produced by the desktop app process and make them available to the log view in real time.
  - **FR-12.12.2** The log view shall display at minimum: timestamp, log level, logger category, and message for each entry.
  - **FR-12.12.3** The log view shall provide a **Clear** command that discards all currently captured log entries.
  - **FR-12.12.4** The log view shall provide a **Save** command that exports the captured log entries to a user-selected file in one of three formats: **plain text** (`.txt`), **JSON** (`.json`), or **CSV** (`.csv`).
  - **FR-12.12.5** The export format shall be selectable at save time (e.g. via file-type filter in the save dialog or an explicit format selector).

*See:* [TR-13](technical-requirements.md#13-observability-and-structured-logging), [TR-14](technical-requirements.md#14-desktop-management-capabilities).

---

## 13. Session/device/admin operations

- **FR-13.1** The management experience shall support querying **open sessions** and **abandoned sessions**.
- **FR-13.2** The management experience shall support querying **connected mobile devices** and **connection history**.
- **FR-13.3** Authorized operators shall be able to **cancel active sessions**.
- **FR-13.4** Authorized operators shall be able to **ban/unban specific mobile devices**.
- **FR-13.5** The management experience shall support **auth user and permission management**.
- **FR-13.6** When MCP server mappings for an agent change, active sessions for that agent shall be notified of **enabled/disabled** MCP servers.
- **FR-13.7** The server shall enforce a configurable **maximum concurrent sessions** limit across all agents.
- **FR-13.8** Each agent may define its own configurable **maximum concurrent sessions**; agent-level limits shall not allow exceeding the server-wide session limit.

*See:* [TR-15](technical-requirements.md#15-management-apis-and-policy-controls).

---

## 14. Prompt templates

- **FR-14.1** Chat clients shall be able to access reusable **templatized prompts** from the server.
- **FR-14.2** Prompt templates shall support **Handlebars** placeholders (e.g. `{{incident_id}}`, `{{service_name}}`).
- **FR-14.3** Submitting a template shall display a compact UI flow that asks the user for each required template variable.
- **FR-14.4** After variable input, the client shall render the final prompt and submit it through the standard chat send flow.

## 15. Connection protection

- **FR-15.1** Connection management shall include configurable **rate limiting** for inbound client traffic.
- **FR-15.2** The server shall detect likely **DoS patterns** (e.g. burst connection attempts or message floods) and temporarily throttle/block offending peers.

## 16. Test execution policy

- **FR-16.1** Integration tests shall be executable on demand through an explicit isolated workflow or script and shall not run as part of default build-and-release pipeline executions.

---

## 17. Device pairing and API key management

- **FR-17.1** The system shall support a **device pairing flow** that securely provisions a mobile device with connection details and an API key for the server.
- **FR-17.2** An administrator shall be able to **set pairing credentials** (username and password) from the desktop management app; the service shall hash and store them in configuration.
- **FR-17.3** When pairing credentials are saved, the service shall **generate a cryptographically random API key**, store it in server configuration, and **return it** to the desktop app.
- **FR-17.4** The service shall expose a **web-based login page** at `/pair` on the HTTP/1 web port (computed as `"1" + gRPC port`) so that a mobile device can authenticate with the pairing credentials.
- **FR-17.5** After successful login, the service shall display the API key, a **server-generated QR code** (no external CDN dependency), and a **deep-link button** (`remoteagent://pair?key=…&host=…&port=…`) that carries connection details.
- **FR-17.6** The mobile app shall provide a **Login button** that opens the server's `/pair` page in an in-app WebView; after successful authentication the app shall **automatically extract the deep-link URI** from the rendered page and populate host, port, and API key.
  - **FR-17.6.1** The Login button shall be **disabled** until a server host/IP is entered.
  - **FR-17.6.2** The Login button shall be **disabled** when an API key is already stored (pairing already completed).
- **FR-17.7** The mobile app's Connect button shall be **disabled** until a valid API key is stored (i.e., pairing must precede connection).
- **FR-17.8** The API key shall be **persisted** on the mobile device alongside the server host and port after a successful connection.

*See:* [TR-19](technical-requirements.md#19-device-pairing-and-api-key-management).

---

## 18. Desktop UX refinements

- **FR-18.1** All text display controls in Avalonia desktop views shall use **SelectableTextBlock** (instead of `TextBlock`) so users can select and copy displayed text.
- **FR-18.2** All interactive and display controls in the Avalonia desktop app shall have a uniform **4px margin** applied via a global style.

*See:* [TR-20](technical-requirements.md#20-desktop-ux-refinements).

---

## 19. Server profiles and persistent connection settings

- **FR-19.1** Both mobile and desktop apps shall **persist server connection details** (host, port, API key, display name) after a successful connection, keyed by **host:port**.
- **FR-19.2** The Settings page (mobile) and Server Setup panel (desktop) shall display a **list of saved server profiles** with the ability to **add, edit, and remove** entries.
- **FR-19.3** Each saved server profile shall store a configurable **per-request context** (text prepended to every chat message) and a **default session context** (context seeded into new sessions).
- **FR-19.4** Connecting to a previously saved server shall **auto-load** the stored per-request context into the workspace (without overriding user-entered values).
- **FR-19.5** Saving or updating a server profile shall not affect **existing active sessions**; changes apply only to subsequent connections.

*See:* [TR-21](technical-requirements.md#21-server-profiles-and-persistent-connection-settings).

---

## 20. Mobile chat UX

- **FR-20.1** On mobile clients, pressing **Enter** in the chat input shall **send the message** (submit the request).
- **FR-20.2** On mobile clients, the **connection mode shall be fixed to "server"**; no mode-selection dialog shall be shown.
- **FR-20.3** Navigation items (Settings, Account, MCP Registry) shall be **hidden until a connection is established**; the connection card shall be **hidden once connected**.
- **FR-20.4** All client-to-server API calls shall use **gRPC exclusively**; no REST/HTTP fallback endpoints shall be used by the mobile client.

*See:* [TR-22](technical-requirements.md#22-mobile-chat-ux).
