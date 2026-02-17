### Automated UI Test Plan for Remote Agent Mobile (Android MAUI) and Management (Avalonia Desktop) Apps

#### 1. Introduction
**Purpose**: This automated UI test plan verifies the user interface and interaction flows for the Remote Agent mobile (Android MAUI) and management (Avalonia desktop) apps against the functional requirements. It focuses on end-to-end user scenarios, ensuring usability, responsiveness, and correctness without testing backend logic (e.g., gRPC internals, which are covered in integration tests per TR-8.3). Automation ensures repeatability, regression detection, and CI/CD integration.

**Scope**:
- In-scope: UI elements, interactions (taps, swipes, inputs, navigation), visual feedback (e.g., markdown rendering, notifications), session management, and platform-specific behaviors.
- Out-of-scope: Performance metrics, security (e.g., DoS per FR-15), unit tests (TR-8.2), manual exploratory testing.
- Assumptions: Tests run on emulated/simulated environments; real devices for final validation.

#### 2. Tools and Frameworks
- **Mobile (MAUI Android)**: Appium (v5+) with NUnit/xUnit for test orchestration. Use UIAutomator2 driver for Android. Assign AutomationId to all interactive elements (e.g., buttons, entries) per best practices. Mock platform services (e.g., INotificationManager) for isolation.
- **Desktop (Avalonia)**: Avalonia.Headless with xUnit/NUnit for headless execution (fast, no visible window). For visual regression, use screenshot rendering (e.g., RenderTargetBitmap to PNG). Mock DI dependencies (e.g., view-models via NSubstitute).
- **Shared**: GitHub Actions for CI (integrate with build workflows per TR-7). Allure or ExtentReports for reporting. In-memory LiteDB for persistence mocks.
- **Best Practices Applied**: Focused tests (one scenario per test), AAA pattern, theory tests for variations (e.g., light/dark modes), main-thread handling in MAUI, command bindings in Avalonia.

#### 3. Test Environment
- **Mobile**: Android Emulator (API 34+), .NET 10 SDK with maui-android workload. Devices: Pixel 6 emulation (light/dark modes). OS: Android 14+.
- **Desktop**: Windows/Linux/macOS hosts (headless mode). .NET 9 SDK for Avalonia. Simulate multi-server via mocked registrations.
- **Setup**: Auto-start Appium server in tests (per Microsoft sample). Clean app data/session before each test. Use environment variables for configs (e.g., host/port).
- **Teardown**: Close sessions, clear notifications, dispose mocks.

#### 4. Test Cases
Test cases are grouped by FR sections. Each includes:
- **ID**: Unique identifier (M for Mobile, D for Desktop).
- **Description**: What is tested.
- **Preconditions**: Setup.
- **Automated Steps**: Pseudo-code for script (Appium/NUnit for mobile; Headless/xUnit for desktop).
- **Expected Result**: Verification.
- **Linked FR/TR**: Traceability.

##### 4.1 Product Purpose and Chat/Messaging (FR-1, FR-2, FR-7)
- **M-1.1**: Verify chat UI displays and sends messages, streams agent output in real-time with markdown formatting.
  - Preconditions: App launched, connected to mock service.
  - Automated Steps:
    ```
    driver.FindElementByAutomationId("HostEntry").SendKeys("localhost");
    driver.FindElementByAutomationId("ConnectButton").Tap();
    WaitForElement("ChatView");
    driver.FindElementByAutomationId("MessageEntry").SendKeys("Test message\nMulti-line");
    driver.FindElementByAutomationId("SendButton").Tap();  // Or simulate Ctrl+Enter on desktop emulator
    WaitForStreamingOutputContaining("Formatted **bold** text");
    ```
  - Expected: Message sent, output rendered as formatted text in list, session events visible (e.g., "Session started").
  - Linked: FR-1.3-1.6, FR-2.1-2.3, FR-2.5-2.7, FR-7.2; TR-5.1-5.3, TR-5.6-5.8.

- **M-1.2**: Verify connect/disconnect starts/stops session and agent.
  - Preconditions: Mock service with agent echo.
  - Automated Steps: Connect as above; send message; disconnect via button; attempt send (should fail).
  - Expected: Session events in chat; no output after disconnect.
  - Linked: FR-1.2, FR-2.4, FR-7.1.

- **D-1.1**: Verify desktop chat UI mirrors mobile for direct/server modes, with menu/toolbar actions.
  - Preconditions: Headless app initialized with mock DI.
  - Automated Steps:
    ```
    var app = HeadlessAppBuilder.Build();
    app.MainWindow.FindControl<MenuItem>("ConnectMenu").Click();
    SelectMode("Server");
    app.MainWindow.FindControl<TextBox>("MessageEntry").Text = "Test";
    app.MainWindow.FindControl<Button>("SendButton").Command.Execute(null);
    Assert.ObservableCollectionUpdated(app.ViewModel.ChatMessages, m => m.Contains("Formatted output"));
    ```
  - Expected: Messages sent/received; toolbar/menu functional.
  - Linked: FR-12.1-12.1.4, FR-12.1.6; TR-14.1.1-14.1.4.

- Edge: M-1.3 (invalid host → error in chat); D-1.2 (concurrent sessions via tabs).

##### 4.2 Message Priority and Notifications (FR-3)
- **M-2.1**: Verify notify-priority message triggers system notification; tap opens app to message.
  - Preconditions: Notification permissions granted.
  - Automated Steps: Simulate server sending notify message; wait for notification; tap notification via Appium extension; assert chat scrolls to message.
  - Expected: Notification shown with channel; app foregrounded, message visible.
  - Linked: FR-3.1-3.4; TR-5.4.

- Edge: M-2.2 (normal/high priorities → no notification, but styled in chat).

- Desktop: No direct equivalent (notifications not specified); test high-priority styling if implemented.

##### 4.3 Archive (FR-4)
- **M-3.1**: Verify swipe left/right archives message, hides from list.
  - Preconditions: Chat with messages.
  - Automated Steps: SwipeElement("MessageItem1", Direction.Left); assert item not visible in list.
  - Expected: Message archived (property set), list filtered.
  - Linked: FR-4.1-4.2; TR-5.5.

##### 4.4 User Interface and Presentation (FR-5)
- **M-4.1**: Verify Material Design, Font Awesome icons, light/dark mode switching.
  - Preconditions: System theme toggled.
  - Automated Steps: Assert element styles (e.g., button elevation, icon class "fa-solid"); switch theme; re-assert colors/brushes.
  - Expected: Consistent typography/spacing; icons render; theme adapts.
  - Linked: FR-5.1-5.3; TR-9.1-9.3.

- **D-4.1**: Similar, but test Avalonia theming and menu/toolbar icons.

##### 4.5 Session and Lifecycle (FR-7) – Covered in 4.1.

##### 4.6 Extensibility (Plugins) (FR-8)
- Desktop-focused: **D-6.1**: Verify plugin management UI (view/update configs).
  - Preconditions: Mock plugins loaded.
  - Automated Steps: Navigate to plugin panel; edit assembly config; assert restart prompt if needed.
  - Expected: Plugins listed; updates persisted; feedback shown.
  - Linked: FR-8.1, FR-12.4; TR-10.1-10.2, TR-14.2-14.4.

##### 4.7 Run Scripts from Chat (FR-9)
- **M-7.1**: Verify sending script path/command runs and displays stdout/stderr.
  - Preconditions: Connected session.
  - Automated Steps: Enter "/run bash script.sh"; send; wait for output message.
  - Expected: Combined/separated output in chat.
  - Linked: FR-9.1-9.2; TR-4 (script payloads).

- Desktop similar.

##### 4.8 Media as Agent Context (FR-10)
- **M-8.1**: Verify sending image/video as context; stored in DCIM.
  - Preconditions: Gallery access.
  - Automated Steps: Tap attach; select media; send; assert path in DCIM/Remote Agent.
  - Expected: Media uploaded, visible in chat; persisted.
  - Linked: FR-10.1; TR-11.2-11.3.

##### 4.9 Multiple Sessions and Agent Selection (FR-11)
- **M-9.1**: Verify multiple sessions, agent picker, title default/edit.
  - Preconditions: Mock agents list.
  - Automated Steps: Start session → picker shown; select agent; send first message → title defaults; tap title → editor focused, keyboard open; edit and unfocus → title saved.
  - Expected: Sessions listed/switched; id bound; title editable inline.
  - Linked: FR-11.1-11.1.3.2; TR-12.1-12.2.2.

- **D-9.1**: Tabbed interface for sessions; terminate action.
  - Automated Steps: Open new tab; select agent; terminate via button/menu.
  - Expected: Tabs switch; session closed.
  - Linked: FR-12.1.3, FR-12.1.5-12.1.6; TR-14.1.3-14.1.6.

##### 4.10 Desktop Management App (FR-12)
- **D-10.1**: Verify structured log viewer with real-time monitoring and filtering.
  - Preconditions: Mock logs ingested to LiteDB.
  - Automated Steps: Open viewer; filter by session_id/time; assert results; simulate new log → auto-update.
  - Expected: Filters apply (time, level, id); real-time refresh.
  - Linked: FR-12.2-12.3; TR-13.1-13.6.

- **D-10.2**: Verify per-request context editor attached to outbound.
  - Automated Steps: Edit context text; send message; assert attached in mock dispatch.
  - Expected: Context sent with request.
  - Linked: FR-12.8; TR-14.1.7.

- **D-10.3**: Verify multi-server registration and concurrent connections.
  - Automated Steps: Add server; switch; assert scoped state (e.g., logs filtered by server id).
  - Expected: Servers listed; concurrent active.
  - Linked: FR-12.9-12.11; TR-14.1.8-14.1.10.

##### 4.11 Session/Device/Admin Operations (FR-13)
- **D-11.1**: Verify querying/canceling sessions, banning devices, user management.
  - Preconditions: Mock APIs.
  - Automated Steps: Navigate to operator panel; query open sessions; cancel one; ban peer; edit user permissions.
  - Expected: Lists updated; actions logged; MCP notifications if changes.
  - Linked: FR-13.1-13.8; TR-15.1-15.10.

- Edge: Session limit reached → warning UI.

##### 4.12 Prompt Templates (FR-14)
- **M-12.1**: Verify accessing/submitting templates with variable input UI.
  - Preconditions: Mock templates from server.
  - Automated Steps: Select template; input variables via prompts; submit → rendered text sent.
  - Expected: Handlebars evaluated; flow compact.
  - Linked: FR-14.1-14.4; TR-17.1-17.4.

##### 4.13 Connection Protection (FR-15) – Server-side; test UI feedback on throttle (e.g., error messages).

##### 4.14 Test Execution Policy (FR-16) – Plan aligns: Isolated workflow for UI tests, not in default pipeline.

#### 5. Execution Strategy
- **Running Tests**: Mobile: `dotnet test --filter Category=UI` with Appium auto-start. Desktop: Headless mode via xUnit console.
- **CI/CD**: GitHub Actions workflow (per TR-7): Trigger on PR/main; run on emulators (Android) and hosts (desktop). Exclude from default build (TR-8.4.1).
- **Frequency**: Nightly + on-demand.
- **Reporting**: Allure for visuals/screenshots; coverage via ReportGenerator (>80% UI path coverage).

#### 6. Metrics and Identified Gaps
- **Success Criteria**: 95% pass rate; coverage of all FR UI flows.
- **Gaps**: Media storage edge (large files); full DoS UI (needs real server); accessibility tests (e.g., screen reader). Mitigation: Add in future recursion if code evolves.
