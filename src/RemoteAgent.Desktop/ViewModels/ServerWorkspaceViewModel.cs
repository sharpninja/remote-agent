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

namespace RemoteAgent.Desktop.ViewModels;

public sealed class ServerWorkspaceViewModel : INotifyPropertyChanged, IServerConnectionContext
{
    private readonly CurrentServerContext _serverContext;
    private readonly IDesktopSessionViewModelFactory _sessionViewModelFactory;
    private readonly IRequestDispatcher _dispatcher;
    private readonly Dictionary<DesktopSessionViewModel, (Action<RemoteAgent.App.Services.ChatMessage> OnMessage, Action OnConnectionStateChanged)> _sessionEventHandlers = [];
    private DesktopSessionViewModel? _selectedSession;
    private string _host = "127.0.0.1";
    private string _port = "5243";
    private string _apiKey = "";
    private string _perRequestContext = "";
    private string _selectedConnectionMode = "server";
    private string _selectedAgentId = "process";
    private string _statusText = "Ready.";
    private string _capacitySummary = "Capacity not checked.";

    public ServerWorkspaceViewModel(
        CurrentServerContext serverContext,
        IServerCapacityClient serverCapacityClient,
        IDesktopStructuredLogStore structuredLogStore,
        IDesktopSessionViewModelFactory sessionViewModelFactory,
        IRequestDispatcher dispatcher)
    {
        _serverContext = serverContext;
        _sessionViewModelFactory = sessionViewModelFactory;
        _dispatcher = dispatcher;

        if (_serverContext.Registration != null)
        {
            _host = _serverContext.Registration.Host;
            _port = _serverContext.Registration.Port.ToString();
            _apiKey = _serverContext.Registration.ApiKey ?? "";
        }

        Security = new SecurityViewModel(dispatcher, this);
        AuthUsers = new AuthUsersViewModel(dispatcher, this);
        Plugins = new PluginsViewModel(dispatcher, this);
        McpRegistry = new McpRegistryDesktopViewModel(dispatcher, this);
        PromptTemplates = new PromptTemplatesViewModel(dispatcher, this);
        StructuredLogs = new StructuredLogsViewModel(dispatcher, this, structuredLogStore);

        // Forward sub-VM StatusText changes to parent StatusText for the global status bar.
        Security.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SecurityViewModel.StatusText) && !string.IsNullOrWhiteSpace(Security.StatusText))
                StatusText = Security.StatusText;
        };
        AuthUsers.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AuthUsersViewModel.StatusText) && !string.IsNullOrWhiteSpace(AuthUsers.StatusText))
                StatusText = AuthUsers.StatusText;
        };

        NewSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting new session...", NewSessionAsync));
        CheckCapacityCommand = new RelayCommand(() => _ = RunCommandAsync("Starting capacity check...", CheckCapacityAsync));
        TerminateCurrentSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting current session termination...", TerminateCurrentSessionAsync), () => SelectedSession != null);
        TerminateSessionCommand = new RelayCommand<DesktopSessionViewModel>(session => _ = RunCommandAsync("Starting session termination...", () => TerminateSessionAsync(session)), session => session != null);
        SendCurrentMessageCommand = new RelayCommand(() => _ = RunCommandAsync("Starting message send...", SendCurrentMessageAsync), () => SelectedSession != null);
        ToggleThemeCommand = new RelayCommand(() => RunCommand("Starting theme toggle...", ToggleTheme));

        ObserveBackgroundTask(NewSessionAsync(), "initial session load");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // IServerConnectionContext implementation
    string IServerConnectionContext.Host => _host;
    string IServerConnectionContext.Port => _port;
    string IServerConnectionContext.ApiKey => _apiKey;
    string IServerConnectionContext.SelectedAgentId => _selectedAgentId;
    string IServerConnectionContext.SelectedConnectionMode => _selectedConnectionMode;
    string IServerConnectionContext.PerRequestContext => _perRequestContext;
    string IServerConnectionContext.ServerId => _serverContext.Registration?.ServerId ?? "";
    string IServerConnectionContext.ServerDisplayName => _serverContext.Registration?.DisplayName ?? "";

    public string CurrentServerLabel =>
        _serverContext.Registration == null
            ? "(unassigned)"
            : $"{_serverContext.Registration.DisplayName} [{_serverContext.Registration.Host}:{_serverContext.Registration.Port}]";

    public string CurrentServerId => _serverContext.Registration?.ServerId ?? "";

    public ObservableCollection<DesktopSessionViewModel> Sessions { get; } = [];
    public IReadOnlyList<string> ConnectionModes { get; } = ["server", "direct"];

    // Sub-VMs
    public SecurityViewModel Security { get; }
    public AuthUsersViewModel AuthUsers { get; }
    public PluginsViewModel Plugins { get; }
    public McpRegistryDesktopViewModel McpRegistry { get; }
    public PromptTemplatesViewModel PromptTemplates { get; }
    public StructuredLogsViewModel StructuredLogs { get; }

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

    public ICommand NewSessionCommand { get; }
    public ICommand CheckCapacityCommand { get; }
    public ICommand TerminateCurrentSessionCommand { get; }
    public ICommand TerminateSessionCommand { get; }
    public ICommand SendCurrentMessageCommand { get; }
    public ICommand ToggleThemeCommand { get; }

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
