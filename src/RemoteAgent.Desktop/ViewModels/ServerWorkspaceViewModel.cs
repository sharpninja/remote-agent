using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class ServerWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly CurrentServerContext _serverContext;
    private readonly IServerCapacityClient _serverCapacityClient;
    private readonly IDesktopStructuredLogStore _structuredLogStore;
    private readonly IDesktopSessionViewModelFactory _sessionViewModelFactory;
    private readonly IRequestDispatcher _dispatcher;
    private readonly Dictionary<DesktopSessionViewModel, (Action<RemoteAgent.App.Services.ChatMessage> OnMessage, Action OnConnectionStateChanged)> _sessionEventHandlers = [];
    private CancellationTokenSource? _logMonitorCts;
    private DesktopSessionViewModel? _selectedSession;
    private string _host = "127.0.0.1";
    private string _port = "5243";
    private string _apiKey = "";
    private string _perRequestContext = "";
    private string _selectedConnectionMode = "server";
    private string _selectedAgentId = "process";
    private string _statusText = "Ready.";
    private string _capacitySummary = "Capacity not checked.";
    private OpenServerSessionSnapshot? _selectedOpenServerSession;
    private ConnectedPeerSnapshot? _selectedConnectedPeer;
    private BannedPeerSnapshot? _selectedBannedPeer;
    private AuthUserSnapshot? _selectedAuthUser;
    private string _banReason = "";
    private string _authUserId = "";
    private string _authDisplayName = "";
    private string _authRole = "viewer";
    private bool _authEnabled = true;
    private string _pluginAssembliesText = "";
    private string _pluginStatus = "Plugin configuration not loaded.";
    private McpServerDefinition? _selectedMcpServer;
    private string _mcpServerId = "";
    private string _mcpDisplayName = "";
    private string _mcpTransport = "stdio";
    private string _mcpEndpoint = "";
    private string _mcpCommand = "";
    private string _mcpArguments = "";
    private bool _mcpEnabled = true;
    private string _agentMcpServerIdsText = "";
    private string _mcpStatus = "MCP registry not loaded.";
    private PromptTemplateDefinition? _selectedPromptTemplate;
    private string _promptTemplateId = "";
    private string _promptTemplateName = "";
    private string _promptTemplateDescription = "";
    private string _promptTemplateContent = "";
    private string _promptTemplateStatus = "Prompt templates not loaded.";
    private string _seedSessionId = "";
    private string _seedContextType = "system";
    private string _seedContent = "";
    private string _seedSource = "";
    private string _seedStatus = "";
    private string _logLevelFilter = "";
    private string _logEventTypeFilter = "";
    private string _logSessionIdFilter = "";
    private string _logCorrelationIdFilter = "";
    private string _logComponentFilter = "";
    private string _logServerIdFilter = "";
    private string _logSearchFilter = "";
    private string _logFromUtcFilter = "";
    private string _logToUtcFilter = "";
    private string _logMonitorStatus = "Log monitor stopped.";

    public ServerWorkspaceViewModel(
        CurrentServerContext serverContext,
        IServerCapacityClient serverCapacityClient,
        IDesktopStructuredLogStore structuredLogStore,
        IDesktopSessionViewModelFactory sessionViewModelFactory,
        IRequestDispatcher dispatcher)
    {
        _serverContext = serverContext;
        _serverCapacityClient = serverCapacityClient;
        _structuredLogStore = structuredLogStore;
        _sessionViewModelFactory = sessionViewModelFactory;
        _dispatcher = dispatcher;

        if (_serverContext.Registration != null)
        {
            _host = _serverContext.Registration.Host;
            _port = _serverContext.Registration.Port.ToString();
            _apiKey = _serverContext.Registration.ApiKey ?? "";
            _logServerIdFilter = _serverContext.Registration.ServerId;
        }

        NewSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting new session...", NewSessionAsync));
        CheckCapacityCommand = new RelayCommand(() => _ = RunCommandAsync("Starting capacity check...", CheckCapacityAsync));
        RefreshOpenSessionsCommand = new RelayCommand(() => _ = RunCommandAsync("Starting open sessions refresh...", RefreshOpenSessionsAsync));
        TerminateOpenServerSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting open session termination...", TerminateSelectedOpenServerSessionAsync), () => SelectedOpenServerSession != null);
        TerminateCurrentSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting current session termination...", TerminateCurrentSessionAsync), () => SelectedSession != null);
        TerminateSessionCommand = new RelayCommand<DesktopSessionViewModel>(session => _ = RunCommandAsync("Starting session termination...", () => TerminateSessionAsync(session)), session => session != null);
        SendCurrentMessageCommand = new RelayCommand(() => _ = RunCommandAsync("Starting message send...", SendCurrentMessageAsync), () => SelectedSession != null);
        RefreshSecurityDataCommand = new RelayCommand(() => _ = RunCommandAsync("Starting security refresh...", RefreshSecurityDataAsync));
        BanSelectedPeerCommand = new RelayCommand(() => _ = RunCommandAsync("Starting peer ban...", BanSelectedPeerAsync), () => SelectedConnectedPeer != null);
        UnbanSelectedPeerCommand = new RelayCommand(() => _ = RunCommandAsync("Starting peer unban...", UnbanSelectedPeerAsync), () => SelectedBannedPeer != null);
        RefreshAuthUsersCommand = new RelayCommand(() => _ = RunCommandAsync("Starting auth users refresh...", RefreshAuthUsersAsync));
        SaveAuthUserCommand = new RelayCommand(() => _ = RunCommandAsync("Starting auth user save...", SaveAuthUserAsync));
        DeleteAuthUserCommand = new RelayCommand(() => _ = RunCommandAsync("Starting auth user delete...", DeleteSelectedAuthUserAsync), () => SelectedAuthUser != null);
        RefreshPluginsCommand = new RelayCommand(() => _ = RunCommandAsync("Starting plugins refresh...", RefreshPluginsAsync));
        SavePluginsCommand = new RelayCommand(() => _ = RunCommandAsync("Starting plugins save...", SavePluginsAsync));
        RefreshMcpCommand = new RelayCommand(() => _ = RunCommandAsync("Starting MCP refresh...", RefreshMcpAsync));
        SaveMcpServerCommand = new RelayCommand(() => _ = RunCommandAsync("Starting MCP server save...", SaveMcpServerAsync));
        DeleteMcpServerCommand = new RelayCommand(() => _ = RunCommandAsync("Starting MCP server delete...", DeleteSelectedMcpServerAsync), () => SelectedMcpServer != null);
        SaveAgentMcpMappingCommand = new RelayCommand(() => _ = RunCommandAsync("Starting MCP agent mapping save...", SaveAgentMcpMappingAsync));
        RefreshPromptTemplatesCommand = new RelayCommand(() => _ = RunCommandAsync("Starting prompt templates refresh...", RefreshPromptTemplatesAsync));
        SavePromptTemplateCommand = new RelayCommand(() => _ = RunCommandAsync("Starting prompt template save...", SavePromptTemplateAsync));
        DeletePromptTemplateCommand = new RelayCommand(() => _ = RunCommandAsync("Starting prompt template delete...", DeleteSelectedPromptTemplateAsync), () => SelectedPromptTemplate != null);
        SeedContextCommand = new RelayCommand(() => _ = RunCommandAsync("Starting session context seed...", SeedContextAsync));
        ToggleThemeCommand = new RelayCommand(() => RunCommand("Starting theme toggle...", ToggleTheme));
        StartLogMonitoringCommand = new RelayCommand(() => _ = RunCommandAsync("Starting log monitoring...", StartLogMonitoringAsync));
        StopLogMonitoringCommand = new RelayCommand(() => RunCommand("Starting log monitoring stop...", StopLogMonitoring));
        ApplyLogFilterCommand = new RelayCommand(() => RunCommand("Starting log filter apply...", ReloadStructuredLogs));
        ClearLogFilterCommand = new RelayCommand(() => RunCommand("Starting log filter clear...", ClearLogFilter));

        ObserveBackgroundTask(NewSessionAsync(), "initial session load");
        ObserveBackgroundTask(RefreshOpenSessionsAsync(), "initial open sessions refresh");
        ObserveBackgroundTask(RefreshSecurityDataAsync(), "initial security refresh");
        ObserveBackgroundTask(RefreshAuthUsersAsync(), "initial auth users refresh");
        ObserveBackgroundTask(RefreshPluginsAsync(), "initial plugins refresh");
        ObserveBackgroundTask(RefreshMcpAsync(), "initial MCP refresh");
        ObserveBackgroundTask(RefreshPromptTemplatesAsync(), "initial prompt templates refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentServerLabel =>
        _serverContext.Registration == null
            ? "(unassigned)"
            : $"{_serverContext.Registration.DisplayName} [{_serverContext.Registration.Host}:{_serverContext.Registration.Port}]";

    public string CurrentServerId => _serverContext.Registration?.ServerId ?? "";

    public ObservableCollection<DesktopSessionViewModel> Sessions { get; } = [];
    public IReadOnlyList<string> ConnectionModes { get; } = ["server", "direct"];
    public ObservableCollection<OpenServerSessionSnapshot> OpenServerSessions { get; } = [];
    public ObservableCollection<AbandonedServerSessionSnapshot> AbandonedServerSessions { get; } = [];
    public ObservableCollection<ConnectedPeerSnapshot> ConnectedPeers { get; } = [];
    public ObservableCollection<ConnectionHistorySnapshot> ConnectionHistory { get; } = [];
    public ObservableCollection<BannedPeerSnapshot> BannedPeers { get; } = [];
    public ObservableCollection<AuthUserSnapshot> AuthUsers { get; } = [];
    public ObservableCollection<string> PermissionRoles { get; } = [];
    public ObservableCollection<string> ConfiguredPluginAssemblies { get; } = [];
    public ObservableCollection<string> LoadedPluginRunnerIds { get; } = [];
    public ObservableCollection<McpServerDefinition> McpServers { get; } = [];
    public ObservableCollection<PromptTemplateDefinition> PromptTemplates { get; } = [];
    public ObservableCollection<DesktopStructuredLogRecord> VisibleStructuredLogs { get; } = [];

    public string Host
    {
        get => _host;
        set
        {
            if (_host == value) return;
            _host = value;
            OnPropertyChanged();
        }
    }

    public string Port
    {
        get => _port;
        set
        {
            if (_port == value) return;
            _port = value;
            OnPropertyChanged();
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey == value) return;
            _apiKey = value;
            OnPropertyChanged();
        }
    }

    public string PerRequestContext
    {
        get => _perRequestContext;
        set
        {
            if (_perRequestContext == value) return;
            _perRequestContext = value ?? "";
            OnPropertyChanged();
        }
    }

    public string SelectedConnectionMode
    {
        get => _selectedConnectionMode;
        set
        {
            if (_selectedConnectionMode == value) return;
            _selectedConnectionMode = value;
            OnPropertyChanged();
        }
    }

    public string SelectedAgentId
    {
        get => _selectedAgentId;
        set
        {
            if (_selectedAgentId == value) return;
            _selectedAgentId = value;
            OnPropertyChanged();
        }
    }

    public DesktopSessionViewModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (_selectedSession == value) return;
            _selectedSession = value;
            OnPropertyChanged();
            ((RelayCommand)TerminateCurrentSessionCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SendCurrentMessageCommand).RaiseCanExecuteChanged();
        }
    }

    public OpenServerSessionSnapshot? SelectedOpenServerSession
    {
        get => _selectedOpenServerSession;
        set
        {
            if (_selectedOpenServerSession == value) return;
            _selectedOpenServerSession = value;
            OnPropertyChanged();
            ((RelayCommand)TerminateOpenServerSessionCommand).RaiseCanExecuteChanged();
        }
    }

    public ConnectedPeerSnapshot? SelectedConnectedPeer
    {
        get => _selectedConnectedPeer;
        set
        {
            if (_selectedConnectedPeer == value) return;
            _selectedConnectedPeer = value;
            OnPropertyChanged();
            ((RelayCommand)BanSelectedPeerCommand).RaiseCanExecuteChanged();
        }
    }

    public BannedPeerSnapshot? SelectedBannedPeer
    {
        get => _selectedBannedPeer;
        set
        {
            if (_selectedBannedPeer == value) return;
            _selectedBannedPeer = value;
            OnPropertyChanged();
            ((RelayCommand)UnbanSelectedPeerCommand).RaiseCanExecuteChanged();
        }
    }

    public string BanReason
    {
        get => _banReason;
        set
        {
            if (_banReason == value) return;
            _banReason = value;
            OnPropertyChanged();
        }
    }

    public AuthUserSnapshot? SelectedAuthUser
    {
        get => _selectedAuthUser;
        set
        {
            if (_selectedAuthUser == value) return;
            _selectedAuthUser = value;
            OnPropertyChanged();
            ((RelayCommand)DeleteAuthUserCommand).RaiseCanExecuteChanged();
            if (_selectedAuthUser != null)
            {
                AuthUserId = _selectedAuthUser.UserId;
                AuthDisplayName = _selectedAuthUser.DisplayName;
                AuthRole = string.IsNullOrWhiteSpace(_selectedAuthUser.Role) ? "viewer" : _selectedAuthUser.Role;
                AuthEnabled = _selectedAuthUser.Enabled;
            }
        }
    }

    public string AuthUserId
    {
        get => _authUserId;
        set
        {
            if (_authUserId == value) return;
            _authUserId = value;
            OnPropertyChanged();
        }
    }

    public string AuthDisplayName
    {
        get => _authDisplayName;
        set
        {
            if (_authDisplayName == value) return;
            _authDisplayName = value;
            OnPropertyChanged();
        }
    }

    public string AuthRole
    {
        get => _authRole;
        set
        {
            if (_authRole == value) return;
            _authRole = value;
            OnPropertyChanged();
        }
    }

    public bool AuthEnabled
    {
        get => _authEnabled;
        set
        {
            if (_authEnabled == value) return;
            _authEnabled = value;
            OnPropertyChanged();
        }
    }

    public string PluginAssembliesText
    {
        get => _pluginAssembliesText;
        set
        {
            if (_pluginAssembliesText == value) return;
            _pluginAssembliesText = value;
            OnPropertyChanged();
        }
    }

    public string PluginStatus
    {
        get => _pluginStatus;
        set
        {
            if (_pluginStatus == value) return;
            _pluginStatus = value;
            OnPropertyChanged();
        }
    }

    public McpServerDefinition? SelectedMcpServer
    {
        get => _selectedMcpServer;
        set
        {
            if (_selectedMcpServer == value) return;
            _selectedMcpServer = value;
            OnPropertyChanged();
            ((RelayCommand)DeleteMcpServerCommand).RaiseCanExecuteChanged();
            if (_selectedMcpServer != null)
            {
                McpServerId = _selectedMcpServer.ServerId;
                McpDisplayName = _selectedMcpServer.DisplayName;
                McpTransport = string.IsNullOrWhiteSpace(_selectedMcpServer.Transport) ? "stdio" : _selectedMcpServer.Transport;
                McpEndpoint = _selectedMcpServer.Endpoint;
                McpCommand = _selectedMcpServer.Command;
                McpArguments = string.Join(' ', _selectedMcpServer.Arguments);
                McpEnabled = _selectedMcpServer.Enabled;
            }
        }
    }

    public string McpServerId
    {
        get => _mcpServerId;
        set
        {
            if (_mcpServerId == value) return;
            _mcpServerId = value;
            OnPropertyChanged();
        }
    }

    public string McpDisplayName
    {
        get => _mcpDisplayName;
        set
        {
            if (_mcpDisplayName == value) return;
            _mcpDisplayName = value;
            OnPropertyChanged();
        }
    }

    public string McpTransport
    {
        get => _mcpTransport;
        set
        {
            if (_mcpTransport == value) return;
            _mcpTransport = value;
            OnPropertyChanged();
        }
    }

    public string McpEndpoint
    {
        get => _mcpEndpoint;
        set
        {
            if (_mcpEndpoint == value) return;
            _mcpEndpoint = value;
            OnPropertyChanged();
        }
    }

    public string McpCommand
    {
        get => _mcpCommand;
        set
        {
            if (_mcpCommand == value) return;
            _mcpCommand = value;
            OnPropertyChanged();
        }
    }

    public string McpArguments
    {
        get => _mcpArguments;
        set
        {
            if (_mcpArguments == value) return;
            _mcpArguments = value;
            OnPropertyChanged();
        }
    }

    public bool McpEnabled
    {
        get => _mcpEnabled;
        set
        {
            if (_mcpEnabled == value) return;
            _mcpEnabled = value;
            OnPropertyChanged();
        }
    }

    public string AgentMcpServerIdsText
    {
        get => _agentMcpServerIdsText;
        set
        {
            if (_agentMcpServerIdsText == value) return;
            _agentMcpServerIdsText = value;
            OnPropertyChanged();
        }
    }

    public string McpStatus
    {
        get => _mcpStatus;
        set
        {
            if (_mcpStatus == value) return;
            _mcpStatus = value;
            OnPropertyChanged();
        }
    }

    public PromptTemplateDefinition? SelectedPromptTemplate
    {
        get => _selectedPromptTemplate;
        set
        {
            if (_selectedPromptTemplate == value) return;
            _selectedPromptTemplate = value;
            OnPropertyChanged();
            ((RelayCommand)DeletePromptTemplateCommand).RaiseCanExecuteChanged();
            if (_selectedPromptTemplate != null)
            {
                PromptTemplateId = _selectedPromptTemplate.TemplateId;
                PromptTemplateName = _selectedPromptTemplate.DisplayName;
                PromptTemplateDescription = _selectedPromptTemplate.Description;
                PromptTemplateContent = _selectedPromptTemplate.TemplateContent;
            }
        }
    }

    public string PromptTemplateId
    {
        get => _promptTemplateId;
        set
        {
            if (_promptTemplateId == value) return;
            _promptTemplateId = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateName
    {
        get => _promptTemplateName;
        set
        {
            if (_promptTemplateName == value) return;
            _promptTemplateName = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateDescription
    {
        get => _promptTemplateDescription;
        set
        {
            if (_promptTemplateDescription == value) return;
            _promptTemplateDescription = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateContent
    {
        get => _promptTemplateContent;
        set
        {
            if (_promptTemplateContent == value) return;
            _promptTemplateContent = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateStatus
    {
        get => _promptTemplateStatus;
        set
        {
            if (_promptTemplateStatus == value) return;
            _promptTemplateStatus = value;
            OnPropertyChanged();
        }
    }

    public string SeedSessionId
    {
        get => _seedSessionId;
        set
        {
            if (_seedSessionId == value) return;
            _seedSessionId = value;
            OnPropertyChanged();
        }
    }

    public string SeedContextType
    {
        get => _seedContextType;
        set
        {
            if (_seedContextType == value) return;
            _seedContextType = value;
            OnPropertyChanged();
        }
    }

    public string SeedContent
    {
        get => _seedContent;
        set
        {
            if (_seedContent == value) return;
            _seedContent = value;
            OnPropertyChanged();
        }
    }

    public string SeedSource
    {
        get => _seedSource;
        set
        {
            if (_seedSource == value) return;
            _seedSource = value;
            OnPropertyChanged();
        }
    }

    public string SeedStatus
    {
        get => _seedStatus;
        set
        {
            if (_seedStatus == value) return;
            _seedStatus = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string CapacitySummary
    {
        get => _capacitySummary;
        set
        {
            if (_capacitySummary == value) return;
            _capacitySummary = value;
            OnPropertyChanged();
        }
    }

    public string LogLevelFilter
    {
        get => _logLevelFilter;
        set
        {
            if (_logLevelFilter == value) return;
            _logLevelFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogEventTypeFilter
    {
        get => _logEventTypeFilter;
        set
        {
            if (_logEventTypeFilter == value) return;
            _logEventTypeFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogSessionIdFilter
    {
        get => _logSessionIdFilter;
        set
        {
            if (_logSessionIdFilter == value) return;
            _logSessionIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogCorrelationIdFilter
    {
        get => _logCorrelationIdFilter;
        set
        {
            if (_logCorrelationIdFilter == value) return;
            _logCorrelationIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogComponentFilter
    {
        get => _logComponentFilter;
        set
        {
            if (_logComponentFilter == value) return;
            _logComponentFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogServerIdFilter
    {
        get => _logServerIdFilter;
        set
        {
            if (_logServerIdFilter == value) return;
            _logServerIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogSearchFilter
    {
        get => _logSearchFilter;
        set
        {
            if (_logSearchFilter == value) return;
            _logSearchFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogFromUtcFilter
    {
        get => _logFromUtcFilter;
        set
        {
            if (_logFromUtcFilter == value) return;
            _logFromUtcFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogToUtcFilter
    {
        get => _logToUtcFilter;
        set
        {
            if (_logToUtcFilter == value) return;
            _logToUtcFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogMonitorStatus
    {
        get => _logMonitorStatus;
        set
        {
            if (_logMonitorStatus == value) return;
            _logMonitorStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand NewSessionCommand { get; }
    public ICommand CheckCapacityCommand { get; }
    public ICommand RefreshOpenSessionsCommand { get; }
    public ICommand TerminateOpenServerSessionCommand { get; }
    public ICommand TerminateCurrentSessionCommand { get; }
    public ICommand TerminateSessionCommand { get; }
    public ICommand SendCurrentMessageCommand { get; }
    public ICommand RefreshSecurityDataCommand { get; }
    public ICommand BanSelectedPeerCommand { get; }
    public ICommand UnbanSelectedPeerCommand { get; }
    public ICommand RefreshAuthUsersCommand { get; }
    public ICommand SaveAuthUserCommand { get; }
    public ICommand DeleteAuthUserCommand { get; }
    public ICommand RefreshPluginsCommand { get; }
    public ICommand SavePluginsCommand { get; }
    public ICommand RefreshMcpCommand { get; }
    public ICommand SaveMcpServerCommand { get; }
    public ICommand DeleteMcpServerCommand { get; }
    public ICommand SaveAgentMcpMappingCommand { get; }
    public ICommand RefreshPromptTemplatesCommand { get; }
    public ICommand SavePromptTemplateCommand { get; }
    public ICommand DeletePromptTemplateCommand { get; }
    public ICommand SeedContextCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand StartLogMonitoringCommand { get; }
    public ICommand StopLogMonitoringCommand { get; }
    public ICommand ApplyLogFilterCommand { get; }
    public ICommand ClearLogFilterCommand { get; }

    private async Task NewSessionAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var title = $"Session {Sessions.Count + 1}";
        await _dispatcher.SendAsync(new CreateDesktopSessionRequest(Guid.NewGuid(), title, host, port, SelectedConnectionMode, SelectedAgentId, ApiKey, PerRequestContext, Workspace: this));
    }

    private async Task CheckCapacityAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new CheckSessionCapacityRequest(Guid.NewGuid(), host, port, SelectedAgentId, ApiKey, Workspace: this));
    }

    private async Task RefreshOpenSessionsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new RefreshOpenSessionsRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task RefreshSecurityDataAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new RefreshSecurityDataRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task TerminateSelectedOpenServerSessionAsync()
    {
        var selected = SelectedOpenServerSession;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new TerminateOpenServerSessionRequest(Guid.NewGuid(), host, port, selected.SessionId, ApiKey, Workspace: this));
    }

    private async Task BanSelectedPeerAsync()
    {
        var selected = SelectedConnectedPeer;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new BanPeerRequest(Guid.NewGuid(), host, port, selected.Peer, BanReason, ApiKey, Workspace: this));
    }

    private async Task UnbanSelectedPeerAsync()
    {
        var selected = SelectedBannedPeer;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new UnbanPeerRequest(Guid.NewGuid(), host, port, selected.Peer, ApiKey, Workspace: this));
    }

    private async Task RefreshAuthUsersAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshAuthUsersRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task SaveAuthUserAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var payload = new AuthUserSnapshot(
            UserId: AuthUserId,
            DisplayName: AuthDisplayName,
            Role: string.IsNullOrWhiteSpace(AuthRole) ? "viewer" : AuthRole,
            Enabled: AuthEnabled,
            CreatedUtc: DateTimeOffset.UtcNow,
            UpdatedUtc: DateTimeOffset.UtcNow);
        await _dispatcher.SendAsync(new SaveAuthUserRequest(Guid.NewGuid(), host, port, payload, ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedAuthUserAsync()
    {
        var selected = SelectedAuthUser;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new DeleteAuthUserRequest(Guid.NewGuid(), host, port, selected.UserId, ApiKey, Workspace: this));
    }

    private async Task RefreshPluginsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshPluginsRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task SavePluginsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var assemblies = (PluginAssembliesText ?? "")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _dispatcher.SendAsync(new SavePluginsRequest(Guid.NewGuid(), host, port, assemblies, ApiKey, Workspace: this));
    }

    private async Task RefreshMcpAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshMcpRegistryRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task SaveMcpServerAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var definition = new McpServerDefinition
        {
            ServerId = McpServerId?.Trim() ?? "",
            DisplayName = McpDisplayName ?? "",
            Transport = string.IsNullOrWhiteSpace(McpTransport) ? "stdio" : McpTransport.Trim(),
            Endpoint = McpEndpoint ?? "",
            Command = McpCommand ?? "",
            AuthType = "none",
            Enabled = McpEnabled
        };
        definition.Arguments.AddRange((McpArguments ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        await _dispatcher.SendAsync(new SaveMcpServerRequest(Guid.NewGuid(), host, port, definition, ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedMcpServerAsync()
    {
        var selected = SelectedMcpServer;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new Requests.DeleteMcpServerRequest(Guid.NewGuid(), host, port, selected.ServerId, ApiKey, Workspace: this));
    }

    private async Task SaveAgentMcpMappingAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var ids = (AgentMcpServerIdsText ?? "")
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _dispatcher.SendAsync(new SaveAgentMcpMappingRequest(Guid.NewGuid(), host, port, SelectedAgentId, ids, ApiKey, Workspace: this));
    }

    private async Task RefreshPromptTemplatesAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshPromptTemplatesRequest(Guid.NewGuid(), host, port, ApiKey, Workspace: this));
    }

    private async Task SavePromptTemplateAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var template = new PromptTemplateDefinition
        {
            TemplateId = PromptTemplateId ?? "",
            DisplayName = PromptTemplateName ?? "",
            Description = PromptTemplateDescription ?? "",
            TemplateContent = PromptTemplateContent ?? ""
        };
        await _dispatcher.SendAsync(new SavePromptTemplateRequest(Guid.NewGuid(), host, port, template, ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedPromptTemplateAsync()
    {
        var selected = SelectedPromptTemplate;
        if (selected == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new Requests.DeletePromptTemplateRequest(Guid.NewGuid(), host, port, selected.TemplateId, ApiKey, Workspace: this));
    }

    private async Task SeedContextAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        if (string.IsNullOrWhiteSpace(SeedSessionId)) { SeedStatus = "Seed session id is required."; return; }
        if (string.IsNullOrWhiteSpace(SeedContent)) { SeedStatus = "Seed content is required."; return; }
        await _dispatcher.SendAsync(new Requests.SeedSessionContextRequest(Guid.NewGuid(), host, port, SeedSessionId, SeedContextType, SeedContent, SeedSource, ApiKey, Workspace: this));
    }

    private async Task StartLogMonitoringAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { LogMonitorStatus = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { LogMonitorStatus = "Port must be 1-65535."; return; }

        StopLogMonitoring();
        _logMonitorCts = new CancellationTokenSource();
        var ct = _logMonitorCts.Token;

        var replayFromOffset = _structuredLogStore.GetMaxEventId(host, port, CurrentServerId);
        var result = await _dispatcher.SendAsync(new StartLogMonitoringRequest(Guid.NewGuid(), host, port, ApiKey, CurrentServerId, replayFromOffset, Workspace: this));
        if (!result.Success) { LogMonitorStatus = result.ErrorMessage ?? "Log monitoring failed."; return; }
        var fromOffset = result.Data?.NextOffset ?? replayFromOffset;

        _ = Task.Run(async () =>
        {
            try
            {
                await ServerApiClient.MonitorStructuredLogsAsync(
                    host,
                    port,
                    fromOffset,
                    entry =>
                    {
                        IngestStructuredLogs(host, port, [entry]);
                        var row = ToDesktopStructuredLog(CurrentServerId, _serverContext.Registration?.DisplayName ?? "", host, port, entry);
                        var filter = BuildLogFilter();
                        if (filter.Matches(row))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                VisibleStructuredLogs.Insert(0, row);
                                while (VisibleStructuredLogs.Count > 5000)
                                    VisibleStructuredLogs.RemoveAt(VisibleStructuredLogs.Count - 1);
                            });
                        }

                        return Task.CompletedTask;
                    },
                    ApiKey,
                    ct);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => LogMonitorStatus = $"Log monitor error: {ex.Message}");
            }
        }, ct);
    }

    private void StopLogMonitoring()
    {
        try { _logMonitorCts?.Cancel(); } catch { }
        _logMonitorCts = null;
        LogMonitorStatus = "Log monitor stopped.";
    }

    internal void ReloadStructuredLogs()
    {
        var rows = _structuredLogStore.Query(BuildLogFilter(), 5000);
        VisibleStructuredLogs.Clear();
        foreach (var row in rows)
            VisibleStructuredLogs.Add(row);
        LogMonitorStatus = $"Loaded {VisibleStructuredLogs.Count} log row(s).";
    }

    private void ClearLogFilter()
    {
        LogLevelFilter = "";
        LogEventTypeFilter = "";
        LogSessionIdFilter = "";
        LogCorrelationIdFilter = "";
        LogComponentFilter = "";
        LogServerIdFilter = CurrentServerId;
        LogSearchFilter = "";
        LogFromUtcFilter = "";
        LogToUtcFilter = "";
        ReloadStructuredLogs();
    }

    private DesktopStructuredLogFilter BuildLogFilter()
    {
        return new DesktopStructuredLogFilter
        {
            Level = string.IsNullOrWhiteSpace(LogLevelFilter) ? null : LogLevelFilter.Trim(),
            EventType = string.IsNullOrWhiteSpace(LogEventTypeFilter) ? null : LogEventTypeFilter.Trim(),
            SessionId = string.IsNullOrWhiteSpace(LogSessionIdFilter) ? null : LogSessionIdFilter.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(LogCorrelationIdFilter) ? null : LogCorrelationIdFilter.Trim(),
            Component = string.IsNullOrWhiteSpace(LogComponentFilter) ? null : LogComponentFilter.Trim(),
            ServerId = string.IsNullOrWhiteSpace(LogServerIdFilter) ? CurrentServerId : LogServerIdFilter.Trim(),
            SearchText = string.IsNullOrWhiteSpace(LogSearchFilter) ? null : LogSearchFilter.Trim(),
            FromUtc = TryParseUtc(LogFromUtcFilter),
            ToUtc = TryParseUtc(LogToUtcFilter),
            SourceHost = string.IsNullOrWhiteSpace(Host) ? null : Host.Trim()
        };
    }

    private static DateTimeOffset? TryParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!DateTimeOffset.TryParse(value.Trim(), out var parsed))
            return null;

        return parsed.ToUniversalTime();
    }

    internal void IngestStructuredLogs(string host, int port, IEnumerable<StructuredLogEntry> entries)
    {
        var rows = entries
            .Select(x => ToDesktopStructuredLog(CurrentServerId, _serverContext.Registration?.DisplayName ?? "", host, port, x))
            .ToList();
        if (rows.Count == 0) return;
        _structuredLogStore.UpsertBatch(rows);
    }

    private static DesktopStructuredLogRecord ToDesktopStructuredLog(string serverId, string serverDisplayName, string host, int port, StructuredLogEntry row)
    {
        DateTimeOffset parsed;
        if (!DateTimeOffset.TryParse(row.TimestampUtc, out parsed))
            parsed = DateTimeOffset.UtcNow;
        return new DesktopStructuredLogRecord
        {
            ServerId = serverId ?? "",
            ServerDisplayName = serverDisplayName ?? "",
            EventId = row.EventId,
            TimestampUtc = parsed,
            Level = row.Level ?? "",
            EventType = row.EventType ?? "",
            Message = row.Message ?? "",
            Component = row.Component ?? "",
            SessionId = string.IsNullOrWhiteSpace(row.SessionId) ? null : row.SessionId,
            CorrelationId = string.IsNullOrWhiteSpace(row.CorrelationId) ? null : row.CorrelationId,
            DetailsJson = string.IsNullOrWhiteSpace(row.DetailsJson) ? null : row.DetailsJson,
            SourceHost = host,
            SourcePort = port
        };
    }

    private async Task ConnectSessionAsync(DesktopSessionViewModel session)
    {
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var host = string.Equals(session.ConnectionMode, "direct", StringComparison.OrdinalIgnoreCase)
            ? "127.0.0.1"
            : (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!_sessionEventHandlers.ContainsKey(session))
        {
            Action<RemoteAgent.App.Services.ChatMessage> onMessage = message =>
            {
                var text = message.IsEvent
                    ? $"event: {message.EventMessage}"
                    : message.IsError
                        ? $"error: {message.Text}"
                        : message.Text;
                Dispatcher.UIThread.Post(() =>
                {
                    session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] agent: {text}");
                    session.IsConnected = session.SessionClient.IsConnected;
                });
            };
            Action onConnectionStateChanged = () =>
                Dispatcher.UIThread.Post(() => session.IsConnected = session.SessionClient.IsConnected);
            _sessionEventHandlers[session] = (onMessage, onConnectionStateChanged);
            session.SessionClient.MessageReceived += onMessage;
            session.SessionClient.ConnectionStateChanged += onConnectionStateChanged;
        }

        session.SessionClient.PerRequestContext = (PerRequestContext ?? "").Trim();
        try
        {
            await session.SessionClient.ConnectAsync(
                host,
                port,
                session.SessionId,
                session.AgentId,
                apiKey: ApiKey);
            session.IsConnected = true;
            session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] connected to {host}:{port}.");
            StatusText = $"Connected {session.Title} ({session.ConnectionMode}).";
        }
        catch (Exception ex)
        {
            session.IsConnected = false;
            session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] connection failed: {ex.Message}");
            StatusText = $"Failed to connect {session.Title}: {ex.Message}";
        }
    }

    private async Task TerminateCurrentSessionAsync()
    {
        if (SelectedSession == null)
            return;

        await _dispatcher.SendAsync(new TerminateDesktopSessionRequest(Guid.NewGuid(), SelectedSession, Workspace: this));
    }

    private async Task SendCurrentMessageAsync()
    {
        var session = SelectedSession;
        if (session == null) return;
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new SendDesktopMessageRequest(Guid.NewGuid(), session, host, port, ApiKey, PerRequestContext));
    }

    private async Task TerminateSessionAsync(DesktopSessionViewModel? session)
    {
        if (session == null) return;
        await _dispatcher.SendAsync(new TerminateDesktopSessionRequest(Guid.NewGuid(), session, Workspace: this));
    }

    private static void ToggleTheme()
    {
        if (Application.Current == null)
            return;

        var current = Application.Current.RequestedThemeVariant;
        Application.Current.RequestedThemeVariant = current == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RunCommand(string startMessage, Action action)
    {
        StatusText = startMessage;
        try
        {
            var before = StatusText;
            action();
            if (StatusText == before)
                StatusText = "Command completed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Command failed: {ex.Message}";
        }
    }

    private async Task RunCommandAsync(string startMessage, Func<Task> action)
    {
        StatusText = startMessage;
        try
        {
            var before = StatusText;
            await action();
            if (StatusText == before)
                StatusText = "Command completed.";
        }
        catch (Exception ex)
        {
            StatusText = $"Command failed: {ex.Message}";
        }
    }

    private void ObserveBackgroundTask(Task task, string operation)
    {
        _ = task.ContinueWith(
            completed =>
            {
                if (completed.IsCanceled || completed.Exception == null)
                    return;

                var error = completed.Exception.GetBaseException().Message;
                Dispatcher.UIThread.Post(() => StatusText = $"{operation} failed: {error}");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
