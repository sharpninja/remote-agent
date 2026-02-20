using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;

namespace RemoteAgent.App.ViewModels;

public sealed class MainPageViewModel : INotifyPropertyChanged, ISessionCommandBus
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string PrefPerRequestContext = "PerRequestContext";
    private const string PrefApiKey = "ApiKey";
    private const string DefaultPort = "5244";

    /// <summary>Well-known ports offered in the port picker (Windows service = 5244, Linux/Docker = 5243).</summary>
    public static readonly IReadOnlyList<string> AvailablePorts = ["5244", "5243"];

    private readonly ISessionStore _sessionStore;
    private readonly IAgentGatewayClient _gateway;
    private readonly IServerApiClient _apiClient;
    private readonly IAppPreferences _preferences;
    private readonly IConnectionModeSelector _connectionModeSelector;
    private readonly IAgentSelector _agentSelector;
    private readonly IAttachmentPicker _attachmentPicker;
    private readonly IPromptTemplateSelector _promptTemplateSelector;
    private readonly IPromptVariableProvider _promptVariableProvider;
    private readonly ISessionTerminationConfirmation _sessionTerminationConfirmation;
    private readonly INotificationService _notificationService;
    private readonly IRequestDispatcher _dispatcher;

    private bool _isEditingTitle;

    private string _host = "";
    private string _port = DefaultPort;
    private string _apiKey = "";
    private string _status = "Enter host and port, then Connect.";
    private string _pendingMessage = "";
    private string _perRequestContext = "";
    private SessionItem? _currentSession;

    public MainPageViewModel(
        ISessionStore sessionStore,
        IAgentGatewayClient gateway,
        IServerApiClient apiClient,
        IAppPreferences preferences,
        IConnectionModeSelector connectionModeSelector,
        IAgentSelector agentSelector,
        IAttachmentPicker attachmentPicker,
        IPromptTemplateSelector promptTemplateSelector,
        IPromptVariableProvider promptVariableProvider,
        ISessionTerminationConfirmation sessionTerminationConfirmation,
        INotificationService notificationService,
        IRequestDispatcher dispatcher,
        IDeepLinkService deepLinkService)
    {
        _sessionStore = sessionStore;
        _gateway = gateway;
        _apiClient = apiClient;
        _preferences = preferences;
        _connectionModeSelector = connectionModeSelector;
        _agentSelector = agentSelector;
        _attachmentPicker = attachmentPicker;
        _promptTemplateSelector = promptTemplateSelector;
        _promptVariableProvider = promptVariableProvider;
        _sessionTerminationConfirmation = sessionTerminationConfirmation;
        _notificationService = notificationService;
        _dispatcher = dispatcher;

        ConnectCommand = new Command(async () => await RunAsync(new ConnectMobileSessionRequest(Guid.NewGuid(), this)), () => !_gateway.IsConnected && HasApiKey);
        DisconnectCommand = new Command(async () => await RunAsync(new DisconnectMobileSessionRequest(Guid.NewGuid(), this)), () => _gateway.IsConnected);
        NewSessionCommand = new Command(async () => await RunAsync(new CreateMobileSessionRequest(Guid.NewGuid(), this)));
        TerminateCurrentSessionCommand = new Command(async () => await RunAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), CurrentSession, this)));
        TerminateSessionCommand = new Command<SessionItem>(async session => await RunAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), session, this)));
        SendMessageCommand = new Command(async () => await RunAsync(new SendMobileMessageRequest(Guid.NewGuid(), this)), () => _gateway.IsConnected);
        AttachCommand = new Command(async () => await RunAsync(new SendMobileAttachmentRequest(Guid.NewGuid(), this)), () => _gateway.IsConnected);
        ArchiveMessageCommand = new Command<ChatMessage>(async msg => await RunAsync(new ArchiveMobileMessageRequest(Guid.NewGuid(), msg, this)));
        UsePromptTemplateCommand = new Command(async () => await RunAsync(new UsePromptTemplateRequest(Guid.NewGuid(), this)));
        BeginEditTitleCommand = new Command(() => { if (CurrentSession != null) IsEditingTitle = true; });
        ScanQrCodeCommand = new Command(async () => await RunAsync(new ScanQrCodeRequest(Guid.NewGuid(), this)), () => !HasApiKey);

        _gateway.ConnectionStateChanged += OnGatewayConnectionStateChanged;
        _gateway.MessageReceived += OnGatewayMessageReceived;

        LoadSavedServerDetails();
        LoadSessions();
        if (Sessions.Count > 0)
            CurrentSession = Sessions[0];

        deepLinkService.Subscribe(uri => _ = RunAsync(new ScanQrCodeRequest(Guid.NewGuid(), this) { RawUri = uri }));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SessionItem> Sessions { get; } = new();
    public ObservableCollection<ChatMessage> Messages => _gateway.Messages;

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand NewSessionCommand { get; }
    public ICommand TerminateCurrentSessionCommand { get; }
    public ICommand TerminateSessionCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand AttachCommand { get; }
    public ICommand ArchiveMessageCommand { get; }
    public ICommand UsePromptTemplateCommand { get; }
    public ICommand BeginEditTitleCommand { get; }
    public ICommand ScanQrCodeCommand { get; }

    public string Host
    {
        get => _host;
        set => Set(ref _host, value);
    }

    public string Port
    {
        get => _port;
        set => Set(ref _port, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (Set(ref _apiKey, value ?? ""))
            {
                _preferences.Set(PrefApiKey, _apiKey);
                OnPropertyChanged(nameof(HasApiKey));
                ((Command)ConnectCommand).ChangeCanExecute();
                ((Command)ScanQrCodeCommand).ChangeCanExecute();
            }
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string PendingMessage
    {
        get => _pendingMessage;
        set => Set(ref _pendingMessage, value);
    }

    public string PerRequestContext
    {
        get => _perRequestContext;
        set
        {
            if (Set(ref _perRequestContext, value ?? ""))
            {
                var normalized = _perRequestContext.Trim();
                _gateway.PerRequestContext = normalized;
                _preferences.Set(PrefPerRequestContext, normalized);
            }
        }
    }

    public SessionItem? CurrentSession
    {
        get => _currentSession;
        set
        {
            if (!Set(ref _currentSession, value)) return;
            if (value != null)
                _gateway.LoadFromStore(value.SessionId);
            else
                _gateway.LoadFromStore(null);
            OnPropertyChanged(nameof(CurrentSessionTitle));
            OnPropertyChanged(nameof(ConnectionModeLabel));
            OnPropertyChanged(nameof(CurrentSessionTitleEditorText));
        }
    }

    public string CurrentSessionTitle => CurrentSession?.Title ?? "No session";
    public string CurrentSessionTitleEditorText
    {
        get => CurrentSession?.Title ?? "";
        set
        {
            if (CurrentSession == null) return;
            CurrentSession.Title = string.IsNullOrWhiteSpace(value) ? "New chat" : value.Trim();
            _sessionStore.UpdateTitle(CurrentSession.SessionId, CurrentSession.Title);
            OnPropertyChanged(nameof(CurrentSessionTitle));
        }
    }

    public string ConnectionModeLabel => $"Mode: {(string.Equals(CurrentSession?.ConnectionMode, "direct", StringComparison.OrdinalIgnoreCase) ? "direct" : "server")}";

    public bool IsConnected => _gateway.IsConnected;
    public bool IsEditingTitle
    {
        get => _isEditingTitle;
        set => Set(ref _isEditingTitle, value);
    }

    /// <summary>Updates session title in store and fires property-changed notifications. Called by handlers.</summary>
    public void UpdateSessionTitle(string sessionId, string title)
    {
        _sessionStore.UpdateTitle(sessionId, title);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        OnPropertyChanged(nameof(CurrentSessionTitleEditorText));
    }

    /// <summary>Fires connection-state-change notifications. Called by handlers after connect/disconnect.</summary>
    public void NotifyConnectionStateChanged() => OnGatewayConnectionStateChanged();

    private async Task RunAsync<TResponse>(IRequest<TResponse> request)
    {
        try
        {
            await _dispatcher.SendAsync(request);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private void LoadSavedServerDetails()
    {
        Host = _preferences.Get(PrefServerHost, "");
        Port = _preferences.Get(PrefServerPort, DefaultPort);
        PerRequestContext = _preferences.Get(PrefPerRequestContext, "");
        _apiKey = _preferences.Get(PrefApiKey, "");
    }

    private void SaveServerDetails(string host, int port)
    {
        _preferences.Set(PrefServerHost, host ?? "");
        _preferences.Set(PrefServerPort, port.ToString());
    }

    private void LoadSessions()
    {
        Sessions.Clear();
        foreach (var s in _sessionStore.GetAll())
            Sessions.Add(s);
    }

    public void CommitSessionTitle(string value)
    {
        IsEditingTitle = false;
        if (CurrentSession == null) return;
        var newTitle = string.IsNullOrWhiteSpace(value) ? "New chat" : value.Trim();
        CurrentSession.Title = newTitle;
        _sessionStore.UpdateTitle(CurrentSession.SessionId, newTitle);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        OnPropertyChanged(nameof(CurrentSessionTitleEditorText));
    }

    public void StartNewSession() => _ = RunAsync(new CreateMobileSessionRequest(Guid.NewGuid(), this));

    public async Task<bool> TerminateSessionByIdAsync(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        var match = Sessions.FirstOrDefault(x => string.Equals(x.SessionId, sessionId, StringComparison.Ordinal));
        if (match == null) return false;
        var result = await _dispatcher.SendAsync(new TerminateMobileSessionRequest(Guid.NewGuid(), match, this));
        return result.Success;
    }

    Task<bool> ISessionCommandBus.TerminateSessionAsync(string? sessionId) =>
        TerminateSessionByIdAsync(sessionId);

    public string? GetCurrentSessionId() => CurrentSession?.SessionId;

    public bool SelectSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        var match = Sessions.FirstOrDefault(x => string.Equals(x.SessionId, sessionId, StringComparison.Ordinal));
        if (match == null) return false;
        CurrentSession = match;
        return true;
    }

    private void OnGatewayConnectionStateChanged()
    {
        OnPropertyChanged(nameof(IsConnected));
        ((Command)ConnectCommand).ChangeCanExecute();
        ((Command)DisconnectCommand).ChangeCanExecute();
        ((Command)SendMessageCommand).ChangeCanExecute();
        ((Command)AttachCommand).ChangeCanExecute();
        if (!_gateway.IsConnected)
            Status = "Enter host and port, then Connect.";
    }

    private void OnGatewayMessageReceived(ChatMessage msg)
    {
        if (msg.Priority == ChatMessagePriority.Notify)
        {
            var body = msg.IsEvent ? (msg.EventMessage ?? "Event") : (msg.Text.Length > 200 ? msg.Text[..200] + "…" : msg.Text);
            _notificationService.Show("Remote Agent", body);
        }
    }

    public static bool TryParseScriptRun(string text, out string pathOrCommand, out ScriptType scriptType)
    {
        pathOrCommand = "";
        scriptType = ScriptType.Bash;
        const string prefix = "/run ";
        if (text.Length <= prefix.Length || !text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = text[prefix.Length..].TrimStart();
        if (rest.StartsWith("bash ", StringComparison.OrdinalIgnoreCase))
        {
            scriptType = ScriptType.Bash;
            pathOrCommand = rest["bash ".Length..].Trim();
        }
        else if (rest.StartsWith("pwsh ", StringComparison.OrdinalIgnoreCase))
        {
            scriptType = ScriptType.Pwsh;
            pathOrCommand = rest["pwsh ".Length..].Trim();
        }
        else
        {
            pathOrCommand = rest;
        }

        return pathOrCommand.Length > 0;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
