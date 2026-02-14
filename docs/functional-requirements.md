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

*These requirements were recovered from the crashed Project Initializer session and reflect the intended product behavior.*
