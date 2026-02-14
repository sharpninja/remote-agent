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

---

## 2. Chat and messaging

- **FR-2.1** The user shall be able to **send messages** from the app to the agent via the service.
- **FR-2.2** Agent output (stdout/stderr) shall be **streamed to the app in real time** and displayed in the chat.
- **FR-2.3** Agent output in the chat shall be **formatted with a markdown parser** (e.g. bold, code, lists) so that Cursor output is readable and structured.
- **FR-2.4** The user shall be able to **connect** to the service (host/port) and **disconnect**; connecting shall start a session with the agent; disconnecting shall stop the session and the agent.

---

## 3. Message priority and notifications

- **FR-3.1** The server shall support **pushing chat messages with a priority**: **normal**, **high**, or **notify**.
- **FR-3.2** When a chat message with **notify** priority arrives, the app shall **register a system notification**.
- **FR-3.3** When the user **taps the notification**, the app shall open and the chat shall **make the message visible** to the user (e.g. open the chat and show the message in the list).
- **FR-3.4** Priorities **normal** and **high** may be used for future differentiation (e.g. styling or ordering); currently they are supported in the protocol and app model.

---

## 4. Archive

- **FR-4.1** The user shall be able to **swipe a message left or right** in the chat to **archive** it.
- **FR-4.2** Archived messages shall be **hidden from the main chat list** (they are not deleted; they are removed from view).

---

## 5. User interface and presentation

- **FR-5.1** The app shall use **Material Design UI norms** (layout, components, spacing, typography).
- **FR-5.2** The app shall use **Font Awesome** for all iconography.
- **FR-5.3** The app shall support **both light and dark mode** styling so that it is usable in either theme.

---

## 6. Deployment and distribution (user-facing)

- **FR-6.1** Built **APK packages** shall be **deployable to GitHub Pages** via an **F-Droid–style static site** (index page with app info and direct APK download; optional repo metadata for F-Droid-style clients).
- **FR-6.2** The pipeline shall **update** this F-Droid–style repo and **deploy** it to GitHub Pages on successful builds so that users can download the latest APK from the project site.

---

## 7. Session and lifecycle

- **FR-7.1** Each connection from the app to the service shall be treated as a **session**; the service shall start the agent when the session starts and stop it when the session ends (e.g. disconnect or stream closed).
- **FR-7.2** Session lifecycle events (e.g. session started, session stopped, session error) shall be **visible in the chat** so the user understands connection and agent state.

---

## 8. Extensibility (plugins)

- **FR-8.1** **Additional CLI agents** shall be **supported via plugins** to the server: the service shall allow plugging in agents (e.g. other command-line tools or backends) so that users can use agents beyond the default Cursor agent without changing the core server code.

---

## 9. Run scripts from chat

- **FR-9.1** The user shall be able to **specify running a specific script** from the chat: either a **bash** script or a **PowerShell (pwsh)** script (e.g. by path, name, or a supported command syntax in a chat message).
- **FR-9.2** When such a script is run, the user shall receive **both stdout and stderr** from the script **upon completion** (e.g. as one or more chat messages showing the combined or separated output and error streams).

---

*These requirements were recovered from the crashed Project Initializer session and reflect the intended product behavior.*
