using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Services;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class OpenNewSessionHandlerTests
{
    [AvaloniaFact]
    public async Task HandleAsync_WhenDialogReturnsNull_ShouldReturnCancelled()
    {
        var dialogService = new MockDialogService(result: null);
        var dispatcher = new StubDispatcher();
        var handler = new OpenNewSessionHandler(dialogService, dispatcher);
        var workspace = CreateWorkspace();

        var result = await handler.HandleAsync(new OpenNewSessionRequest(
            Guid.NewGuid(), () => new Window(), workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cancelled.");
    }

    [AvaloniaFact]
    public async Task HandleAsync_WhenDialogReturnsResult_ShouldApplyToWorkspace()
    {
        var dialogResult = new ConnectionSettingsDialogResult(
            "newhost", "8080", "client", "agent2", "key123", "context");
        var dialogService = new MockDialogService(dialogResult);
        var dispatcher = new StubDispatcher(CommandResult.Ok());
        var handler = new OpenNewSessionHandler(dialogService, dispatcher);
        var workspace = CreateWorkspace();

        var result = await handler.HandleAsync(new OpenNewSessionRequest(
            Guid.NewGuid(), () => new Window(), workspace));

        result.Success.Should().BeTrue();
        workspace.Host.Should().Be("newhost");
        workspace.Port.Should().Be("8080");
        workspace.SelectedConnectionMode.Should().Be("client");
        workspace.SelectedAgentId.Should().Be("agent2");
        workspace.ApiKey.Should().Be("key123");
        workspace.PerRequestContext.Should().Be("context");
    }

    [Fact]
    public async Task HandleAsync_WhenNoOwnerWindow_ShouldReturnFail()
    {
        var dialogService = new MockDialogService(result: null);
        var dispatcher = new StubDispatcher();
        var handler = new OpenNewSessionHandler(dialogService, dispatcher);
        var workspace = CreateWorkspace();

        var result = await handler.HandleAsync(new OpenNewSessionRequest(
            Guid.NewGuid(), () => null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("owner window");
    }

    [Fact]
    public void Request_ToString_ShouldNotThrow()
    {
        var workspace = CreateWorkspace();
        var request = new OpenNewSessionRequest(Guid.NewGuid(), () => null, workspace);

        var str = request.ToString();

        str.Should().Contain("OpenNewSessionRequest");
        str.Should().Contain("127.0.0.1");
    }

    private static ServerWorkspaceViewModel CreateWorkspace()
    {
        var context = new CurrentServerContext
        {
            Registration = new ServerRegistration
            {
                ServerId = "srv-test",
                DisplayName = "Test",
                Host = "127.0.0.1",
                Port = 5243,
                ApiKey = ""
            }
        };
        return new ServerWorkspaceViewModel(
            context,
            new StubCapacityClient(),
            new StubLogStore(),
            new StubSessionFactory(),
            new NullDispatcher());
    }

    private sealed class NullDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResponse)!);
    }

    private sealed class StubDispatcher : IRequestDispatcher
    {
        private readonly object? _response;

        public StubDispatcher(object? response = null)
        {
            _response = response;
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_response is TResponse typed)
                return Task.FromResult(typed);
            // For unregistered/unmatched request types, return default (handles CommandResult for CreateDesktopSessionRequest)
            if (typeof(TResponse) == typeof(CommandResult))
                return Task.FromResult((TResponse)(object)CommandResult.Ok());
            throw new InvalidOperationException($"No stub response configured for {typeof(TResponse).Name}");
        }
    }

    private sealed class MockDialogService(ConnectionSettingsDialogResult? result) : IConnectionSettingsDialogService
    {
        public Task<ConnectionSettingsDialogResult?> ShowAsync(
            Window ownerWindow, ConnectionSettingsDefaults defaults, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class StubCapacityClient : IServerCapacityClient
    {
        public Task<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?> GetCapacityAsync(string host, int port, string? agentId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?>(null);
        public Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OpenServerSessionSnapshot>>([]);
        public Task<bool> TerminateSessionAsync(string host, int port, string sessionId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AbandonedServerSessionSnapshot>>([]);
        public Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPeerSnapshot>>([]);
        public Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(string host, int port, int limit, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ConnectionHistorySnapshot>>([]);
        public Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BannedPeerSnapshot>>([]);
        public Task<bool> BanPeerAsync(string host, int port, string peer, string? reason, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<bool> UnbanPeerAsync(string host, int port, string peer, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuthUserSnapshot>>([]);
        public Task<IReadOnlyList<string>> GetPermissionRolesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
        public Task<AuthUserSnapshot?> UpsertAuthUserAsync(string host, int port, AuthUserSnapshot user, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthUserSnapshot?>(user);
        public Task<bool> DeleteAuthUserAsync(string host, int port, string userId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<PluginConfigurationSnapshot?> GetPluginsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<PluginConfigurationSnapshot?>(null);
        public Task<PluginConfigurationSnapshot?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<PluginConfigurationSnapshot?>(null);
        public Task<IReadOnlyList<McpServerDefinition>> ListMcpServersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<McpServerDefinition>>([]);
        public Task<McpServerDefinition?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<McpServerDefinition?>(server);
        public Task<bool> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<Proto.GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Proto.GetAgentMcpServersResponse?>(null);
        public Task<bool> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<IReadOnlyList<PromptTemplateDefinition>> ListPromptTemplatesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PromptTemplateDefinition>>([]);
        public Task<PromptTemplateDefinition?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<PromptTemplateDefinition?>(template);
        public Task<bool> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
        public Task<bool> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source, string? correlationId, string? apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class StubLogStore : IDesktopStructuredLogStore
    {
        public long GetMaxEventId(string host, int port, string? serverId = null) => 0;
        public IReadOnlyList<DesktopStructuredLogRecord> Query(DesktopStructuredLogFilter? filter = null, int limit = 1000) => [];
        public void UpsertBatch(IEnumerable<DesktopStructuredLogRecord> logs) { }
    }

    private sealed class StubSessionFactory : IDesktopSessionViewModelFactory
    {
        public DesktopSessionViewModel Create(string title, string connectionMode, string agentId) =>
            new(new FakeAgentSession())
            {
                Title = title,
                ConnectionMode = connectionMode,
                AgentId = agentId,
                SessionId = Guid.NewGuid().ToString("N")[..12]
            };
    }

    private sealed class FakeAgentSession : IAgentSessionClient
    {
        public bool IsConnected { get; private set; }
        public bool CanAcceptInput => IsConnected;
        public string? CurrentSessionId => null;
        public ServerInfoResponse? ServerInfo => null;
        public string? PerRequestContext { get; set; }
        public event Action? ConnectionStateChanged;
        public event Action<ChatMessage>? MessageReceived
        { add { } remove { } }

        public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        { IsConnected = true; ConnectionStateChanged?.Invoke(); return Task.CompletedTask; }
        public void Disconnect() { IsConnected = false; ConnectionStateChanged?.Invoke(); }
        public Task SendInputAsync(string input, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendTextAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default) => Task.CompletedTask;
        public Task StopSessionAsync(CancellationToken ct = default) { Disconnect(); return Task.CompletedTask; }
    }
}
