# Remote Agent

Android app (MAUI) that talks to a Linux service over gRPC. The service spawns a Cursor agent process, forwards messages from the app to the agent, and streams agent output back to the app in real time. All interaction is logged.

## Architecture

- **RemoteAgent.App** – MAUI Android app with a chat UI. Connects to the service via gRPC (bidirectional streaming), sends user messages, and shows agent output and session events.
- **RemoteAgent.Service** – .NET gRPC server (Linux/WSL). Listens for client connections, spawns a configurable agent process (e.g. Cursor agent), forwards app messages to the agent’s stdin, and streams stdout/stderr to the app. Writes a session log file per connection.
- **RemoteAgent.Proto** – Shared gRPC contracts (`AgentGateway.proto`): `Connect(stream ClientMessage) returns (stream ServerMessage)`.

## Run the service (Linux / WSL)

1. Configure the agent command in `src/RemoteAgent.Service/appsettings.json` or `appsettings.Development.json`:

   ```json
   "Agent": {
     "Command": "/path/to/your/agent-or-script",
     "Arguments": "",
     "LogDirectory": ""
   }
   ```

   For a quick test, use `/bin/cat` (echoes each line back). Leave `Command` empty if you only want to test connection; the app will get an error if it sends START without a command.

2. From the repo root:

   ```bash
   dotnet run --project src/RemoteAgent.Service
   ```

   The service listens on `http://0.0.0.0:5243` (gRPC over HTTP/2).

## Run the service with Docker

Build and run the container (e.g. after pushing to GitHub, use the image from GHCR):

```bash
docker run -p 5243:5243 \
  -e Agent__Command=/path/to/agent \
  -e Agent__LogDirectory=/app/logs \
  -v /path/on/host/logs:/app/logs \
  ghcr.io/OWNER/remote-agent/service:latest
```

Or build locally from the repo root:

```bash
docker build -t remote-agent-service .
docker run -p 5243:5243 -e Agent__Command=/bin/cat remote-agent-service
```

Override `Agent:Command` and `Agent:LogDirectory` via environment variables (e.g. `Agent__Command`, `Agent__LogDirectory`).

## Run the Android app

1. Build and run on an emulator or device:

   ```bash
   dotnet build src/RemoteAgent.App -f net10.0-android
   dotnet build src/RemoteAgent.App -f net10.0-android -t:Run
   ```

2. In the app, set **Host** and **Port**:
   - Emulator: host `10.0.2.2`, port `5243` (to reach the host machine).
   - Physical device on same LAN: use the Linux machine’s IP and port `5243`.

3. Tap **Connect**. The app sends a START control message; the service starts the agent process and streams output. Type in the box and tap **Send** to send a line to the agent. Tap **Disconnect** to stop the session and the agent.

## Protocol (gRPC)

- **ClientMessage**  
  - `text`: user message (forwarded to agent stdin).  
  - `control`: `START` (spawn agent) or `STOP` (kill agent).

- **ServerMessage**  
  - `output`: agent stdout line.  
  - `error`: agent stderr line.  
  - `event`: `SESSION_STARTED`, `SESSION_STOPPED`, `SESSION_ERROR`.  
  - `priority`: optional `NORMAL`, `HIGH`, or `NOTIFY`. When the app receives `NOTIFY`, it shows a system notification; tapping it opens the chat.

**App behaviour**

- **Priority**  
  Messages from the server can have priority `normal`, `high`, or `notify`. `notify` messages trigger a system notification on the device; tapping the notification opens the app (and the message is visible in the chat).

- **Swipe to archive**  
  In the chat, swipe a message left or right to archive it. Archived messages are hidden from the list.

Session logs are written under `Agent:LogDirectory` (default: temp) as `remote-agent-{sessionId}.log`.

## CI/CD (GitHub Actions)

On push to `main` (or manual run), the workflow:

1. **Builds** the solution and the Android APK.
2. **Builds** the Docker image for the service and **publishes** it to GitHub Container Registry: `ghcr.io/<owner>/<repo>/service:latest` (and `@sha256:...`).
3. **Updates** an F-Droid-style static repo and **deploys** it to **GitHub Pages**: index page with app info and a direct APK download; optional `index.xml` for repo clients.

**Setup**

- **GitHub Pages**: In the repo **Settings → Pages**, set “Build and deployment” to **GitHub Actions**. The workflow uses the `github-pages` environment and `actions/deploy-pages`.
- **Container registry**: No extra setup; the workflow uses `GITHUB_TOKEN` to push to `ghcr.io`.

**Artifacts**

- APK: built in the `android` job and published via the Pages site (e.g. `https://<owner>.github.io/<repo>/remote-agent.apk`) as a static F-Droid-style repo.
- Docker image: built and pushed to **GitHub Container Registry** (e.g. `ghcr.io/<owner>/<repo>/service:latest`).
- Documentation: generated with **DocFX** from `docs/` and published at `https://<owner>.github.io/<repo>/docs/` (functional and technical requirements).

## Tests

Unit and integration tests use xUnit and FluentAssertions.

- **RemoteAgent.App.Tests** – Unit tests for `RemoteAgent.App.Logic`: `MarkdownFormat` (ToHtml, PlainToHtml, escaping, markdown features) and `ChatMessage` (DisplayText, RenderedHtml for user/event/agent/error).
- **RemoteAgent.Service.Tests** – Unit tests for `AgentOptions`; integration tests for the gRPC service (in-memory test server via `WebApplicationFactory`):
  - No command configured → client receives `SessionError` event.
  - Agent = `/bin/cat` → client sends text and receives echoed output (Unix only).
  - Agent = `sleep` / `cmd` → client sends START then STOP and receives `SessionStarted` and `SessionStopped`.

Run all tests:

```bash
dotnet test RemoteAgent.slnx
```
