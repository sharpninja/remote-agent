# Requirements Test Coverage

This document maps every requirement ID in `docs/functional-requirements.md` (FR) and `docs/technical-requirements.md` (TR) to current automated test coverage.

Coverage status values:
- `Covered`: direct automated test coverage exists.
- `Partial`: some behavior is covered, but not the full requirement scope.
- `None`: no automated test currently covers the requirement.

## Functional Requirements (FR)

| Requirement | Coverage | Tests |
|---|---|---|
| FR-1.1 | Partial | `HostBootstrapSmokeTests.RootEndpoint_RespondsQuickly` (`tests/RemoteAgent.Service.IntegrationTests/HostBootstrapSmokeTests.cs`), `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| FR-1.2 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`), `AgentGatewayServiceIntegrationTests_NoCommand.Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_NoCommand.cs`) |
| FR-1.3 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| FR-1.4 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| FR-1.5 | Partial | `StructuredLogServiceTests.*` (`tests/RemoteAgent.Service.Tests/StructuredLogServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-1.6 | Partial | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`), `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| FR-2.1 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| FR-2.2 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| FR-2.3 | Covered | `MarkdownFormatTests.*` (`tests/RemoteAgent.App.Tests/MarkdownFormatTests.cs`), `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| FR-2.4 | Covered | `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`), `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| FR-2.5 | None | None |
| FR-2.6 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-2.7 | Covered | `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| FR-3.1 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| FR-3.2 | None | None |
| FR-3.3 | None | None |
| FR-3.4 | Partial | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| FR-4.1 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| FR-4.2 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| FR-5.1 | None | None |
| FR-5.2 | None | None |
| FR-5.3 | None | None |
| FR-6.1 | None | None |
| FR-6.2 | None | None |
| FR-7.1 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`) |
| FR-7.2 | Covered | `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`), `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`), `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| FR-8.1 | Covered | `PluginConfigurationServiceTests.*` (`tests/RemoteAgent.Service.Tests/PluginConfigurationServiceTests.cs`), `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| FR-9.1 | Partial | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| FR-9.2 | None | None |
| FR-10.1 | None | None |
| FR-11.1 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-11.1.1 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-11.1.2 | Covered | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| FR-11.1.3 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-11.1.3.1 | None | None |
| FR-11.1.3.2 | None | None |
| FR-12.1 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.1.1 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.1.2 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.1.3 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.1.4 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.1.5 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.2 | Partial | `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-12.3 | Covered | `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`) |
| FR-12.4 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-12.5 | Partial | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| FR-12.6 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-12.7 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-12.8 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-12.9 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.10 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| FR-12.11 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`) |
| FR-12.12 | Not covered | Pending implementation of management app log view |
| FR-13.1 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `SessionCapacityServiceTests.MarkSessionAbandoned_ShouldTrackAndClearOnRegister` (`tests/RemoteAgent.Service.Tests/SessionCapacityServiceTests.cs`) |
| FR-13.2 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`) |
| FR-13.3 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-13.4 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`) |
| FR-13.5 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `AuthUserServiceTests.*` (`tests/RemoteAgent.Service.Tests/AuthUserServiceTests.cs`) |
| FR-13.6 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-13.7 | Covered | `SessionCapacityServiceTests_CapacityLimits.*` (`tests/RemoteAgent.Service.Tests/SessionCapacityServiceTests_CapacityLimits.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-13.8 | Covered | `SessionCapacityServiceTests_CapacityLimits.*` (`tests/RemoteAgent.Service.Tests/SessionCapacityServiceTests_CapacityLimits.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-14.1 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-14.2 | Covered | `PromptTemplateEngineTests.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests.cs`), `PromptTemplateEngineTests_EdgeCases.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests_EdgeCases.cs`) |
| FR-14.3 | Partial | `PromptTemplateEngineTests.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-14.4 | Partial | `PromptTemplateEngineTests.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-15.1 | Covered | `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| FR-15.2 | Covered | `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`) |
| FR-16.1 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`), `AgentGatewayServiceIntegrationTests_NoCommand.Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_NoCommand.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |

## Technical Requirements (TR)

| Requirement | Coverage | Tests |
|---|---|---|
| TR-1.1 | None | None |
| TR-1.2 | None | None |
| TR-1.3 | None | None |
| TR-1.4 | None | None |
| TR-1.5 | None | None |
| TR-2.1 | Partial | `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| TR-2.1.1 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-2.1.2 | None | None |
| TR-2.2 | Partial | `HostBootstrapSmokeTests.RootEndpoint_RespondsQuickly` (`tests/RemoteAgent.Service.IntegrationTests/HostBootstrapSmokeTests.cs`) |
| TR-2.3 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`), `AgentGatewayServiceIntegrationTests_NoCommand.Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_NoCommand.cs`) |
| TR-2.4 | None | None |
| TR-3.1 | Partial | `HostBootstrapSmokeTests.RootEndpoint_RespondsQuickly` (`tests/RemoteAgent.Service.IntegrationTests/HostBootstrapSmokeTests.cs`) |
| TR-3.2 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`) |
| TR-3.3 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| TR-3.4 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| TR-3.5 | Partial | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| TR-3.6 | Partial | `StructuredLogServiceTests.*` (`tests/RemoteAgent.Service.Tests/StructuredLogServiceTests.cs`) |
| TR-3.7 | Covered | `SessionCapacityServiceTests_CapacityLimits.*` (`tests/RemoteAgent.Service.Tests/SessionCapacityServiceTests_CapacityLimits.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-3.8 | Covered | `SessionCapacityServiceTests_CapacityLimits.*` (`tests/RemoteAgent.Service.Tests/SessionCapacityServiceTests_CapacityLimits.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-4.1 | Partial | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| TR-4.2 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`) |
| TR-4.3 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_CorrelationId.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_CorrelationId.cs`) |
| TR-4.4 | Covered | `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`) |
| TR-4.5 | Covered | `AgentGatewayServiceIntegrationTests_CorrelationId.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_CorrelationId.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-5.1 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| TR-5.2 | Covered | `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| TR-5.3 | Covered | `MarkdownFormatTests.*` (`tests/RemoteAgent.App.Tests/MarkdownFormatTests.cs`), `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| TR-5.4 | None | None |
| TR-5.5 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`) |
| TR-5.6 | None | None |
| TR-5.7 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-5.8 | Covered | `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| TR-6.1 | None | None |
| TR-6.2 | None | None |
| TR-6.3 | None | None |
| TR-7.1 | None | None |
| TR-7.2 | None | None |
| TR-7.3 | None | None |
| TR-7.3.1 | None | None |
| TR-7.3.2 | None | None |
| TR-7.3.3 | None | None |
| TR-7.3.4 | None | None |
| TR-7.3.5 | None | None |
| TR-8.1 | Covered | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`), `AgentOptionsTests.*` (`tests/RemoteAgent.Service.Tests/AgentOptionsTests.cs`), `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`) |
| TR-8.2 | Covered | `MarkdownFormatTests.*` (`tests/RemoteAgent.App.Tests/MarkdownFormatTests.cs`), `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`), `AgentOptionsTests.*` (`tests/RemoteAgent.Service.Tests/AgentOptionsTests.cs`) |
| TR-8.3 | Covered | `AgentGatewayServiceIntegrationTests_NoCommand.Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_NoCommand.cs`), `AgentGatewayServiceIntegrationTests_Echo.Connect_StartThenSendText_ReceivesEchoFromAgent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Echo.cs`), `AgentGatewayServiceIntegrationTests_Stop.Connect_SendStop_ReceivesSessionStoppedEvent` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_Stop.cs`) |
| TR-8.4 | Partial | `ChatMessageTests.*` (`tests/RemoteAgent.App.Tests/ChatMessageTests.cs`), `AgentOptionsTests.*` (`tests/RemoteAgent.Service.Tests/AgentOptionsTests.cs`), `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-8.4.1 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-8.5 | Covered | `MobileConnectionUiTests.*` (`tests/RemoteAgent.Mobile.UiTests/MobileConnectionUiTests.cs`) |
| TR-8.6 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-9.1 | None | None |
| TR-9.2 | None | None |
| TR-9.3 | None | None |
| TR-10.1 | Covered | `PluginConfigurationServiceTests.*` (`tests/RemoteAgent.Service.Tests/PluginConfigurationServiceTests.cs`), `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| TR-10.2 | Covered | `PluginConfigurationServiceTests.*` (`tests/RemoteAgent.Service.Tests/PluginConfigurationServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-11.1 | Covered | `LiteDbLocalStorageTests.SessionExists_ReturnsTrue_WhenEntriesExist` (`tests/RemoteAgent.Service.Tests/LiteDbLocalStorageTests.cs`), `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`) |
| TR-11.2 | None | None |
| TR-11.3 | None | None |
| TR-12.1 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-12.1.1 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-12.1.2 | Covered | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| TR-12.1.3 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-12.2 | Covered | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| TR-12.2.1 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-12.2.2 | None | None |
| TR-13.1 | Covered | `StructuredLogServiceTests.*` (`tests/RemoteAgent.Service.Tests/StructuredLogServiceTests.cs`) |
| TR-13.2 | Covered | `StructuredLogServiceTests.*` (`tests/RemoteAgent.Service.Tests/StructuredLogServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-13.3 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-13.4 | Covered | `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`) |
| TR-13.5 | Covered | `StructuredLogStoreTests.Query_ShouldApplyFilterCriteria` (`tests/RemoteAgent.Desktop.UiTests/StructuredLogStoreTests.cs`) |
| TR-13.6 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.0 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.1 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.2 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.3 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.4 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.5 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.6 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.7 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.8 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.9 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.1.10 | Covered | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.2 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-14.3 | Covered | `AgentGatewayServiceIntegrationTests_GetServerInfo.GetServerInfo_ReturnsVersionAndCapabilities` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_GetServerInfo.cs`) |
| TR-14.4 | None | None |
| TR-14.5 | None | None |
| TR-15.1 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.2 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`) |
| TR-15.3 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `AuthUserServiceTests.*` (`tests/RemoteAgent.Service.Tests/AuthUserServiceTests.cs`) |
| TR-15.4 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.5 | Covered | `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.6 | Covered | `ConnectionProtectionServiceTests.*` (`tests/RemoteAgent.Service.Tests/ConnectionProtectionServiceTests.cs`) |
| TR-15.7 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.8 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.9 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-15.10 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`), `AuthUserServiceTests.*` (`tests/RemoteAgent.Service.Tests/AuthUserServiceTests.cs`) |
| TR-16.1 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-16.2 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-16.3 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-16.4 | Covered | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-16.5 | Partial | `MainWindowUiTests.*` (`tests/RemoteAgent.Desktop.UiTests/MainWindowUiTests.cs`) |
| TR-16.6 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-16.7 | Partial | `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-17.1 | Covered | `PromptTemplateServiceTests.*` (`tests/RemoteAgent.Service.Tests/PromptTemplateServiceTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |
| TR-17.2 | Covered | `PromptTemplateServiceTests.*` (`tests/RemoteAgent.Service.Tests/PromptTemplateServiceTests.cs`) |
| TR-17.3 | Covered | `PromptTemplateEngineTests.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests.cs`), `PromptTemplateEngineTests_EdgeCases.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests_EdgeCases.cs`) |
| TR-17.4 | Partial | `PromptTemplateEngineTests.*` (`tests/RemoteAgent.App.Tests/PromptTemplateEngineTests.cs`), `AgentGatewayServiceIntegrationTests_ManagementApis.*` (`tests/RemoteAgent.Service.IntegrationTests/AgentGatewayServiceIntegrationTests_ManagementApis.cs`) |

## Notes

- This is an automated-test matrix only. Some `None` rows may still be validated by implementation review, workflow checks, or manual QA.
- If you want, this matrix can be tightened further by adding direct FR/TR annotations in every test method and generating this file automatically from source comments.
