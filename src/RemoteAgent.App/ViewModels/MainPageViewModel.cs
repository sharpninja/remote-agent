using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;

namespace RemoteAgent.App.ViewModels;

public sealed class MainPageViewModel : INotifyPropertyChanged, ISessionCommandBus
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string PrefPerRequestContext = "PerRequestContext";
    private const string DefaultPort = "5243";

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

    private string _host = "";
    private string _port = DefaultPort;
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
        INotificationService notificationService)
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

        ConnectCommand = new Command(async () => await ConnectAsync(), () => !_gateway.IsConnected);
        DisconnectCommand = new Command(Disconnect, () => _gateway.IsConnected);
        NewSessionCommand = new Command(CreateNewSession);
        TerminateCurrentSessionCommand = new Command(async () => await TerminateCurrentSessionAsync());
        TerminateSessionCommand = new Command<SessionItem>(async session => await TerminateSessionAsync(session));
        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => _gateway.IsConnected);
        AttachCommand = new Command(async () => await SendAttachmentAsync(), () => _gateway.IsConnected);
        ArchiveMessageCommand = new Command<ChatMessage>(ArchiveMessage);
        UsePromptTemplateCommand = new Command(async () => await UsePromptTemplateAsync());

        _gateway.ConnectionStateChanged += OnGatewayConnectionStateChanged;
        _gateway.MessageReceived += OnGatewayMessageReceived;

        LoadSavedServerDetails();
        LoadSessions();
        if (Sessions.Count > 0)
            CurrentSession = Sessions[0];
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

    private void LoadSavedServerDetails()
    {
        Host = _preferences.Get(PrefServerHost, "");
        Port = _preferences.Get(PrefServerPort, DefaultPort);
        PerRequestContext = _preferences.Get(PrefPerRequestContext, "");
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

    private async Task ConnectAsync()
    {
        var selectedMode = await _connectionModeSelector.SelectAsync();
        if (string.IsNullOrWhiteSpace(selectedMode))
        {
            Status = "Connect cancelled.";
            return;
        }

        var host = (Host ?? "").Trim();
        var portText = (Port ?? DefaultPort).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            if (string.Equals(selectedMode, "direct", StringComparison.OrdinalIgnoreCase))
                host = "127.0.0.1";
            else
            {
                Status = "Enter a host.";
                return;
            }
        }

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            Status = "Enter a valid port (1-65535).";
            return;
        }

        SessionItem? sessionToConnect = CurrentSession;
        if (sessionToConnect == null)
        {
            sessionToConnect = new SessionItem
            {
                SessionId = Guid.NewGuid().ToString("N")[..12],
                Title = "New chat",
                ConnectionMode = selectedMode
            };
            Sessions.Insert(0, sessionToConnect);
            _sessionStore.Add(sessionToConnect);
            CurrentSession = sessionToConnect;
        }

        if (string.IsNullOrWhiteSpace(sessionToConnect.AgentId))
        {
            if (string.Equals(selectedMode, "server", StringComparison.OrdinalIgnoreCase))
            {
                Status = "Getting server info...";
                var serverInfo = await _apiClient.GetServerInfoAsync(host, port);
                if (serverInfo == null)
                {
                    Status = "Could not reach server.";
                    return;
                }

                var agentId = await _agentSelector.SelectAsync(serverInfo);
                if (agentId == null)
                {
                    Status = "Connect cancelled.";
                    return;
                }

                sessionToConnect.AgentId = agentId;
            }
            else
            {
                sessionToConnect.AgentId = "process";
            }

            _sessionStore.UpdateAgentId(sessionToConnect.SessionId, sessionToConnect.AgentId);
        }

        sessionToConnect.ConnectionMode = selectedMode;
        _sessionStore.UpdateConnectionMode(sessionToConnect.SessionId, selectedMode);
        OnPropertyChanged(nameof(ConnectionModeLabel));

        if (string.Equals(selectedMode, "server", StringComparison.OrdinalIgnoreCase))
        {
            var capacity = await _apiClient.GetSessionCapacityAsync(host, port, sessionToConnect.AgentId);
            if (capacity == null)
            {
                Status = "Could not verify server session capacity.";
                return;
            }

            if (!capacity.CanCreateSession)
            {
                Status = string.IsNullOrWhiteSpace(capacity.Reason)
                    ? "Server session capacity reached."
                    : capacity.Reason;
                return;
            }
        }

        Status = $"Connecting ({selectedMode})...";
        try
        {
            await _gateway.ConnectAsync(host, port, sessionToConnect.SessionId, sessionToConnect.AgentId);
            SaveServerDetails(host, port);
            Host = host;
            Port = port.ToString();
            Status = $"Connected ({selectedMode}).";
            OnGatewayConnectionStateChanged();
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
            OnGatewayConnectionStateChanged();
        }
    }

    private void Disconnect()
    {
        _gateway.Disconnect();
        OnGatewayConnectionStateChanged();
    }

    private void CreateNewSession()
    {
        var session = new SessionItem
        {
            SessionId = Guid.NewGuid().ToString("N")[..12],
            Title = "New chat",
            AgentId = "",
            ConnectionMode = "server"
        };
        _sessionStore.Add(session);
        Sessions.Insert(0, session);
        CurrentSession = session;
    }

    public void CommitSessionTitle(string value)
    {
        if (CurrentSession == null) return;
        var newTitle = string.IsNullOrWhiteSpace(value) ? "New chat" : value.Trim();
        CurrentSession.Title = newTitle;
        _sessionStore.UpdateTitle(CurrentSession.SessionId, newTitle);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        OnPropertyChanged(nameof(CurrentSessionTitleEditorText));
    }

    public void StartNewSession()
    {
        CreateNewSession();
    }

    public async Task<bool> TerminateSessionByIdAsync(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        var match = Sessions.FirstOrDefault(x => string.Equals(x.SessionId, sessionId, StringComparison.Ordinal));
        if (match == null) return false;
        return await TerminateSessionAsync(match);
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

    private async Task TerminateCurrentSessionAsync()
    {
        await TerminateSessionAsync(CurrentSession);
    }

    private async Task<bool> TerminateSessionAsync(SessionItem? session)
    {
        if (session == null)
        {
            Status = "No session selected.";
            return false;
        }

        var sessionLabel = string.IsNullOrWhiteSpace(session.Title) ? session.SessionId : session.Title;
        var confirmed = await _sessionTerminationConfirmation.ConfirmAsync(sessionLabel);
        if (!confirmed)
        {
            Status = "Terminate cancelled.";
            return false;
        }

        var isCurrent = string.Equals(CurrentSession?.SessionId, session.SessionId, StringComparison.Ordinal);
        if (isCurrent && _gateway.IsConnected)
        {
            try
            {
                await _gateway.StopSessionAsync();
            }
            catch
            {
                // best effort; always close local transport after stop request attempt
            }

            _gateway.Disconnect();
        }

        _sessionStore.Delete(session.SessionId);
        Sessions.Remove(session);
        if (isCurrent)
            CurrentSession = Sessions.FirstOrDefault();

        Status = $"Session terminated: {sessionLabel}";
        return true;
    }

    private async Task SendMessageAsync()
    {
        var text = (PendingMessage ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text) || !_gateway.IsConnected) return;

        if (CurrentSession != null && (CurrentSession.Title == "New chat" || string.IsNullOrWhiteSpace(CurrentSession.Title)))
        {
            CurrentSession.Title = text.Length > 60 ? text[..60] + "…" : text;
            _sessionStore.UpdateTitle(CurrentSession.SessionId, CurrentSession.Title);
            OnPropertyChanged(nameof(CurrentSessionTitle));
            OnPropertyChanged(nameof(CurrentSessionTitleEditorText));
        }

        _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = text });
        PendingMessage = "";

        try
        {
            if (TryParseScriptRun(text, out var pathOrCommand, out var scriptType))
                await _gateway.SendScriptRequestAsync(pathOrCommand, scriptType);
            else
                await _gateway.SendTextAsync(text);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }

    private async Task SendAttachmentAsync()
    {
        if (!_gateway.IsConnected) return;
        var picked = await _attachmentPicker.PickAsync();
        if (picked == null) return;

        try
        {
            _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = $"[Attachment: {picked.FileName}]" });
            await _gateway.SendMediaAsync(picked.Content, picked.ContentType, picked.FileName);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }

    private async Task UsePromptTemplateAsync()
    {
        var host = (Host ?? "").Trim();
        var portText = (Port ?? DefaultPort).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            Status = "Enter host to load templates.";
            return;
        }

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            Status = "Enter a valid port (1-65535).";
            return;
        }

        var response = await _apiClient.ListPromptTemplatesAsync(host, port);
        if (response == null || response.Templates.Count == 0)
        {
            Status = "No prompt templates available.";
            return;
        }

        var template = await _promptTemplateSelector.SelectAsync(response.Templates.ToList());
        if (template == null)
        {
            Status = "Prompt template selection cancelled.";
            return;
        }

        var variables = PromptTemplateEngine.ExtractVariables(template.TemplateContent);
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            var value = await _promptVariableProvider.GetValueAsync(variable);
            if (value == null)
            {
                Status = "Prompt template input cancelled.";
                return;
            }

            data[variable] = value;
        }

        PendingMessage = PromptTemplateEngine.Render(template.TemplateContent, data);
        await SendMessageAsync();
    }

    private void ArchiveMessage(ChatMessage? message)
    {
        if (message == null) return;
        message.IsArchived = true;
        _gateway.SetArchived(message, true);
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

    private static bool TryParseScriptRun(string text, out string pathOrCommand, out ScriptType scriptType)
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
