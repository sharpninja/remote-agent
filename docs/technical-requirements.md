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

---

## 2. Technology stack

- **TR-2.1** The **client** shall be built with **.NET MAUI** targeting **Android** (e.g. `net10.0-android`).
- **TR-2.2** The **service** shall be a **.NET** application (e.g. ASP.NET Core) targeting **Linux** (and runnable on **WSL** or native Linux).
- **TR-2.3** Communication between the app and the service shall use **gRPC** with **persistent bidirectional streaming** for requests and responses (e.g. a single `Connect(stream ClientMessage) returns (stream ServerMessage)` RPC).
- **TR-2.4** The **.NET 10 SDK** shall be used; the **Android workload** (and **Maui–Android**) shall be installed as needed for building the app; the **Android SDK** shall be available for the build environment.

---

## 3. Service architecture

- **TR-3.1** The service shall **listen** for client connections (e.g. on a configurable host/port, such as `http://0.0.0.0:5243` for gRPC over HTTP/2).
- **TR-3.2** The service shall **spawn a configurable agent process** (e.g. Cursor agent or a test process like `/bin/cat`) when a session starts.
- **TR-3.3** The service shall **forward** app messages (e.g. `ClientMessage.text`) to the agent’s **stdin** (line-oriented).
- **TR-3.4** The service shall **stream** the agent’s **stdout** and **stderr** back to the app (e.g. as `ServerMessage.output` and `ServerMessage.error`).
- **TR-3.5** The service shall support an optional **message priority** on each `ServerMessage` (e.g. `NORMAL`, `HIGH`, `NOTIFY`) so that the app can implement notifications and prioritization.
- **TR-3.6** The service shall **write a session log file** per connection (e.g. under a configurable `LogDirectory`, with a name such as `remote-agent-{sessionId}.log`), logging session lifecycle and message flow.

---

## 4. Protocol (gRPC)

- **TR-4.1** The **contract** shall be defined in a shared **Protocol Buffers** file (e.g. `AgentGateway.proto`) and compiled for C# (e.g. in a `RemoteAgent.Proto` project).
- **TR-4.2** **ClientMessage** shall support: (a) **text** — user message to forward to the agent; (b) **control** — start or stop the session.
- **TR-4.3** **ServerMessage** shall support: (a) **output** — agent stdout line; (b) **error** — agent stderr line; (c) **event** — session lifecycle (e.g. session started, stopped, error); (d) **priority** — optional level (e.g. normal, high, notify) for the message.
- **TR-4.4** The RPC shall be **duplex streaming** so that the client can send messages and receive server messages over the same connection without opening multiple calls per message.

---

## 5. App architecture

- **TR-5.1** The app shall use a **chat UI** (e.g. a list of messages and an input field) bound to an **observable collection** of chat messages.
- **TR-5.2** The app shall **connect** to the service using the configured host and port (e.g. emulator: `10.0.2.2:5243`; device: host machine IP and port).
- **TR-5.3** Agent output (and errors) shall be **rendered in the chat** using a **markdown parser** (e.g. Markdig or equivalent) to produce HTML or formatted text for display (e.g. in a WebView or rich control).
- **TR-5.4** When the app receives a message with **notify** priority, it shall **show a system notification** (e.g. on Android, using a notification channel and `NotificationManager`/`NotificationCompat`); tapping the notification shall open the app so the message is visible in the chat.
- **TR-5.5** The app shall support **swipe gestures** (e.g. left or right) on a message to **archive** it; archived messages shall be hidden from the visible list (e.g. via a property on the message and binding or filtering).

---

## 6. Docker and containerization

- **TR-6.1** The **service** shall be containerizable via a **Dockerfile** (e.g. multi-stage build, runtime based on a Linux image).
- **TR-6.2** The container shall expose the gRPC port (e.g. `5243`) and support configuration via **environment variables** (e.g. `Agent__Command`, `Agent__LogDirectory`).
- **TR-6.3** The pipeline shall **build** the service Docker image and **publish** it to a container registry (e.g. **GitHub Container Registry**, `ghcr.io/<owner>/<repo>/service:latest`) on successful builds.

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

---

## 8. Testing

- **TR-8.1** The project shall include **thorough unit and integration tests**.
- **TR-8.2** Unit tests shall cover app logic (e.g. markdown formatting, chat message display, priority/archive behavior) and service options/configuration.
- **TR-8.3** Integration tests shall cover the gRPC service (e.g. in-memory test server): no command configured → session error; agent = `/bin/cat` → echo behavior; start/stop session → correct events and agent lifecycle).
- **TR-8.4** Tests shall be runnable via the solution (e.g. `dotnet test RemoteAgent.slnx`).

---

## 9. UI and assets

- **TR-9.1** **Font Awesome** shall be used for **all iconography** (e.g. Font Awesome 6 Solid, `fa-solid-900` font).
- **TR-9.2** The app shall follow **Material Design** norms (e.g. M3 tokens: surfaces, outlines, typography, components such as cards and buttons).
- **TR-9.3** Theming shall support **light and dark mode** (e.g. `AppThemeBinding` or equivalent for colors and brushes).

---

*These requirements were recovered from the crashed Project Initializer session and reflect the intended technical design and constraints.*
