using System.Collections.ObjectModel;
using FluentAssertions;
using RemoteAgent.App.Handlers;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Tests;

/// <summary>Unit tests for all 8 mobile CQRS handlers: Connect, Disconnect, CreateSession, TerminateSession, SendMessage, SendAttachment, ArchiveMessage, UsePromptTemplate. FR-2.1, FR-2.2, FR-4.1, FR-11.1, FR-12.5, FR-12.6; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-2.1")]
[Trait("Requirement", "FR-2.2")]
[Trait("Requirement", "FR-4.1")]
[Trait("Requirement", "FR-11.1")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "FR-12.6")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public sealed class MobileHandlerTests
{
    // -------------------------------------------------------------------------
    // Stubs (internal so PortPickerViewModelTests can reuse them)
    // -------------------------------------------------------------------------

    private sealed class StubGateway : IAgentGatewayClient
    {
        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public bool IsConnected { get; set; }
        public string? PerRequestContext { get; set; }

        public bool DisconnectCalled { get; private set; }
        public string? LastSentText { get; private set; }
        public bool SendMediaCalled { get; private set; }
        public bool SetArchivedCalled { get; private set; }
        public Exception? ConnectException { get; set; }

        public event Action? ConnectionStateChanged;
        public event Action<ChatMessage>? MessageReceived;

        public void LoadFromStore(string? sessionId) { }
        public void AddUserMessage(ChatMessage message) => Messages.Add(message);
        public void SetArchived(ChatMessage message, bool archived) { SetArchivedCalled = true; }

        public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null,
            string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        {
            if (ConnectException != null) throw ConnectException;
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken ct = default)
        {
            LastSentText = text;
            return Task.CompletedTask;
        }

        public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default)
        {
            SendMediaCalled = true;
            return Task.CompletedTask;
        }

        public Task StopSessionAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void Disconnect()
        {
            DisconnectCalled = true;
            IsConnected = false;
        }

        // Helper to fire events from tests
        public void RaiseConnectionStateChanged() => ConnectionStateChanged?.Invoke();
        public void RaiseMessageReceived(ChatMessage msg) => MessageReceived?.Invoke(msg);
    }

    private sealed class StubSessionStore : ISessionStore
    {
        private readonly List<SessionItem> _sessions = new();

        public IReadOnlyList<SessionItem> GetAll() => _sessions;
        public SessionItem? Get(string sessionId) =>
            _sessions.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.Ordinal));
        public void Add(SessionItem session) => _sessions.Add(session);
        public void UpdateTitle(string sessionId, string title) { }
        public void UpdateAgentId(string sessionId, string agentId) { }
        public void UpdateConnectionMode(string sessionId, string connectionMode) { }
        public void Delete(string sessionId) =>
            _sessions.RemoveAll(s => string.Equals(s.SessionId, sessionId, StringComparison.Ordinal));
    }

    private sealed class StubApiClient : IServerApiClient
    {
        public ServerInfoResponse? ServerInfo { get; set; }
        public SessionCapacitySnapshot? Capacity { get; set; }
        public ListPromptTemplatesResponse? PromptTemplates { get; set; }

        public string? ReceivedServerInfoApiKey { get; private set; }
        public string? ReceivedCapacityApiKey { get; private set; }

        public Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        {
            ReceivedServerInfoApiKey = apiKey;
            return Task.FromResult(ServerInfo);
        }

        public Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(string host, int port, string? agentId = null, string? apiKey = null, CancellationToken ct = default)
        {
            ReceivedCapacityApiKey = apiKey;
            return Task.FromResult(Capacity);
        }

        public Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult(PromptTemplates);

        public Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<ListMcpServersResponse?>(null);

        public Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<UpsertMcpServerResponse?>(null);

        public Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<DeleteMcpServerResponse?>(null);

        public Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<GetPluginsResponse?>(null);

        public Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<UpdatePluginsResponse?>(null);

        public Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<UpsertPromptTemplateResponse?>(null);

        public Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<DeletePromptTemplateResponse?>(null);

        public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<GetAgentMcpServersResponse?>(null);

        public Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<SetAgentMcpServersResponse?>(null);

        public Task<SeedSessionContextResponse?> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source = null, string? correlationId = null, string? apiKey = null, CancellationToken ct = default)
            => Task.FromResult<SeedSessionContextResponse?>(null);
    }

    private sealed class StubConnectionModeSelector(string? mode) : IConnectionModeSelector
    {
        public Task<string?> SelectAsync() => Task.FromResult(mode);
    }

    private sealed class StubAgentSelector(string? agentId) : IAgentSelector
    {
        public Task<string?> SelectAsync(ServerInfoResponse serverInfo) => Task.FromResult(agentId);
    }

    private sealed class StubAttachmentPicker(PickedAttachment? attachment) : IAttachmentPicker
    {
        public Task<PickedAttachment?> PickAsync() => Task.FromResult(attachment);
    }

    private sealed class StubPromptTemplateSelector(PromptTemplateDefinition? template) : IPromptTemplateSelector
    {
        public Task<PromptTemplateDefinition?> SelectAsync(IReadOnlyList<PromptTemplateDefinition> templates)
            => Task.FromResult(template);
    }

    private sealed class StubPromptVariableProvider(string? value = "test-value") : IPromptVariableProvider
    {
        public Task<string?> GetValueAsync(string variableName) => Task.FromResult(value);
    }

    private sealed class StubTerminationConfirmation(bool result) : ISessionTerminationConfirmation
    {
        public Task<bool> ConfirmAsync(string sessionLabel) => Task.FromResult(result);
    }

    private sealed class NullAppPreferences : IAppPreferences
    {
        public string Get(string key, string defaultValue) => defaultValue;
        public void Set(string key, string value) { }
    }

    private sealed class NullNotificationService : INotificationService
    {
        public void Show(string title, string body) { }
    }

    private sealed class NullRequestDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            => Task.FromResult(default(TResponse)!);
    }

    private sealed class NullDeepLinkService : IDeepLinkService
    {
        public void Subscribe(Action<string> handler) { }
        public void Dispatch(string rawUri) { }
    }

    // -------------------------------------------------------------------------
    // Workspace factory
    // -------------------------------------------------------------------------

    internal static MainPageViewModel CreateDefaultViewModel() => CreateWorkspace();

    private static MainPageViewModel CreateWorkspace(
        StubGateway? gateway = null,
        StubSessionStore? sessionStore = null,
        StubApiClient? apiClient = null,
        IAppPreferences? preferences = null,
        IConnectionModeSelector? connectionModeSelector = null,
        IAgentSelector? agentSelector = null,
        IAttachmentPicker? attachmentPicker = null,
        IPromptTemplateSelector? templateSelector = null,
        IPromptVariableProvider? variableProvider = null,
        ISessionTerminationConfirmation? terminationConfirmation = null,
        INotificationService? notificationService = null,
        IRequestDispatcher? dispatcher = null)
    {
        return new MainPageViewModel(
            sessionStore ?? new StubSessionStore(),
            gateway ?? new StubGateway(),
            apiClient ?? new StubApiClient(),
            preferences ?? new NullAppPreferences(),
            connectionModeSelector ?? new StubConnectionModeSelector(null),
            agentSelector ?? new StubAgentSelector(null),
            attachmentPicker ?? new StubAttachmentPicker(null),
            templateSelector ?? new StubPromptTemplateSelector(null),
            variableProvider ?? new StubPromptVariableProvider(),
            terminationConfirmation ?? new StubTerminationConfirmation(false),
            notificationService ?? new NullNotificationService(),
            dispatcher ?? new NullRequestDispatcher(),
            new NullDeepLinkService());
    }

    // -------------------------------------------------------------------------
    // ConnectMobileSessionHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Connect_WhenModeSelectedAndServerReachable_ShouldConnectAndReturnOk()
    {
        var gateway = new StubGateway();
        var modeSelector = new StubConnectionModeSelector("server");
        var agentSelector = new StubAgentSelector("agent1");
        var apiClient = new StubApiClient
        {
            ServerInfo = new ServerInfoResponse(),
            Capacity = new SessionCapacitySnapshot(true, "", 10, 0, 10, "agent1", null, 0, null)
        };
        var workspace = CreateWorkspace(gateway: gateway, connectionModeSelector: modeSelector,
            agentSelector: agentSelector, apiClient: apiClient);
        workspace.Host = "localhost";
        workspace.Port = "5243";

        var handler = new ConnectMobileSessionHandler(gateway, new StubSessionStore(), apiClient,
            modeSelector, agentSelector, new NullAppPreferences());

        var result = await handler.HandleAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        workspace.Status.Should().Contain("Connected");
    }

    [Fact]
    public async Task Connect_WhenModeSelectorCancelled_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var workspace = CreateWorkspace(gateway: gateway, connectionModeSelector: new StubConnectionModeSelector(null));

        var handler = new ConnectMobileSessionHandler(gateway, new StubSessionStore(), new StubApiClient(),
            new StubConnectionModeSelector(null), new StubAgentSelector(null), new NullAppPreferences());

        var result = await handler.HandleAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task Connect_WhenHostEmptyAndNotDirect_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var modeSelector = new StubConnectionModeSelector("server");
        var workspace = CreateWorkspace(gateway: gateway, connectionModeSelector: modeSelector);
        workspace.Host = "";

        var handler = new ConnectMobileSessionHandler(gateway, new StubSessionStore(), new StubApiClient(),
            modeSelector, new StubAgentSelector("agent1"), new NullAppPreferences());

        var result = await handler.HandleAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        workspace.Status.Should().Contain("host");
    }

    [Fact]
    public async Task Connect_WhenGatewayConnectThrows_ShouldReturnFail()
    {
        var gateway = new StubGateway { ConnectException = new Exception("connection refused") };
        var modeSelector = new StubConnectionModeSelector("direct");
        var workspace = CreateWorkspace(gateway: gateway, connectionModeSelector: modeSelector);
        workspace.Host = "localhost";
        workspace.Port = "5243";

        var handler = new ConnectMobileSessionHandler(gateway, new StubSessionStore(), new StubApiClient(),
            modeSelector, new StubAgentSelector("process"), new NullAppPreferences());

        var result = await handler.HandleAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        workspace.Status.Should().Contain("Failed");
    }

    [Fact]
    public async Task Connect_WithApiKey_ShouldForwardKeyToServerInfoAndCapacityChecks()
    {
        // Regression: both GetServerInfoAsync and GetSessionCapacityAsync must receive the
        // workspace ApiKey — without it, a service configured with Agent:ApiKey returns 401
        // and the capacity check returns null → "Could not verify server session capacity."
        var gateway = new StubGateway();
        var modeSelector = new StubConnectionModeSelector("server");
        var agentSelector = new StubAgentSelector("agent1");
        var apiClient = new StubApiClient
        {
            ServerInfo = new ServerInfoResponse(),
            Capacity = new SessionCapacitySnapshot(true, "", 10, 0, 10, "agent1", null, 0, null)
        };
        var workspace = CreateWorkspace(gateway: gateway, connectionModeSelector: modeSelector,
            agentSelector: agentSelector, apiClient: apiClient);
        workspace.Host = "192.168.1.10";
        workspace.Port = "5243";
        workspace.ApiKey = "test-secret-key";

        var handler = new ConnectMobileSessionHandler(gateway, new StubSessionStore(), apiClient,
            modeSelector, agentSelector, new NullAppPreferences());

        var result = await handler.HandleAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        apiClient.ReceivedServerInfoApiKey.Should().Be("test-secret-key");
        apiClient.ReceivedCapacityApiKey.Should().Be("test-secret-key");
    }

    // -------------------------------------------------------------------------
    // DisconnectMobileSessionHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Disconnect_ShouldCallGatewayDisconnectAndReturnOk()
    {
        var gateway = new StubGateway { IsConnected = true };
        var workspace = CreateWorkspace(gateway: gateway);

        var handler = new DisconnectMobileSessionHandler(gateway);
        var result = await handler.HandleAsync(new DisconnectMobileSessionRequest(Guid.NewGuid(), workspace));

        gateway.DisconnectCalled.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // CreateMobileSessionHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSession_ShouldAddSessionToWorkspaceAndReturnOk()
    {
        var sessionStore = new StubSessionStore();
        var workspace = CreateWorkspace(sessionStore: sessionStore);
        workspace.Sessions.Should().BeEmpty();

        var handler = new CreateMobileSessionHandler(sessionStore);

        var result = await handler.HandleAsync(new CreateMobileSessionRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        workspace.Sessions.Should().HaveCount(1);
        workspace.CurrentSession.Should().NotBeNull();
        workspace.CurrentSession!.Title.Should().Be("New chat");
    }

    // -------------------------------------------------------------------------
    // TerminateMobileSessionHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Terminate_WhenConfirmed_ShouldRemoveSessionAndReturnOk()
    {
        var gateway = new StubGateway { IsConnected = false };
        var sessionStore = new StubSessionStore();
        var confirmation = new StubTerminationConfirmation(true);
        var workspace = CreateWorkspace(gateway: gateway, sessionStore: sessionStore,
            terminationConfirmation: confirmation);

        var session = new SessionItem { SessionId = "abc", Title = "Chat 1" };
        workspace.Sessions.Add(session);
        // Do NOT make it the current session so gateway disconnect is not triggered.

        var handler = new TerminateMobileSessionHandler(gateway, sessionStore, confirmation);

        var result = await handler.HandleAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), session, workspace));

        result.Success.Should().BeTrue();
        workspace.Sessions.Should().NotContain(session);
    }

    [Fact]
    public async Task Terminate_WhenCancelled_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var sessionStore = new StubSessionStore();
        var confirmation = new StubTerminationConfirmation(false);
        var workspace = CreateWorkspace(gateway: gateway, sessionStore: sessionStore,
            terminationConfirmation: confirmation);
        var session = new SessionItem { SessionId = "abc", Title = "Chat 1" };

        var handler = new TerminateMobileSessionHandler(gateway, sessionStore, confirmation);

        var result = await handler.HandleAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), session, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task Terminate_WhenSessionNull_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var sessionStore = new StubSessionStore();
        var confirmation = new StubTerminationConfirmation(true);
        var workspace = CreateWorkspace(gateway: gateway, sessionStore: sessionStore,
            terminationConfirmation: confirmation);

        var handler = new TerminateMobileSessionHandler(gateway, sessionStore, confirmation);

        var result = await handler.HandleAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No session");
    }

    [Fact]
    public async Task Terminate_WhenCurrentSessionTerminated_ShouldDisconnectGateway()
    {
        var gateway = new StubGateway { IsConnected = true };
        var sessionStore = new StubSessionStore();
        var confirmation = new StubTerminationConfirmation(true);
        var workspace = CreateWorkspace(gateway: gateway, sessionStore: sessionStore,
            terminationConfirmation: confirmation);

        var session = new SessionItem { SessionId = "current-session", Title = "Active" };
        workspace.Sessions.Add(session);
        workspace.CurrentSession = session;

        var handler = new TerminateMobileSessionHandler(gateway, sessionStore, confirmation);

        var result = await handler.HandleAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), session, workspace));

        result.Success.Should().BeTrue();
        gateway.DisconnectCalled.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // SendMobileMessageHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendMessage_WhenConnectedWithText_ShouldSendAndClearPending()
    {
        var gateway = new StubGateway { IsConnected = true };
        var workspace = CreateWorkspace(gateway: gateway);
        workspace.PendingMessage = "hello";

        var handler = new SendMobileMessageHandler(gateway);

        var result = await handler.HandleAsync(new SendMobileMessageRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        gateway.LastSentText.Should().Be("hello");
        workspace.PendingMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task SendMessage_WhenNotConnected_ShouldReturnFail()
    {
        var gateway = new StubGateway { IsConnected = false };
        var workspace = CreateWorkspace(gateway: gateway);
        workspace.PendingMessage = "hello";

        var handler = new SendMobileMessageHandler(gateway);

        var result = await handler.HandleAsync(new SendMobileMessageRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_WhenPendingEmpty_ShouldReturnFail()
    {
        var gateway = new StubGateway { IsConnected = true };
        var workspace = CreateWorkspace(gateway: gateway);
        workspace.PendingMessage = "";

        var handler = new SendMobileMessageHandler(gateway);

        var result = await handler.HandleAsync(new SendMobileMessageRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // SendMobileAttachmentHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAttachment_WhenConnectedAndFilePicked_ShouldSendMediaAndReturnOk()
    {
        var gateway = new StubGateway { IsConnected = true };
        var picker = new StubAttachmentPicker(new PickedAttachment(new byte[] { 1, 2, 3 }, "image/png", "photo.png"));
        var workspace = CreateWorkspace(gateway: gateway, attachmentPicker: picker);

        var handler = new SendMobileAttachmentHandler(gateway, picker);

        var result = await handler.HandleAsync(new SendMobileAttachmentRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        gateway.SendMediaCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SendAttachment_WhenNotConnected_ShouldReturnFail()
    {
        var gateway = new StubGateway { IsConnected = false };
        var picker = new StubAttachmentPicker(new PickedAttachment(new byte[] { 1 }, "image/png", "x.png"));
        var workspace = CreateWorkspace(gateway: gateway, attachmentPicker: picker);

        var handler = new SendMobileAttachmentHandler(gateway, picker);

        var result = await handler.HandleAsync(new SendMobileAttachmentRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendAttachment_WhenPickerReturnsNull_ShouldReturnFail()
    {
        var gateway = new StubGateway { IsConnected = true };
        var picker = new StubAttachmentPicker(null);
        var workspace = CreateWorkspace(gateway: gateway, attachmentPicker: picker);

        var handler = new SendMobileAttachmentHandler(gateway, picker);

        var result = await handler.HandleAsync(new SendMobileAttachmentRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No attachment");
    }

    // -------------------------------------------------------------------------
    // ArchiveMobileMessageHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Archive_WhenMessageNotNull_ShouldArchiveAndReturnOk()
    {
        var gateway = new StubGateway();
        var workspace = CreateWorkspace(gateway: gateway);
        var message = new ChatMessage { IsUser = false, Text = "Agent reply" };

        var handler = new ArchiveMobileMessageHandler(gateway);

        var result = await handler.HandleAsync(new ArchiveMobileMessageRequest(Guid.NewGuid(), message, workspace));

        result.Success.Should().BeTrue();
        message.IsArchived.Should().BeTrue();
        gateway.SetArchivedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_WhenMessageNull_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var workspace = CreateWorkspace(gateway: gateway);

        var handler = new ArchiveMobileMessageHandler(gateway);

        var result = await handler.HandleAsync(new ArchiveMobileMessageRequest(Guid.NewGuid(), null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No message");
    }

    // -------------------------------------------------------------------------
    // UsePromptTemplateHandler
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UseTemplate_WhenTemplatesAvailableAndFilled_ShouldSendRenderedMessageAndReturnOk()
    {
        var gateway = new StubGateway { IsConnected = true };
        var template = new PromptTemplateDefinition
        {
            TemplateId = "t1",
            DisplayName = "Greet",
            TemplateContent = "Hello World"  // no variables
        };
        var templates = new ListPromptTemplatesResponse();
        templates.Templates.Add(template);

        var apiClient = new StubApiClient { PromptTemplates = templates };
        var templateSelector = new StubPromptTemplateSelector(template);
        var variableProvider = new StubPromptVariableProvider();

        var workspace = CreateWorkspace(gateway: gateway, apiClient: apiClient,
            templateSelector: templateSelector, variableProvider: variableProvider);
        workspace.Host = "localhost";
        workspace.Port = "5243";

        var handler = new UsePromptTemplateHandler(apiClient, templateSelector, variableProvider, gateway);

        var result = await handler.HandleAsync(new UsePromptTemplateRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        gateway.LastSentText.Should().Be("Hello World");
    }

    [Fact]
    public async Task UseTemplate_WhenHostEmpty_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var workspace = CreateWorkspace(gateway: gateway);
        workspace.Host = "";

        var handler = new UsePromptTemplateHandler(new StubApiClient(),
            new StubPromptTemplateSelector(null), new StubPromptVariableProvider(), gateway);

        var result = await handler.HandleAsync(new UsePromptTemplateRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UseTemplate_WhenTemplateListEmpty_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var workspace = CreateWorkspace(gateway: gateway);
        workspace.Host = "localhost";
        workspace.Port = "5243";

        var apiClient = new StubApiClient { PromptTemplates = new ListPromptTemplatesResponse() };
        var handler = new UsePromptTemplateHandler(apiClient,
            new StubPromptTemplateSelector(null), new StubPromptVariableProvider(), gateway);

        var result = await handler.HandleAsync(new UsePromptTemplateRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        workspace.Status.Should().Contain("No prompt templates");
    }

    [Fact]
    public async Task UseTemplate_WhenTemplateSelectorCancelled_ShouldReturnFail()
    {
        var gateway = new StubGateway();
        var template = new PromptTemplateDefinition { TemplateId = "t1", TemplateContent = "Hello" };
        var templates = new ListPromptTemplatesResponse();
        templates.Templates.Add(template);

        var apiClient = new StubApiClient { PromptTemplates = templates };
        var workspace = CreateWorkspace(gateway: gateway, apiClient: apiClient);
        workspace.Host = "localhost";
        workspace.Port = "5243";

        var handler = new UsePromptTemplateHandler(apiClient,
            new StubPromptTemplateSelector(null), new StubPromptVariableProvider(), gateway);

        var result = await handler.HandleAsync(new UsePromptTemplateRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }
}
