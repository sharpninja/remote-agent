using System.Collections.ObjectModel;
using System.Windows.Input;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;
using Microsoft.Maui.Storage;

namespace RemoteAgent.App;

public partial class MainPage : ContentPage
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string DefaultPort = "5243";

    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "remote-agent.db");
    private readonly ILocalMessageStore _messageStore;
    private readonly ISessionStore _sessionStore;
    private readonly AgentGatewayClientService _gateway;

    /// <summary>All sessions for the list (TR-12.1.3).</summary>
    public ObservableCollection<SessionItem> Sessions { get; } = new();

    /// <summary>Current session (selected); messages and connect use this (FR-11.1).</summary>
    private SessionItem? _currentSession;
    public SessionItem? CurrentSession
    {
        get => _currentSession;
        set
        {
            if (_currentSession == value) return;
            _currentSession = value;
            OnPropertyChanged(nameof(CurrentSession));
            OnPropertyChanged(nameof(CurrentSessionTitle));
            if (value != null)
            {
                _gateway.LoadFromStore(value.SessionId);
                UpdateSessionTitleControls(value.Title);
                if (SessionTitleEntry != null) SessionTitleEntry.Text = value.Title;
            }
            else
            {
                _gateway.LoadFromStore(null);
                if (SessionTitleLabel != null) SessionTitleLabel.Text = "No session";
                if (SessionTitleEntry != null) SessionTitleEntry.Text = "";
            }
        }
    }

    /// <summary>Title for binding (or editable).</summary>
    public string CurrentSessionTitle => _currentSession?.Title ?? "No session";

    public MainPage()
    {
        _messageStore = new LocalMessageStore(_dbPath);
        _sessionStore = new LocalSessionStore(_dbPath);
        _gateway = new AgentGatewayClientService(_messageStore);
        InitializeComponent();
        LoadSavedServerDetails();
        LoadSessions();
        MessagesList.ItemsSource = _gateway.Messages;
        SessionsList.ItemsSource = Sessions;
        _gateway.ConnectionStateChanged += () => MainThread.BeginInvokeOnMainThread(UpdateConnectionState);
        _gateway.MessageReceived += OnMessageReceived;
        UpdateConnectionState();
        // If we have sessions, select first; else leave null (user will tap New session).
        if (Sessions.Count > 0 && CurrentSession == null)
            SelectSession(Sessions[0]);
    }

    private void LoadSessions()
    {
        Sessions.Clear();
        foreach (var s in _sessionStore.GetAll())
            Sessions.Add(s);
    }

    private void LoadSavedServerDetails()
    {
        var host = Preferences.Default.Get(PrefServerHost, "");
        var port = Preferences.Default.Get(PrefServerPort, DefaultPort);
        if (!string.IsNullOrEmpty(host))
            HostEntry.Text = host;
        PortEntry.Text = string.IsNullOrEmpty(port) ? DefaultPort : port;
    }

    private void SaveServerDetails(string host, int port)
    {
        Preferences.Default.Set(PrefServerHost, host ?? "");
        Preferences.Default.Set(PrefServerPort, port.ToString());
    }

    public ICommand ArchiveMessageCommand => new Command<ChatMessage>(msg =>
    {
        if (msg != null)
        {
            msg.IsArchived = true;
            _gateway.SetArchived(msg, true);
        }
    });

    private void OnMessageReceived(ChatMessage msg)
    {
        if (msg.Priority != ChatMessagePriority.Notify) return;
        ShowNotificationForMessage(msg);
    }

    private void ShowNotificationForMessage(ChatMessage msg)
    {
        var body = msg.IsEvent ? (msg.EventMessage ?? "Event") : (msg.Text.Length > 200 ? msg.Text[..200] + "…" : msg.Text);
#if ANDROID
        PlatformNotificationService.ShowNotification("Remote Agent", body);
#endif
    }

    private void UpdateConnectionState()
    {
        ConnectBtn.IsEnabled = !_gateway.IsConnected;
        DisconnectBtn.IsEnabled = _gateway.IsConnected;
        SendBtn.IsEnabled = _gateway.IsConnected;
        AttachBtn.IsEnabled = _gateway.IsConnected;
        MessageEntry.IsEnabled = _gateway.IsConnected;
        StatusLabel.Text = _gateway.IsConnected ? "Connected" : "Enter host and port, then Connect.";
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        var host = (HostEntry.Text ?? "").Trim();
        var portText = (PortEntry.Text ?? "5243").Trim();
        if (string.IsNullOrEmpty(host))
        {
            StatusLabel.Text = "Enter a host.";
            return;
        }
        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            StatusLabel.Text = "Enter a valid port (1-65535).";
            return;
        }

        StatusLabel.Text = "Getting server info...";
        var serverInfo = await AgentGatewayClientService.GetServerInfoAsync(host, port);
        if (serverInfo == null)
        {
            StatusLabel.Text = "Could not reach server.";
            return;
        }

        // Ensure we have a current session (FR-11.1.2, TR-12.2).
        if (CurrentSession == null)
        {
            var agentId = await ShowAgentPickerAsync(serverInfo);
            if (agentId == null) { StatusLabel.Text = "Enter host and port, then Connect."; return; }
            var session = new SessionItem
            {
                SessionId = Guid.NewGuid().ToString("N")[..12],
                Title = "New chat",
                AgentId = agentId
            };
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _sessionStore.Add(session);
                Sessions.Insert(0, session);
                CurrentSession = session;
            });
        }
        else if (string.IsNullOrEmpty(CurrentSession.AgentId))
        {
            var agentId = await ShowAgentPickerAsync(serverInfo);
            if (agentId == null) { StatusLabel.Text = "Enter host and port, then Connect."; return; }
            var sessionId = CurrentSession.SessionId;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentSession!.AgentId = agentId;
                _sessionStore.UpdateAgentId(sessionId, agentId);
            });
        }

        StatusLabel.Text = "Connecting...";
        try
        {
            await _gateway.ConnectAsync(host, port, CurrentSession.SessionId, CurrentSession.AgentId);
            SaveServerDetails(host, port);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "Connected.";
                UpdateConnectionState();
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = $"Failed: {ex.Message}";
                UpdateConnectionState();
            });
        }
    }

    private async Task<string?> ShowAgentPickerAsync(ServerInfoResponse serverInfo)
    {
        var agents = serverInfo.AvailableAgents.ToList();
        if (agents.Count == 0)
            return "";
        if (agents.Count == 1)
            return agents[0];
        var choice = await DisplayActionSheetAsync("Select agent", "Cancel", null, agents.ToArray());
        return string.IsNullOrEmpty(choice) ? null : choice;
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        _gateway.Disconnect();
        UpdateConnectionState();
    }

    private void OnNewSessionClicked(object? sender, EventArgs e)
    {
        var session = new SessionItem
        {
            SessionId = Guid.NewGuid().ToString("N")[..12],
            Title = "New chat",
            AgentId = ""
        };
        _sessionStore.Add(session);
        Sessions.Insert(0, session);
        SelectSession(session);
    }

    private void SelectSession(SessionItem session)
    {
        CurrentSession = session;
        // Visual selection: could set SelectedItem on CollectionView if we had it
    }

    private void OnSessionTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is SessionItem s)
            SelectSession(s);
    }

    private void OnSessionTitleFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused && SessionTitleEntry.Text is { } t)
        {
            SessionTitleEntry.CursorPosition = 0;
            SessionTitleEntry.SelectionLength = t.Length;
        }
    }

    private void OnSessionTitleUnfocused(object? sender, FocusEventArgs e)
    {
        if (!e.IsFocused)
            CommitSessionTitle();
    }

    private void OnSessionTitleCompleted(object? sender, EventArgs e)
    {
        CommitSessionTitle();
    }

    private void CommitSessionTitle()
    {
        if (CurrentSession != null)
        {
            var newTitle = (SessionTitleEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(newTitle)) newTitle = "New chat";
            CurrentSession.Title = newTitle;
            _sessionStore.UpdateTitle(CurrentSession.SessionId, newTitle);
            SessionTitleLabel.Text = newTitle;
        }
        SessionTitleEntry.IsVisible = false;
        SessionTitleLabel.IsVisible = true;
    }

    private void OnSessionTitleLabelTapped(object? sender, TappedEventArgs e)
    {
        if (CurrentSession == null) return;
        SessionTitleLabel.IsVisible = false;
        SessionTitleEntry.Text = CurrentSession.Title;
        SessionTitleEntry.IsVisible = true;
        SessionTitleEntry.Focus();
    }

    private void OnMessageEntryCompleted(object? sender, EventArgs e)
    {
        SendMessage();
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        SendMessage();
    }

    private async void SendMessage()
    {
        var text = (MessageEntry.Text ?? "").Trim();
        if (string.IsNullOrEmpty(text) || !_gateway.IsConnected) return;

        // Default session title to first request (FR-11.1.3, TR-12.2.1).
        if (CurrentSession != null && (CurrentSession.Title == "New chat" || string.IsNullOrWhiteSpace(CurrentSession.Title)))
        {
            CurrentSession.Title = text.Length > 60 ? text[..60] + "…" : text;
            _sessionStore.UpdateTitle(CurrentSession.SessionId, CurrentSession.Title);
            SessionTitleLabel.Text = CurrentSession.Title;
        }

        _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = text });
        MessageEntry.Text = "";
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

    private async void OnAttachClicked(object? sender, EventArgs e)
    {
        if (!_gateway.IsConnected) return;
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "image/*", "video/*" },
                [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" }
            });
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Pick image or video", FileTypes = customFileType });
            if (result == null) return;
            await using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var content = ms.ToArray();
            var contentType = result.ContentType ?? "application/octet-stream";
            var fileName = result.FileName ?? "attachment";
            _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = $"[Attachment: {fileName}]" });
            await _gateway.SendMediaAsync(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }

    /// <summary>Parse /run bash &lt;path&gt; or /run pwsh &lt;path&gt; (FR-9.1).</summary>
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
}
