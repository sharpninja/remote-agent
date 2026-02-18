using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using FluentAssertions;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Services;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.UiTests.TestHelpers;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests;

/// <summary>
/// Desktop UI coverage for FR-12.x and TR-8.6/TR-14.x using Avalonia headless tests.
/// </summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-2.6")]
[Trait("Requirement", "FR-12.1")]
[Trait("Requirement", "FR-12.1.3")]
[Trait("Requirement", "FR-12.1.4")]
[Trait("Requirement", "FR-12.1.5")]
[Trait("Requirement", "FR-12.4")]
[Trait("Requirement", "FR-12.7")]
[Trait("Requirement", "FR-12.8")]
[Trait("Requirement", "FR-12.9")]
[Trait("Requirement", "FR-12.10")]
[Trait("Requirement", "FR-12.11")]
[Trait("Requirement", "FR-13.1")]
[Trait("Requirement", "FR-13.2")]
[Trait("Requirement", "FR-13.4")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "FR-13.6")]
[Trait("Requirement", "FR-14.1")]
[Trait("Requirement", "TR-5.7")]
[Trait("Requirement", "TR-8.6")]
[Trait("Requirement", "TR-14.1")]
[Trait("Requirement", "TR-14.1.7")]
public sealed class MainWindowUiTests
{
    [AvaloniaFact]
    public void MainWindow_ShouldExposeExpectedManagementControls()
    {
        // FR-12.1.3, FR-12.1.4, FR-12.1.5, FR-12.8, FR-12.11
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        var window = new MainWindow(vm);

        window.FindControl<ComboBox>("ServerSelectorComboBox").Should().NotBeNull();
        window.FindControl<Button>("NewServerButton").Should().NotBeNull();
        window.FindControl<Button>("SaveServerButton").Should().NotBeNull();
        window.FindControl<Button>("RemoveServerButton").Should().NotBeNull();
        window.FindControl<TextBox>("ServerHostTextBox").Should().NotBeNull();

        window.FindControl<Button>("NewSessionButton").Should().NotBeNull();
        window.FindControl<Button>("TerminateSessionButton").Should().NotBeNull();
        window.FindControl<Button>("RefreshOpenSessionsButton").Should().NotBeNull();
        window.FindControl<Button>("TerminateOpenServerSessionButton").Should().NotBeNull();
        window.FindControl<Button>("CheckLocalServerButton").Should().NotBeNull();
        window.FindControl<Button>("ApplyLocalServerActionButton").Should().NotBeNull();
        window.FindControl<Button>("StartLogMonitoringButton").Should().NotBeNull();
        window.FindControl<Button>("ApplyLogFilterButton").Should().NotBeNull();
        window.FindControl<TextBox>("LogServerIdFilterTextBox").Should().NotBeNull();
        window.FindControl<TabControl>("SessionTabs").Should().NotBeNull();
        window.FindControl<NavigationView>("ManagementNavigationView").Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public async Task CurrentServerWorkspace_ShouldCreateSessionTabs()
    {
        // FR-12.1.3: tabbed session interface
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);

        vm.CurrentServerViewModel.Should().NotBeNull();
        var workspace = vm.CurrentServerViewModel!;

        await WaitForAsync(() => workspace.Sessions.Count > 0);

        workspace.Sessions.Count.Should().BeGreaterThan(0);
    }

    [AvaloniaFact]
    public void MainWindow_ShouldExposeSecurityHistoryAndAuthPanels()
    {
        // FR-12.7, FR-13.1, FR-13.2, FR-13.4, FR-13.5
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        var window = new MainWindow(vm);

        window.FindControl<Button>("RefreshSecurityButton").Should().NotBeNull();
        window.FindControl<Button>("BanPeerButton").Should().NotBeNull();
        window.FindControl<Button>("UnbanPeerButton").Should().NotBeNull();
        window.FindControl<ListBox>("ConnectedPeersList").Should().NotBeNull();
        window.FindControl<ListBox>("BannedPeersList").Should().NotBeNull();
        window.FindControl<ListBox>("ConnectionHistoryList").Should().NotBeNull();
        window.FindControl<ListBox>("AbandonedSessionsList").Should().NotBeNull();
        window.FindControl<Button>("RefreshAuthUsersButton").Should().NotBeNull();
        window.FindControl<Button>("SaveAuthUserButton").Should().NotBeNull();
        window.FindControl<Button>("DeleteAuthUserButton").Should().NotBeNull();
        window.FindControl<ListBox>("AuthUsersList").Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void MainWindow_ShouldExposePluginMcpAndPromptTemplatePanels()
    {
        // FR-12.4, FR-12.6, FR-13.6, FR-14.1
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        var window = new MainWindow(vm);

        window.FindControl<Button>("RefreshPluginsButton").Should().NotBeNull();
        window.FindControl<Button>("SavePluginsButton").Should().NotBeNull();
        window.FindControl<ListBox>("LoadedPluginRunnerIdsList").Should().NotBeNull();
        window.FindControl<Button>("RefreshMcpButton").Should().NotBeNull();
        window.FindControl<Button>("SaveMcpServerButton").Should().NotBeNull();
        window.FindControl<Button>("DeleteMcpServerButton").Should().NotBeNull();
        window.FindControl<Button>("SaveAgentMcpMappingButton").Should().NotBeNull();
        window.FindControl<ListBox>("McpServersList").Should().NotBeNull();
        window.FindControl<Button>("RefreshPromptTemplatesButton").Should().NotBeNull();
        window.FindControl<Button>("SavePromptTemplateButton").Should().NotBeNull();
        window.FindControl<Button>("DeletePromptTemplateButton").Should().NotBeNull();
        window.FindControl<Button>("SeedContextButton").Should().NotBeNull();
        window.FindControl<ListBox>("PromptTemplatesList").Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void SessionMessageInput_ShouldExposeCtrlEnterShortcut()
    {
        // FR-2.6, TR-5.7: desktop Ctrl+Enter submits request
        var axamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RemoteAgent.Desktop", "Views", "MainWindow.axaml");
        var content = File.ReadAllText(axamlPath);

        content.Should().Contain("SessionMessageInput");
        content.Should().Contain("Gesture=\"Ctrl+Enter\"");
        content.Should().Contain("SendCurrentMessageCommand");
    }

    [AvaloniaFact]
    public void LocalServerNavigationSection_ShouldExposeDedicatedStatusArea()
    {
        // FR: management UI provides dedicated local-server status and controls in right-side navigation.
        var axamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RemoteAgent.Desktop", "Views", "MainWindow.axaml");
        var content = File.ReadAllText(axamlPath);

        content.Should().Contain("ManagementNavigationView");
        content.Should().Contain("NavigationViewItem");
        content.Should().Contain("Tag=\"LocalServer\"");
        content.Should().Contain("LocalServerStatusTextBlock");
        content.Should().Contain("LocalServerStatusText");
        content.Should().Contain("CheckLocalServerCommand");
        content.Should().Contain("ApplyLocalServerActionCommand");
    }

    [AvaloniaFact]
    public async Task SendCurrentMessage_ShouldPropagatePerRequestContext()
    {
        // FR-12.8, TR-14.1.7: per-request context is attached to outbound messages.
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        var workspace = vm.CurrentServerViewModel;
        workspace.Should().NotBeNull();

        await WaitForAsync(() => workspace!.Sessions.Count > 0);
        var session = workspace!.SelectedSession ?? workspace.Sessions[0];
        workspace.SelectedSession = session;
        workspace.PerRequestContext = "ticket=ABC-123";
        session.PendingMessage = "hello";

        workspace.SendCurrentMessageCommand.Execute(null);
        await WaitForAsync(() => ((FakeAgentSessionClient)session.SessionClient).LastSentText == "hello");

        var client = (FakeAgentSessionClient)session.SessionClient;
        client.LastSentText.Should().Be("hello");
        client.LastPerRequestContextAtSend.Should().Be("ticket=ABC-123");
    }

    [AvaloniaFact]
    public void ConnectionDialog_ShouldExposePerRequestContextEditor()
    {
        // FR-12.1.2, FR-12.8: connection/session defaults are prompted in a dialog when starting a session.
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        var workspace = vm.CurrentServerViewModel;
        workspace.Should().NotBeNull();

        var dialogVm = new ConnectionSettingsDialogViewModel(
            new ConnectionSettingsDefaults(
                workspace!.Host, workspace.Port, workspace.SelectedConnectionMode,
                workspace.SelectedAgentId, workspace.ApiKey, workspace.PerRequestContext,
                workspace.ConnectionModes));
        var dialog = new ConnectionSettingsDialog(dialogVm);

        // Verify all expected editor controls are present in the logical tree
        var textBoxes = dialog.GetLogicalDescendants().OfType<TextBox>().ToList();
        textBoxes.Should().HaveCountGreaterThan(4, "dialog requires Host, Port, Agent, ApiKey, and PerRequestContext editors");
        var comboBoxes = dialog.GetLogicalDescendants().OfType<ComboBox>().ToList();
        comboBoxes.Should().HaveCountGreaterThan(0, "dialog requires a Mode selector");

        dialog.Close();
    }

    [AvaloniaFact]
    public async Task MainWindow_ShouldSupportSwitchingRegisteredServers()
    {
        // FR-12.9, FR-12.10: multiple server registrations and server-scoped workspace switching.
        var store = new InMemoryServerRegistrationStore();
        store.Upsert(new ServerRegistration
        {
            ServerId = "srv-2",
            DisplayName = "Secondary",
            Host = "10.0.0.2",
            Port = 5243,
            ApiKey = ""
        });

        var vm = new MainWindowViewModel(store, new StubWorkspaceFactory(), new StubLocalServerManager(false), _nullDispatcher, _nullAppLog);
        vm.Servers.Count.Should().BeGreaterThanOrEqualTo(2);

        vm.SelectedServer = vm.Servers.First(x => x.ServerId == "srv-2");
        await WaitForAsync(() => vm.CurrentServerId == "srv-2");

        vm.CurrentServerId.Should().Be("srv-2");
        vm.CurrentServerViewModel.Should().NotBeNull();
        vm.CurrentServerViewModel!.CurrentServerId.Should().Be("srv-2");
    }

    [AvaloniaFact]
    public async Task CheckLocalServer_WhenStopped_ShouldOfferStart_AndApplyStarts()
    {
        var localServer = new StubLocalServerManager(false);
        var dispatcher = CreateDispatcher(localServer);
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), localServer, dispatcher, _nullAppLog);

        vm.CheckLocalServerCommand.Execute(null);
        await WaitForAsync(() => vm.LocalServerActionLabel == "Start Local Server");
        vm.CanApplyLocalServerAction.Should().BeTrue();

        vm.ApplyLocalServerActionCommand.Execute(null);
        await WaitForAsync(() => localServer.LastAction == "start");
        vm.LocalServerActionLabel.Should().Be("Stop Local Server");
    }

    [AvaloniaFact]
    public async Task CheckLocalServer_WhenRunning_ShouldOfferStop_AndApplyStops()
    {
        var localServer = new StubLocalServerManager(true);
        var dispatcher = CreateDispatcher(localServer);
        var vm = new MainWindowViewModel(new InMemoryServerRegistrationStore(), new StubWorkspaceFactory(), localServer, dispatcher, _nullAppLog);

        vm.CheckLocalServerCommand.Execute(null);
        await WaitForAsync(() => vm.LocalServerActionLabel.StartsWith("Stop", StringComparison.Ordinal));
        vm.CanApplyLocalServerAction.Should().BeTrue();

        vm.ApplyLocalServerActionCommand.Execute(null);
        await WaitForAsync(() => localServer.LastAction == "stop");
        vm.LocalServerActionLabel.Should().Be("Start Local Server");
    }

    [AvaloniaFact]
    public async Task CheckCapacityCommand_WhenCapacityEndpointFails_ShouldSurfaceDetailedFailure()
    {
        var failingClient = new StubServerCapacityClient
        {
            GetCapacityException = new InvalidOperationException("Capacity check failed (400 Bad Request): capacity endpoint unavailable or unauthorized.")
        };
        var vm = new MainWindowViewModel(
            new InMemoryServerRegistrationStore(),
            new StubWorkspaceFactory(() => failingClient),
            new StubLocalServerManager(false),
            _nullDispatcher,
            _nullAppLog);
        var workspace = vm.CurrentServerViewModel!;

        workspace.CheckCapacityCommand.Execute(null);
        await WaitForAsync(() => workspace.StatusText.Contains("Capacity check failed (400 Bad Request)", StringComparison.Ordinal));

        workspace.StatusText.Should().Contain("Capacity check failed (400 Bad Request)");
    }

    [AvaloniaFact]
    public async Task RefreshPluginsCommand_WhenPluginsEndpointFails_ShouldSurfaceDetailedFailure()
    {
        var failingClient = new StubServerCapacityClient
        {
            GetPluginsException = new InvalidOperationException("Get plugins failed (PermissionDenied): invalid credentials.")
        };
        var vm = new MainWindowViewModel(
            new InMemoryServerRegistrationStore(),
            new StubWorkspaceFactory(() => failingClient),
            new StubLocalServerManager(false),
            _nullDispatcher,
            _nullAppLog);
        var workspace = vm.CurrentServerViewModel!;

        workspace.RefreshPluginsCommand.Execute(null);
        await WaitForAsync(() => workspace.StatusText.Contains("Command failed:", StringComparison.Ordinal));

        workspace.StatusText.Should().Contain("Get plugins failed (PermissionDenied)");
    }

    private sealed class InMemoryServerRegistrationStore : IServerRegistrationStore
    {
        private readonly TestHelpers.InMemoryServerRegistrationStore _inner = new();

        public IReadOnlyList<ServerRegistration> GetAll() => _inner.GetAll();
        public ServerRegistration Upsert(ServerRegistration registration) => _inner.Upsert(registration);
        public bool Delete(string serverId) => _inner.Delete(serverId);
    }

    private sealed class StubWorkspaceFactory(Func<IServerCapacityClient>? capacityClientFactory = null) : IServerWorkspaceFactory
    {
        public ServerWorkspaceLease Create(ServerRegistration registration)
        {
            var client = capacityClientFactory?.Invoke() ?? new StubServerCapacityClient();
            var logStore = new StubStructuredLogStore();
            var sessionFactory = new StubSessionFactory();
            var context = new CurrentServerContext { Registration = registration };
            var dispatcher = new TestRequestDispatcher()
                .Register(new CheckSessionCapacityHandler(client))
                .Register(new RefreshOpenSessionsHandler(client))
                .Register(new TerminateOpenServerSessionHandler(client))
                .Register(new RefreshSecurityDataHandler(client))
                .Register(new BanPeerHandler(client))
                .Register(new UnbanPeerHandler(client))
                .Register(new RefreshAuthUsersHandler(client))
                .Register(new SaveAuthUserHandler(client))
                .Register(new DeleteAuthUserHandler(client))
                .Register(new RefreshPluginsHandler(client))
                .Register(new SavePluginsHandler(client))
                .Register(new RefreshMcpRegistryHandler(client))
                .Register(new SaveMcpServerHandler(client))
                .Register(new DeleteMcpServerHandler(client))
                .Register(new SaveAgentMcpMappingHandler(client))
                .Register(new RefreshPromptTemplatesHandler(client))
                .Register(new SavePromptTemplateHandler(client))
                .Register(new DeletePromptTemplateHandler(client))
                .Register(new SeedSessionContextHandler(client))
                .Register(new CreateDesktopSessionHandler(client, sessionFactory))
                .Register(new TerminateDesktopSessionHandler())
                .Register(new SendDesktopMessageHandler());
            var vm = new ServerWorkspaceViewModel(context, client, logStore, sessionFactory, dispatcher);
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
        public Exception? GetCapacityException { get; init; }
        public Exception? GetPluginsException { get; init; }

        public Task<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?> GetCapacityAsync(string host, int port, string? agentId, string? apiKey, CancellationToken cancellationToken = default)
        {
            if (GetCapacityException != null)
                return Task.FromException<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?>(GetCapacityException);

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
        {
            if (GetPluginsException != null)
                return Task.FromException<PluginConfigurationSnapshot?>(GetPluginsException);
            return Task.FromResult<PluginConfigurationSnapshot?>(new PluginConfigurationSnapshot(["plugins/A.dll"], ["process"], true, ""));
        }

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
        public string? LastSentText { get; private set; }
        public string? LastPerRequestContextAtSend { get; private set; }
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
            LastSentText = text;
            LastPerRequestContextAtSend = PerRequestContext;
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

    private sealed class StubLocalServerManager(bool running) : ILocalServerManager
    {
        private bool _running = running;
        public string LastAction { get; private set; } = "";

        public Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        {
            if (_running)
            {
                return Task.FromResult(new LocalServerProbeResult(
                    IsRunning: true,
                    IsManagedByApp: true,
                    CanApplyAction: true,
                    RecommendedActionLabel: "Stop Local Server",
                    Message: "Local server is running (managed by desktop app)."));
            }

            return Task.FromResult(new LocalServerProbeResult(
                IsRunning: false,
                IsManagedByApp: false,
                CanApplyAction: true,
                RecommendedActionLabel: "Start Local Server",
                Message: "Local server is not running."));
        }

        public Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default)
        {
            _running = true;
            LastAction = "start";
            return Task.FromResult(new LocalServerActionResult(true, "Local server started."));
        }

        public Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default)
        {
            _running = false;
            LastAction = "stop";
            return Task.FromResult(new LocalServerActionResult(true, "Local server stopped."));
        }
    }

    private sealed class NullRequestDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(TResponse)!);
        }
    }

    private sealed class NullFileSaveDialogService : IFileSaveDialogService
    {
        public Task<string?> GetSaveFilePathAsync(string suggestedName, string extension, string filterDescription)
            => Task.FromResult<string?>(null);
    }

    private static readonly IRequestDispatcher _nullDispatcher = new NullRequestDispatcher();
    private static readonly AppLogViewModel _nullAppLog = new(new NullRequestDispatcher(), new NullFileSaveDialogService());

    private static IRequestDispatcher CreateDispatcher(ILocalServerManager localServerManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(localServerManager);
        services.AddTransient<IRequestHandler<CheckLocalServerRequest, CommandResult<LocalServerProbeResult>>, CheckLocalServerHandler>();
        services.AddTransient<IRequestHandler<ApplyLocalServerActionRequest, CommandResult<LocalServerProbeResult>>, ApplyLocalServerActionHandler>();
        services.AddTransient<IRequestHandler<SetManagementSectionRequest, Unit>, SetManagementSectionHandler>();
        services.AddTransient<IRequestHandler<ExpandStatusLogPanelRequest, Unit>, ExpandStatusLogPanelHandler>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        return new ServiceProviderRequestDispatcher(sp, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceProviderRequestDispatcher>>());
    }

    private static async Task WaitForAsync(Func<bool> condition, int attempts = 50, int delayMs = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (condition())
                return;
            await Task.Delay(delayMs);
        }

        condition().Should().BeTrue();
    }
}
