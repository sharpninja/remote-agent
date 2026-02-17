using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Services;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests;

public sealed class MainWindowUiTests
{
    [AvaloniaFact]
    public void MainWindow_ShouldExposeExpectedManagementControls()
    {
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory());
        var window = new MainWindow(vm);

        window.Show();

        window.FindControl<ComboBox>("ServerSelectorComboBox").Should().NotBeNull();
        window.FindControl<Button>("NewServerButton").Should().NotBeNull();
        window.FindControl<Button>("SaveServerButton").Should().NotBeNull();
        window.FindControl<Button>("RemoveServerButton").Should().NotBeNull();
        window.FindControl<TextBox>("ServerHostTextBox").Should().NotBeNull();

        window.FindControl<Button>("NewSessionButton").Should().NotBeNull();
        window.FindControl<Button>("TerminateSessionButton").Should().NotBeNull();
        window.FindControl<Button>("CheckCapacityButton").Should().NotBeNull();
        window.FindControl<Button>("RefreshOpenSessionsButton").Should().NotBeNull();
        window.FindControl<Button>("TerminateOpenServerSessionButton").Should().NotBeNull();
        window.FindControl<Button>("StartLogMonitoringButton").Should().NotBeNull();
        window.FindControl<Button>("ApplyLogFilterButton").Should().NotBeNull();
        window.FindControl<TextBox>("LogServerIdFilterTextBox").Should().NotBeNull();
        window.FindControl<TextBox>("PerRequestContextTextBox").Should().NotBeNull();
        window.FindControl<TabControl>("SessionTabs").Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void CurrentServerWorkspace_ShouldCreateSessionTabs()
    {
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory());

        vm.CurrentServerViewModel.Should().NotBeNull();
        var workspace = vm.CurrentServerViewModel!;
        var initialCount = workspace.Sessions.Count;

        workspace.NewSessionCommand.Execute(null);

        workspace.Sessions.Count.Should().BeGreaterThan(initialCount);
    }

    private sealed class InMemoryServerRegistrationStore : IServerRegistrationStore
    {
        private readonly List<ServerRegistration> _servers =
        [
            new ServerRegistration
            {
                ServerId = "srv-local",
                DisplayName = "Local",
                Host = "127.0.0.1",
                Port = 5243,
                ApiKey = ""
            }
        ];

        public IReadOnlyList<ServerRegistration> GetAll() => _servers.ToList();

        public ServerRegistration Upsert(ServerRegistration registration)
        {
            var copy = new ServerRegistration
            {
                ServerId = string.IsNullOrWhiteSpace(registration.ServerId) ? Guid.NewGuid().ToString("N") : registration.ServerId,
                DisplayName = registration.DisplayName,
                Host = registration.Host,
                Port = registration.Port,
                ApiKey = registration.ApiKey
            };
            var existing = _servers.FindIndex(x => x.ServerId == copy.ServerId);
            if (existing >= 0)
                _servers[existing] = copy;
            else
                _servers.Add(copy);
            return copy;
        }

        public bool Delete(string serverId)
        {
            return _servers.RemoveAll(x => x.ServerId == serverId) > 0;
        }
    }

    private sealed class StubWorkspaceFactory : IServerWorkspaceFactory
    {
        public ServerWorkspaceLease Create(ServerRegistration registration)
        {
            var context = new CurrentServerContext { Registration = registration };
            var vm = new ServerWorkspaceViewModel(
                context,
                new StubServerCapacityClient(),
                new StubStructuredLogStore(),
                new StubSessionFactory());
            return new ServerWorkspaceLease(new DummyScope(), vm);
        }

        private sealed class DummyScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
            public void Dispose() { }
        }
    }

    private sealed class StubServerCapacityClient : IServerCapacityClient
    {
        public Task<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?> GetCapacityAsync(string host, int port, string? agentId, string? apiKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?>(
                new RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot(true, "", 10, 0, 10, agentId ?? "", 10, 0, 10));
        }

        public Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OpenServerSessionSnapshot>>([new OpenServerSessionSnapshot("sess-1", "process", true)]);

        public Task<bool> TerminateSessionAsync(string host, int port, string sessionId, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AbandonedServerSessionSnapshot>>([new AbandonedServerSessionSnapshot("abandoned-1", "process", "disconnect", DateTimeOffset.UtcNow)]);

        public Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConnectedPeerSnapshot>>([new ConnectedPeerSnapshot("127.0.0.1", 1, false, null, DateTime.UtcNow)]);

        public Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(string host, int port, int limit, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConnectionHistorySnapshot>>([new ConnectionHistorySnapshot(DateTimeOffset.UtcNow, "127.0.0.1", "connection_open", true, null)]);

        public Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BannedPeerSnapshot>>([new BannedPeerSnapshot("192.168.1.22", "test", DateTimeOffset.UtcNow)]);

        public Task<bool> BanPeerAsync(string host, int port, string peer, string? reason, string? apiKey, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> UnbanPeerAsync(string host, int port, string peer, string? apiKey, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AuthUserSnapshot>>([new AuthUserSnapshot("admin", "Admin User", "admin", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)]);

        public Task<IReadOnlyList<string>> GetPermissionRolesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["viewer", "operator", "admin"]);

        public Task<AuthUserSnapshot?> UpsertAuthUserAsync(string host, int port, AuthUserSnapshot user, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<AuthUserSnapshot?>(user);

        public Task<bool> DeleteAuthUserAsync(string host, int port, string userId, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<PluginConfigurationSnapshot?> GetPluginsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<PluginConfigurationSnapshot?>(new PluginConfigurationSnapshot(["plugins/A.dll"], ["process"], true, ""));

        public Task<PluginConfigurationSnapshot?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<PluginConfigurationSnapshot?>(new PluginConfigurationSnapshot(assemblies.ToList(), ["process"], true, "updated"));

        public Task<IReadOnlyList<McpServerDefinition>> ListMcpServersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<McpServerDefinition>>([new McpServerDefinition { ServerId = "mcp-1", DisplayName = "Server One", Transport = "stdio", Command = "npx", Enabled = true }]);

        public Task<McpServerDefinition?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<McpServerDefinition?>(server);

        public Task<bool> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey, CancellationToken cancellationToken = default)
        {
            var response = new GetAgentMcpServersResponse { AgentId = agentId ?? "process" };
            response.ServerIds.Add("mcp-1");
            return Task.FromResult<GetAgentMcpServersResponse?>(response);
        }

        public Task<bool> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<PromptTemplateDefinition>> ListPromptTemplatesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PromptTemplateDefinition>>([new PromptTemplateDefinition { TemplateId = "tmpl-1", DisplayName = "Incident Summary", TemplateContent = "Summarize {{incident_id}}" }]);

        public Task<PromptTemplateDefinition?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<PromptTemplateDefinition?>(template);

        public Task<bool> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source, string? correlationId, string? apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class StubStructuredLogStore : IDesktopStructuredLogStore
    {
        public long GetMaxEventId(string host, int port, string? serverId = null) => 0;

        public IReadOnlyList<DesktopStructuredLogRecord> Query(DesktopStructuredLogFilter? filter = null, int limit = 1000)
            => [];

        public void UpsertBatch(IEnumerable<DesktopStructuredLogRecord> logs)
        {
        }
    }

    private sealed class StubSessionFactory : IDesktopSessionViewModelFactory
    {
        public DesktopSessionViewModel Create(string title, string connectionMode, string agentId)
        {
            return new DesktopSessionViewModel(new FakeAgentSessionClient())
            {
                Title = title,
                ConnectionMode = connectionMode,
                AgentId = agentId,
                SessionId = Guid.NewGuid().ToString("N")[..12]
            };
        }
    }

    private sealed class FakeAgentSessionClient : IAgentSessionClient
    {
        public bool IsConnected { get; private set; }
        public bool CanAcceptInput => IsConnected;
        public string? CurrentSessionId { get; private set; }
        public ServerInfoResponse? ServerInfo => null;
        public string? PerRequestContext { get; set; }
        public event Action? ConnectionStateChanged;
        public event Action<ChatMessage>? MessageReceived;

        public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        {
            IsConnected = true;
            CurrentSessionId = sessionId;
            ConnectionStateChanged?.Invoke();
            return Task.CompletedTask;
        }

        public void Disconnect()
        {
            IsConnected = false;
            ConnectionStateChanged?.Invoke();
        }

        public Task SendInputAsync(string input, CancellationToken cancellationToken = default)
            => SendTextAsync(input, cancellationToken);

        public Task SendTextAsync(string text, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(text))
                MessageReceived?.Invoke(new ChatMessage { IsUser = false, Text = $"echo:{text}" });
            return Task.CompletedTask;
        }

        public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task StopSessionAsync(CancellationToken ct = default)
        {
            Disconnect();
            return Task.CompletedTask;
        }
    }
}
