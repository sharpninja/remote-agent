using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using RemoteAgent.App.Logic;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class ServerWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly CurrentServerContext _serverContext;
    private readonly IServerCapacityClient _serverCapacityClient;
    private readonly IDesktopStructuredLogStore _structuredLogStore;
    private readonly IDesktopSessionViewModelFactory _sessionViewModelFactory;
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
        IDesktopSessionViewModelFactory sessionViewModelFactory)
    {
        _serverContext = serverContext;
        _serverCapacityClient = serverCapacityClient;
        _structuredLogStore = structuredLogStore;
        _sessionViewModelFactory = sessionViewModelFactory;

        if (_serverContext.Registration != null)
        {
            _host = _serverContext.Registration.Host;
            _port = _serverContext.Registration.Port.ToString();
            _apiKey = _serverContext.Registration.ApiKey ?? "";
            _logServerIdFilter = _serverContext.Registration.ServerId;
        }

        NewSessionCommand = new RelayCommand(() => _ = NewSessionAsync());
        CheckCapacityCommand = new RelayCommand(() => _ = CheckCapacityAsync());
        RefreshOpenSessionsCommand = new RelayCommand(() => _ = RefreshOpenSessionsAsync());
        TerminateOpenServerSessionCommand = new RelayCommand(() => _ = TerminateSelectedOpenServerSessionAsync(), () => SelectedOpenServerSession != null);
        TerminateCurrentSessionCommand = new RelayCommand(() => _ = TerminateCurrentSessionAsync(), () => SelectedSession != null);
        TerminateSessionCommand = new RelayCommand<DesktopSessionViewModel>(session => _ = TerminateSessionAsync(session), session => session != null);
        SendCurrentMessageCommand = new RelayCommand(() => _ = SendCurrentMessageAsync(), () => SelectedSession != null);
        RefreshSecurityDataCommand = new RelayCommand(() => _ = RefreshSecurityDataAsync());
        BanSelectedPeerCommand = new RelayCommand(() => _ = BanSelectedPeerAsync(), () => SelectedConnectedPeer != null);
        UnbanSelectedPeerCommand = new RelayCommand(() => _ = UnbanSelectedPeerAsync(), () => SelectedBannedPeer != null);
        RefreshAuthUsersCommand = new RelayCommand(() => _ = RefreshAuthUsersAsync());
        SaveAuthUserCommand = new RelayCommand(() => _ = SaveAuthUserAsync());
        DeleteAuthUserCommand = new RelayCommand(() => _ = DeleteSelectedAuthUserAsync(), () => SelectedAuthUser != null);
        RefreshPluginsCommand = new RelayCommand(() => _ = RefreshPluginsAsync());
        SavePluginsCommand = new RelayCommand(() => _ = SavePluginsAsync());
        RefreshMcpCommand = new RelayCommand(() => _ = RefreshMcpAsync());
        SaveMcpServerCommand = new RelayCommand(() => _ = SaveMcpServerAsync());
        DeleteMcpServerCommand = new RelayCommand(() => _ = DeleteSelectedMcpServerAsync(), () => SelectedMcpServer != null);
        SaveAgentMcpMappingCommand = new RelayCommand(() => _ = SaveAgentMcpMappingAsync());
        RefreshPromptTemplatesCommand = new RelayCommand(() => _ = RefreshPromptTemplatesAsync());
        SavePromptTemplateCommand = new RelayCommand(() => _ = SavePromptTemplateAsync());
        DeletePromptTemplateCommand = new RelayCommand(() => _ = DeleteSelectedPromptTemplateAsync(), () => SelectedPromptTemplate != null);
        SeedContextCommand = new RelayCommand(() => _ = SeedContextAsync());
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        StartLogMonitoringCommand = new RelayCommand(() => _ = StartLogMonitoringAsync());
        StopLogMonitoringCommand = new RelayCommand(StopLogMonitoring);
        ApplyLogFilterCommand = new RelayCommand(ReloadStructuredLogs);
        ClearLogFilterCommand = new RelayCommand(ClearLogFilter);

        _ = NewSessionAsync();
        _ = RefreshOpenSessionsAsync();
        _ = RefreshSecurityDataAsync();
        _ = RefreshAuthUsersAsync();
        _ = RefreshPluginsAsync();
        _ = RefreshMcpAsync();
        _ = RefreshPromptTemplatesAsync();
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
        if (string.Equals(SelectedConnectionMode, "server", StringComparison.OrdinalIgnoreCase))
        {
            var capacity = await CheckCapacityAsync();
            if (capacity == null || !capacity.CanCreateSession)
            {
                StatusText = capacity?.Reason ?? "Could not verify session capacity.";
                return;
            }
        }

        var session = _sessionViewModelFactory.Create(
            $"Session {Sessions.Count + 1}",
            SelectedConnectionMode,
            SelectedAgentId);
        session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] session initialized ({session.ConnectionMode}).");

        Sessions.Add(session);
        SelectedSession = session;
        StatusText = $"Created {session.Title}. Connecting...";
        await ConnectSessionAsync(session);
    }

    private async Task<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?> CheckCapacityAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return null;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return null;
        }

        var snapshot = await _serverCapacityClient.GetCapacityAsync(
            host,
            port,
            SelectedAgentId,
            ApiKey);

        if (snapshot == null)
        {
            CapacitySummary = "Capacity check failed.";
            StatusText = "Capacity endpoint unavailable or unauthorized.";
            return null;
        }

        CapacitySummary =
            $"Server {snapshot.ActiveSessionCount}/{snapshot.MaxConcurrentSessions} active, remaining {snapshot.RemainingServerCapacity}; " +
            $"Agent {snapshot.AgentActiveSessionCount}/{snapshot.AgentMaxConcurrentSessions?.ToString() ?? "-"}.";
        StatusText = snapshot.CanCreateSession ? "Capacity available." : snapshot.Reason;
        return snapshot;
    }

    private async Task RefreshOpenSessionsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var sessions = await _serverCapacityClient.GetOpenSessionsAsync(host, port, ApiKey);
        OpenServerSessions.Clear();
        foreach (var session in sessions)
            OpenServerSessions.Add(session);

        SelectedOpenServerSession = OpenServerSessions.FirstOrDefault();
        StatusText = $"Loaded {OpenServerSessions.Count} open server session(s).";
    }

    private async Task RefreshSecurityDataAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var abandoned = await _serverCapacityClient.GetAbandonedSessionsAsync(host, port, ApiKey);
        AbandonedServerSessions.Clear();
        foreach (var row in abandoned)
            AbandonedServerSessions.Add(row);

        var peers = await _serverCapacityClient.GetConnectedPeersAsync(host, port, ApiKey);
        ConnectedPeers.Clear();
        foreach (var peer in peers)
            ConnectedPeers.Add(peer);
        SelectedConnectedPeer = ConnectedPeers.FirstOrDefault();

        var history = await _serverCapacityClient.GetConnectionHistoryAsync(host, port, 500, ApiKey);
        ConnectionHistory.Clear();
        foreach (var row in history)
            ConnectionHistory.Add(row);

        var banned = await _serverCapacityClient.GetBannedPeersAsync(host, port, ApiKey);
        BannedPeers.Clear();
        foreach (var row in banned)
            BannedPeers.Add(row);
        SelectedBannedPeer = BannedPeers.FirstOrDefault();

        StatusText = $"Security data refreshed ({ConnectedPeers.Count} peer(s), {BannedPeers.Count} banned).";
    }

    private async Task TerminateSelectedOpenServerSessionAsync()
    {
        var selected = SelectedOpenServerSession;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var terminated = await _serverCapacityClient.TerminateSessionAsync(host, port, selected.SessionId, ApiKey);
        if (!terminated)
        {
            StatusText = $"Failed to terminate server session {selected.SessionId}.";
            return;
        }

        StatusText = $"Terminated server session {selected.SessionId}.";
        await RefreshOpenSessionsAsync();
    }

    private async Task BanSelectedPeerAsync()
    {
        var selected = SelectedConnectedPeer;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ok = await _serverCapacityClient.BanPeerAsync(host, port, selected.Peer, BanReason, ApiKey);
        StatusText = ok ? $"Peer banned: {selected.Peer}" : $"Failed to ban peer: {selected.Peer}";
        await RefreshSecurityDataAsync();
    }

    private async Task UnbanSelectedPeerAsync()
    {
        var selected = SelectedBannedPeer;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ok = await _serverCapacityClient.UnbanPeerAsync(host, port, selected.Peer, ApiKey);
        StatusText = ok ? $"Peer unbanned: {selected.Peer}" : $"Failed to unban peer: {selected.Peer}";
        await RefreshSecurityDataAsync();
    }

    private async Task RefreshAuthUsersAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
            return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
            return;

        var roles = await _serverCapacityClient.GetPermissionRolesAsync(host, port, ApiKey);
        PermissionRoles.Clear();
        foreach (var role in roles)
            PermissionRoles.Add(role);
        if (PermissionRoles.Count == 0)
            PermissionRoles.Add("viewer");

        if (!PermissionRoles.Contains(AuthRole, StringComparer.OrdinalIgnoreCase))
            AuthRole = PermissionRoles.First();

        var users = await _serverCapacityClient.GetAuthUsersAsync(host, port, ApiKey);
        AuthUsers.Clear();
        foreach (var user in users)
            AuthUsers.Add(user);
        SelectedAuthUser = AuthUsers.FirstOrDefault();
    }

    private async Task SaveAuthUserAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var payload = new AuthUserSnapshot(
            UserId: AuthUserId,
            DisplayName: AuthDisplayName,
            Role: string.IsNullOrWhiteSpace(AuthRole) ? "viewer" : AuthRole,
            Enabled: AuthEnabled,
            CreatedUtc: DateTimeOffset.UtcNow,
            UpdatedUtc: DateTimeOffset.UtcNow);
        var saved = await _serverCapacityClient.UpsertAuthUserAsync(host, port, payload, ApiKey);
        if (saved == null)
        {
            StatusText = "Failed to save auth user.";
            return;
        }

        StatusText = $"Saved auth user {saved.UserId} ({saved.Role}).";
        await RefreshAuthUsersAsync();
        SelectedAuthUser = AuthUsers.FirstOrDefault(x => string.Equals(x.UserId, saved.UserId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task DeleteSelectedAuthUserAsync()
    {
        var selected = SelectedAuthUser;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ok = await _serverCapacityClient.DeleteAuthUserAsync(host, port, selected.UserId, ApiKey);
        StatusText = ok ? $"Deleted auth user {selected.UserId}." : $"Failed to delete auth user {selected.UserId}.";
        await RefreshAuthUsersAsync();
    }

    private async Task RefreshPluginsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
            return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
            return;

        var snapshot = await _serverCapacityClient.GetPluginsAsync(host, port, ApiKey);
        if (snapshot == null)
        {
            PluginStatus = "Failed to load plugin configuration.";
            return;
        }

        ConfiguredPluginAssemblies.Clear();
        foreach (var assembly in snapshot.ConfiguredAssemblies)
            ConfiguredPluginAssemblies.Add(assembly);

        LoadedPluginRunnerIds.Clear();
        foreach (var runnerId in snapshot.LoadedRunnerIds)
            LoadedPluginRunnerIds.Add(runnerId);

        PluginAssembliesText = string.Join(Environment.NewLine, ConfiguredPluginAssemblies);
        PluginStatus = $"Loaded {ConfiguredPluginAssemblies.Count} configured assembly(ies), {LoadedPluginRunnerIds.Count} loaded runner(s).";
    }

    private async Task SavePluginsAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var assemblies = (PluginAssembliesText ?? "")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var result = await _serverCapacityClient.UpdatePluginsAsync(host, port, assemblies, ApiKey);
        if (result == null)
        {
            PluginStatus = "Failed to save plugin configuration.";
            return;
        }

        PluginStatus = string.IsNullOrWhiteSpace(result.Message)
            ? "Plugin configuration updated."
            : result.Message;
        await RefreshPluginsAsync();
    }

    private async Task RefreshMcpAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
            return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
            return;

        var servers = await _serverCapacityClient.ListMcpServersAsync(host, port, ApiKey);
        McpServers.Clear();
        foreach (var row in servers)
            McpServers.Add(row);
        SelectedMcpServer = McpServers.FirstOrDefault();

        var mapping = await _serverCapacityClient.GetAgentMcpServersAsync(host, port, SelectedAgentId, ApiKey);
        AgentMcpServerIdsText = mapping == null
            ? ""
            : string.Join(Environment.NewLine, mapping.ServerIds);

        McpStatus = $"Loaded {McpServers.Count} MCP server(s) for registry.";
    }

    private async Task SaveMcpServerAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

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

        var saved = await _serverCapacityClient.UpsertMcpServerAsync(host, port, definition, ApiKey);
        if (saved == null)
        {
            McpStatus = "Failed to save MCP server.";
            return;
        }

        McpStatus = $"Saved MCP server '{saved.ServerId}'.";
        await RefreshMcpAsync();
        SelectedMcpServer = McpServers.FirstOrDefault(x => string.Equals(x.ServerId, saved.ServerId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task DeleteSelectedMcpServerAsync()
    {
        var selected = SelectedMcpServer;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ok = await _serverCapacityClient.DeleteMcpServerAsync(host, port, selected.ServerId, ApiKey);
        McpStatus = ok ? $"Deleted MCP server '{selected.ServerId}'." : $"Failed to delete MCP server '{selected.ServerId}'.";
        await RefreshMcpAsync();
    }

    private async Task SaveAgentMcpMappingAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ids = (AgentMcpServerIdsText ?? "")
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ok = await _serverCapacityClient.SetAgentMcpServersAsync(host, port, SelectedAgentId, ids, ApiKey);
        McpStatus = ok
            ? $"Saved MCP mapping for agent '{SelectedAgentId}'."
            : $"Failed to save MCP mapping for agent '{SelectedAgentId}'.";
        await RefreshMcpAsync();
    }

    private async Task RefreshPromptTemplatesAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
            return;
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
            return;

        var rows = await _serverCapacityClient.ListPromptTemplatesAsync(host, port, ApiKey);
        PromptTemplates.Clear();
        foreach (var row in rows)
            PromptTemplates.Add(row);
        SelectedPromptTemplate = PromptTemplates.FirstOrDefault();
        PromptTemplateStatus = $"Loaded {PromptTemplates.Count} prompt template(s).";
    }

    private async Task SavePromptTemplateAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var template = new PromptTemplateDefinition
        {
            TemplateId = PromptTemplateId ?? "",
            DisplayName = PromptTemplateName ?? "",
            Description = PromptTemplateDescription ?? "",
            TemplateContent = PromptTemplateContent ?? ""
        };
        var saved = await _serverCapacityClient.UpsertPromptTemplateAsync(host, port, template, ApiKey);
        if (saved == null)
        {
            PromptTemplateStatus = "Failed to save prompt template.";
            return;
        }

        PromptTemplateStatus = $"Saved template '{saved.TemplateId}'.";
        await RefreshPromptTemplatesAsync();
        SelectedPromptTemplate = PromptTemplates.FirstOrDefault(x => string.Equals(x.TemplateId, saved.TemplateId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task DeleteSelectedPromptTemplateAsync()
    {
        var selected = SelectedPromptTemplate;
        if (selected == null)
            return;

        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }

        var ok = await _serverCapacityClient.DeletePromptTemplateAsync(host, port, selected.TemplateId, ApiKey);
        PromptTemplateStatus = ok ? $"Deleted template '{selected.TemplateId}'." : $"Failed to delete template '{selected.TemplateId}'.";
        await RefreshPromptTemplatesAsync();
    }

    private async Task SeedContextAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            return;
        }
        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Port must be 1-65535.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SeedSessionId))
        {
            SeedStatus = "Seed session id is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SeedContent))
        {
            SeedStatus = "Seed content is required.";
            return;
        }

        var ok = await _serverCapacityClient.SeedSessionContextAsync(
            host,
            port,
            SeedSessionId,
            SeedContextType,
            SeedContent,
            SeedSource,
            Guid.NewGuid().ToString("N"),
            ApiKey);
        SeedStatus = ok
            ? $"Seed context queued for session '{SeedSessionId}'."
            : $"Failed to seed context for session '{SeedSessionId}'.";
    }

    private async Task StartLogMonitoringAsync()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            LogMonitorStatus = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            LogMonitorStatus = "Port must be 1-65535.";
            return;
        }

        StopLogMonitoring();
        _logMonitorCts = new CancellationTokenSource();
        var ct = _logMonitorCts.Token;

        var replayFromOffset = _structuredLogStore.GetMaxEventId(host, port, CurrentServerId);
        var snapshot = await ServerApiClient.GetStructuredLogsSnapshotAsync(host, port, replayFromOffset, 5000, ApiKey, ct);
        if (snapshot != null)
        {
            IngestStructuredLogs(host, port, snapshot.Entries);
            ReloadStructuredLogs();
        }

        var fromOffset = snapshot?.NextOffset ?? replayFromOffset;
        LogMonitorStatus = $"Monitoring logs from {host}:{port} (offset {fromOffset}).";

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

    private void ReloadStructuredLogs()
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

    private void IngestStructuredLogs(string host, int port, IEnumerable<StructuredLogEntry> entries)
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

        await TerminateSessionAsync(SelectedSession);
    }

    private async Task SendCurrentMessageAsync()
    {
        var session = SelectedSession;
        if (session == null)
            return;

        var text = session.PendingMessage?.TrimEnd() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!session.SessionClient.IsConnected)
            await ConnectSessionAsync(session);

        session.SessionClient.PerRequestContext = (PerRequestContext ?? "").Trim();
        await session.SessionClient.SendTextAsync(text);

        session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] user: {text}");
        session.PendingMessage = "";
        StatusText = $"Sent message to {session.Title} ({session.ConnectionMode}).";
    }

    private async Task TerminateSessionAsync(DesktopSessionViewModel? session)
    {
        if (session == null)
            return;

        try
        {
            if (session.SessionClient.IsConnected)
                await session.SessionClient.StopSessionAsync();
        }
        catch
        {
            // no-op
        }

        session.SessionClient.Disconnect();
        if (_sessionEventHandlers.TryGetValue(session, out var handlers))
        {
            session.SessionClient.MessageReceived -= handlers.OnMessage;
            session.SessionClient.ConnectionStateChanged -= handlers.OnConnectionStateChanged;
            _sessionEventHandlers.Remove(session);
        }

        var title = session.Title;
        Sessions.Remove(session);
        SelectedSession = Sessions.FirstOrDefault();
        StatusText = $"Terminated {title}.";
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
}
