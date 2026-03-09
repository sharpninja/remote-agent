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

/// <summary>Unit tests for <see cref="ScanQrCodeHandler"/> — pairing URL parsing and field population.</summary>
public sealed class ScanQrCodeHandlerTests
{
    // ── minimal stubs ────────────────────────────────────────────────────────

    private sealed class StubScanner(string? result) : IQrCodeScanner
    {
        public bool Called { get; private set; }
        public string? LastLoginUrl { get; private set; }
        public Task<string?> ScanAsync(string loginUrl) { Called = true; LastLoginUrl = loginUrl; return Task.FromResult(result); }
    }

    private sealed class StubPrefs : IAppPreferences
    {
        private readonly Dictionary<string, string> _store = new();
        public string Get(string key, string defaultValue) => _store.TryGetValue(key, out var v) ? v : defaultValue;
        public void Set(string key, string value) => _store[key] = value;
    }

    private sealed class StubGateway : IAgentGatewayClient
    {
        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public bool IsConnected { get; set; }
        public string? PerRequestContext { get; set; }
        public event Action? ConnectionStateChanged;
        public event Action<ChatMessage>? MessageReceived;
        public void RaiseConnectionStateChanged() => ConnectionStateChanged?.Invoke();
        public void RaiseMessageReceived(ChatMessage msg) => MessageReceived?.Invoke(msg);
        public void LoadFromStore(string? sessionId) { }
        public void AddUserMessage(ChatMessage message) { }
        public void SetArchived(ChatMessage message, bool archived) { }
        public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null,
            string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task SendTextAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default) => Task.CompletedTask;
        public Task StopSessionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Disconnect() { }
    }

    private sealed class NullApiClient : IServerApiClient
    {
        public Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<ServerInfoResponse?>(null);
        public Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<ListMcpServersResponse?>(null);
        public Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<UpsertMcpServerResponse?>(null);
        public Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<DeleteMcpServerResponse?>(null);
        public Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<ListPromptTemplatesResponse?>(null);
        public Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<GetPluginsResponse?>(null);
        public Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<UpdatePluginsResponse?>(null);
        public Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<UpsertPromptTemplateResponse?>(null);
        public Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<DeletePromptTemplateResponse?>(null);
        public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<GetAgentMcpServersResponse?>(null);
        public Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SetAgentMcpServersResponse?>(null);
        public Task<SeedSessionContextResponse?> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source = null, string? correlationId = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SeedSessionContextResponse?>(null);
        public Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(string host, int port, string? agentId = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SessionCapacitySnapshot?>(null);
    }

    private sealed class StubDeepLink : IDeepLinkService
    {
        public void Subscribe(Action<string> handler) { }
        public void Dispatch(string rawUri) { }
    }

    private sealed class NullStore : ISessionStore
    {
        private readonly List<SessionItem> _sessions = new();
        public IReadOnlyList<SessionItem> GetAll() => _sessions;
        public SessionItem? Get(string sessionId) => null;
        public void Add(SessionItem session) => _sessions.Add(session);
        public void UpdateTitle(string sessionId, string title) { }
        public void UpdateAgentId(string sessionId, string agentId) { }
        public void UpdateConnectionMode(string sessionId, string connectionMode) { }
        public void Delete(string sessionId) { }
    }

    private sealed class NullAgentSelector : IAgentSelector
    {
        public Task<string?> SelectAsync(ServerInfoResponse serverInfo) => Task.FromResult<string?>(null);
    }

    private sealed class NullAttachmentPicker : IAttachmentPicker
    {
        public Task<PickedAttachment?> PickAsync() => Task.FromResult<PickedAttachment?>(null);
    }

    private sealed class NullTemplateSelector : IPromptTemplateSelector
    {
        public Task<PromptTemplateDefinition?> SelectAsync(IReadOnlyList<PromptTemplateDefinition> templates) => Task.FromResult<PromptTemplateDefinition?>(null);
    }

    private sealed class NullVariableProvider : IPromptVariableProvider
    {
        public Task<string?> GetValueAsync(string variableName) => Task.FromResult<string?>(null);
    }

    private sealed class NullTermination : ISessionTerminationConfirmation
    {
        public Task<bool> ConfirmAsync(string sessionLabel) => Task.FromResult(true);
    }

    private sealed class NullNotification : INotificationService
    {
        public void Show(string title, string body) { }
    }

    private sealed class NullDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) => Task.FromResult(default(TResponse)!);
    }

    private MainPageViewModel BuildVm(StubPrefs prefs, string host = "10.0.0.1", string port = "5244")
    {
        var vm = new MainPageViewModel(new NullStore(), new StubGateway(), new NullApiClient(), prefs,
            new NullAgentSelector(), new NullAttachmentPicker(),
            new NullTemplateSelector(), new NullVariableProvider(), new NullTermination(),
            new NullNotification(), new NullDispatcher(), new StubDeepLink());
        vm.Host = host;
        vm.Port = port;
        return vm;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidUrl_PopulatesHostPortApiKey()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var handler = new ScanQrCodeHandler(
            new StubScanner("remoteagent://pair?host=192.168.1.10&port=5244&key=secret123"), prefs);

        var result = await handler.HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.Host.Should().Be("192.168.1.10");
        vm.Port.Should().Be("5244");
        vm.ApiKey.Should().Be("secret123");
        vm.Status.Should().Contain("Tap Connect");
        prefs.Get("ServerHost", "").Should().Be("192.168.1.10");
        prefs.Get("ServerPort", "").Should().Be("5244");
        prefs.Get("ApiKey", "").Should().Be("secret123");
    }

    [Fact]
    public async Task HandleAsync_CancelledScan_ReturnsFail()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(new StubScanner(null), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
        vm.Status.Should().Contain("cancelled");
    }

    [Fact]
    public async Task HandleAsync_InvalidScheme_ReturnsFail()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(new StubScanner("https://example.com"), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
        vm.Status.Should().Contain("Invalid");
    }

    [Fact]
    public async Task HandleAsync_MissingHost_ReturnsFail()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(new StubScanner("remoteagent://pair?port=5244&key=abc"), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
        vm.Status.Should().Contain("host");
    }

    [Fact]
    public async Task HandleAsync_InvalidPort_ReturnsFail()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(
                new StubScanner("remoteagent://pair?host=10.0.0.1&port=notaport"), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
        vm.Status.Should().Contain("port");
    }

    [Fact]
    public async Task HandleAsync_RawUriProvided_BypassesScanner()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var scanner = new StubScanner(null); // returns null when called
        var handler = new ScanQrCodeHandler(scanner, prefs);

        var result = await handler.HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm)
        {
            RawUri = "remoteagent://pair?host=10.0.0.2&port=5243&key=deeplink"
        });

        result.Success.Should().BeTrue();
        scanner.Called.Should().BeFalse();
        vm.Host.Should().Be("10.0.0.2");
        vm.Port.Should().Be("5243");
        vm.ApiKey.Should().Be("deeplink");
    }

    [Fact]
    public async Task HandleAsync_UrlEncodedValues_AreDecoded()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(
                new StubScanner("remoteagent://pair?host=10.0.0.1&port=5244&key=hello%20world"), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.ApiKey.Should().Be("hello world");
    }

    [Fact]
    public async Task HandleAsync_EmptyApiKey_IsAllowed()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs);
        var result  = await new ScanQrCodeHandler(
                new StubScanner("remoteagent://pair?host=10.0.0.1&port=5244"), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.ApiKey.Should().Be("");
    }

    [Fact]
    public async Task HandleAsync_NoHost_ReturnsFail()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs, host: ""); // no host set
        var result  = await new ScanQrCodeHandler(new StubScanner(null), prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
        vm.Status.Should().Contain("host");
    }

    [Fact]
    public async Task HandleAsync_BuildsLoginUrlFromHostPort()
    {
        var prefs   = new StubPrefs();
        var vm      = BuildVm(prefs, host: "192.168.1.50", port: "5244");
        var scanner = new StubScanner("remoteagent://pair?host=192.168.1.50&port=5244&key=abc");

        await new ScanQrCodeHandler(scanner, prefs)
            .HandleAsync(new ScanQrCodeRequest(Guid.NewGuid(), vm));

        scanner.LastLoginUrl.Should().Be("http://192.168.1.50:15244/pair");
    }
}
