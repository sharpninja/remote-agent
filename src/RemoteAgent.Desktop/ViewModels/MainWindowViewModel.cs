using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IServerRegistrationStore _serverRegistrationStore;
    private readonly IServerWorkspaceFactory _serverWorkspaceFactory;
    private readonly IRequestDispatcher _dispatcher;
    private readonly Dictionary<string, ServerWorkspaceLease> _workspaceLeases = [];
    private Func<Window?>? _ownerWindowFactory;

    private ServerRegistration? _selectedServer;
    private ServerWorkspaceViewModel? _currentServerViewModel;
    private string _currentServerId = "";
    private string _statusText = "Ready.";
    private bool _isStatusLogExpanded;

    private string _editDisplayName = "";
    private string _editHost = "127.0.0.1";
    private string _editPort = "5243";
    private string _editApiKey = "";
    private string _localServerActionLabel = "Check Local Server";
    private string _localServerStatusText = "Local server status not checked.";
    private bool _canApplyLocalServerAction;
    private bool _localServerRunning;
    private string _selectedManagementSection = "ServerSetup";

    public MainWindowViewModel(
        IServerRegistrationStore serverRegistrationStore,
        IServerWorkspaceFactory serverWorkspaceFactory,
        ILocalServerManager localServerManager,
        IRequestDispatcher dispatcher,
        AppLogViewModel appLog)
    {
        _serverRegistrationStore = serverRegistrationStore;
        _serverWorkspaceFactory = serverWorkspaceFactory;
        _dispatcher = dispatcher;
        AppLog = appLog;

        NewServerCommand = new RelayCommand(() => RunCommand("Starting new server draft...", NewServerDraft));
        SaveServerCommand = new RelayCommand(() => _ = RunCommandAsync("Saving server...", SaveServerAsync));
        RemoveServerCommand = new RelayCommand(() => _ = RunCommandAsync("Removing server...", RemoveSelectedServerAsync), () => SelectedServer != null);
        CheckLocalServerCommand = new RelayCommand(() => _ = RunCommandAsync("Starting local server status check...", CheckLocalServerAsync));
        ApplyLocalServerActionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting local server action...", ApplyLocalServerActionAsync), () => CanApplyLocalServerAction);
        CollapseStatusLogCommand = new RelayCommand(CollapseStatusLogPanel);
        CopyStatusLogCommand = new RelayCommand(() => _ = ExecuteCopyStatusLogAsync());
        SetManagementSectionCommand = new RelayCommand<string>(sectionKey => _ = ExecuteSetManagementSectionAsync(sectionKey));
        ExpandStatusLogCommand = new RelayCommand(() => _ = ExecuteExpandStatusLogAsync());
        StartSessionCommand = new RelayCommand(() => _ = RunCommandAsync("Starting new session...", StartSessionAsync), () => CurrentServerViewModel != null);

        LoadServers();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ServerRegistration> Servers { get; } = [];
    public ObservableCollection<StatusLogEntry> StatusLogEntries { get; } = [];

    public ServerRegistration? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (_selectedServer == value) return;
            _selectedServer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveServerName));
            ((RelayCommand)RemoveServerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StartSessionCommand).RaiseCanExecuteChanged();
            PopulateEditorFromSelection();
            EnsureCurrentServerWorkspace();
        }
    }

    public ServerWorkspaceViewModel? CurrentServerViewModel
    {
        get => _currentServerViewModel;
        private set
        {
            if (_currentServerViewModel == value) return;

            if (_currentServerViewModel != null)
                _currentServerViewModel.PropertyChanged -= OnCurrentServerViewModelPropertyChanged;

            _currentServerViewModel = value;

            if (_currentServerViewModel != null)
                _currentServerViewModel.PropertyChanged += OnCurrentServerViewModelPropertyChanged;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCurrentServer));
            ((RelayCommand)StartSessionCommand).RaiseCanExecuteChanged();
        }
    }

    public bool HasCurrentServer => CurrentServerViewModel != null;

    public string CurrentServerId
    {
        get => _currentServerId;
        private set
        {
            if (_currentServerId == value) return;
            _currentServerId = value;
            OnPropertyChanged();
        }
    }

    public string ActiveServerName => SelectedServer?.DisplayName ?? "(none)";

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
            AppendStatusLogEntry(value);
        }
    }

    public bool IsStatusLogExpanded
    {
        get => _isStatusLogExpanded;
        private set
        {
            if (_isStatusLogExpanded == value) return;
            _isStatusLogExpanded = value;
            OnPropertyChanged();
        }
    }

    public string EditDisplayName
    {
        get => _editDisplayName;
        set
        {
            if (_editDisplayName == value) return;
            _editDisplayName = value;
            OnPropertyChanged();
        }
    }

    public string EditHost
    {
        get => _editHost;
        set
        {
            if (_editHost == value) return;
            _editHost = value;
            OnPropertyChanged();
        }
    }

    public string EditPort
    {
        get => _editPort;
        set
        {
            if (_editPort == value) return;
            _editPort = value;
            OnPropertyChanged();
        }
    }

    public string EditApiKey
    {
        get => _editApiKey;
        set
        {
            if (_editApiKey == value) return;
            _editApiKey = value;
            OnPropertyChanged();
        }
    }

    public string LocalServerActionLabel
    {
        get => _localServerActionLabel;
        private set
        {
            if (_localServerActionLabel == value) return;
            _localServerActionLabel = value;
            OnPropertyChanged();
        }
    }

    public string LocalServerStatusText
    {
        get => _localServerStatusText;
        private set
        {
            if (_localServerStatusText == value) return;
            _localServerStatusText = value;
            OnPropertyChanged();
        }
    }

    public bool CanApplyLocalServerAction
    {
        get => _canApplyLocalServerAction;
        private set
        {
            if (_canApplyLocalServerAction == value) return;
            _canApplyLocalServerAction = value;
            OnPropertyChanged();
            ((RelayCommand)ApplyLocalServerActionCommand).RaiseCanExecuteChanged();
        }
    }

    public string SelectedManagementSection
    {
        get => _selectedManagementSection;
        set
        {
            if (string.Equals(_selectedManagementSection, value, StringComparison.Ordinal))
                return;

            _selectedManagementSection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsServerSetupSectionSelected));
            OnPropertyChanged(nameof(IsSessionsSectionSelected));
            OnPropertyChanged(nameof(IsLocalServerSectionSelected));
            OnPropertyChanged(nameof(IsOpenSessionsSectionSelected));
            OnPropertyChanged(nameof(IsStructuredLogsSectionSelected));
            OnPropertyChanged(nameof(IsSecuritySectionSelected));
            OnPropertyChanged(nameof(IsHistorySectionSelected));
            OnPropertyChanged(nameof(IsAuthUsersSectionSelected));
            OnPropertyChanged(nameof(IsPluginsSectionSelected));
            OnPropertyChanged(nameof(IsMcpSectionSelected));
            OnPropertyChanged(nameof(IsPromptsSectionSelected));
            OnPropertyChanged(nameof(IsSettingsSectionSelected));
            OnPropertyChanged(nameof(IsAppLogSectionSelected));
        }
    }

    public bool IsServerSetupSectionSelected => string.Equals(SelectedManagementSection, "ServerSetup", StringComparison.Ordinal);
    public bool IsSessionsSectionSelected => string.Equals(SelectedManagementSection, "Sessions", StringComparison.Ordinal);
    public bool IsLocalServerSectionSelected => string.Equals(SelectedManagementSection, "LocalServer", StringComparison.Ordinal);
    public bool IsOpenSessionsSectionSelected => string.Equals(SelectedManagementSection, "OpenSessions", StringComparison.Ordinal);
    public bool IsStructuredLogsSectionSelected => string.Equals(SelectedManagementSection, "StructuredLogs", StringComparison.Ordinal);
    public bool IsSecuritySectionSelected => string.Equals(SelectedManagementSection, "Security", StringComparison.Ordinal);
    public bool IsHistorySectionSelected => string.Equals(SelectedManagementSection, "History", StringComparison.Ordinal);
    public bool IsAuthUsersSectionSelected => string.Equals(SelectedManagementSection, "AuthUsers", StringComparison.Ordinal);
    public bool IsPluginsSectionSelected => string.Equals(SelectedManagementSection, "Plugins", StringComparison.Ordinal);
    public bool IsMcpSectionSelected => string.Equals(SelectedManagementSection, "Mcp", StringComparison.Ordinal);
    public bool IsPromptsSectionSelected => string.Equals(SelectedManagementSection, "Prompts", StringComparison.Ordinal);
    public bool IsSettingsSectionSelected => string.Equals(SelectedManagementSection, "Settings", StringComparison.Ordinal);
    public bool IsAppLogSectionSelected => string.Equals(SelectedManagementSection, "AppLog", StringComparison.Ordinal);

    public AppLogViewModel AppLog { get; private set; } = null!;

    public ICommand NewServerCommand { get; }
    public ICommand SaveServerCommand { get; }
    public ICommand RemoveServerCommand { get; }
    public ICommand CheckLocalServerCommand { get; }
    public ICommand ApplyLocalServerActionCommand { get; }
    public ICommand CollapseStatusLogCommand { get; }
    public ICommand CopyStatusLogCommand { get; }
    public ICommand SetManagementSectionCommand { get; }
    public ICommand ExpandStatusLogCommand { get; }
    public ICommand StartSessionCommand { get; }

    public void SetOwnerWindow(Func<Window?> factory) => _ownerWindowFactory = factory;

    public void SetManagementSection(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
            return;

        SelectedManagementSection = sectionKey;
    }

    public void Dispose()
    {
        if (_currentServerViewModel != null)
            _currentServerViewModel.PropertyChanged -= OnCurrentServerViewModelPropertyChanged;

        foreach (var lease in _workspaceLeases.Values)
            lease.Dispose();
        _workspaceLeases.Clear();
    }

    private async Task ExecuteSetManagementSectionAsync(string? sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
            return;

        await _dispatcher.SendAsync(new SetManagementSectionRequest(Guid.NewGuid(), sectionKey));
        SelectedManagementSection = sectionKey;
    }

    private async Task ExecuteExpandStatusLogAsync()
    {
        await _dispatcher.SendAsync(new ExpandStatusLogPanelRequest(Guid.NewGuid()));
        ExpandStatusLogPanel();
    }

    private async Task StartSessionAsync()
    {
        if (CurrentServerViewModel == null || _ownerWindowFactory == null)
            return;

        var result = await _dispatcher.SendAsync(
            new OpenNewSessionRequest(Guid.NewGuid(), _ownerWindowFactory, CurrentServerViewModel));

        if (result.Success)
            SelectedManagementSection = "Sessions";
        else if (result.ErrorMessage != "Cancelled.")
            StatusText = $"Start session failed: {result.ErrorMessage}";
    }

    private async Task SaveServerAsync()
    {
        var host = (EditHost ?? "").Trim();
        if (!int.TryParse((EditPort ?? "").Trim(), out var port))
            port = 0;

        var result = await _dispatcher.SendAsync(new SaveServerRegistrationRequest(
            Guid.NewGuid(),
            SelectedServer?.ServerId,
            EditDisplayName,
            host,
            port,
            EditApiKey ?? ""));

        if (!result.Success)
        {
            StatusText = result.ErrorMessage ?? "Save failed.";
            return;
        }

        var saved = result.Data!;

        if (_workspaceLeases.TryGetValue(saved.ServerId, out var existingLease))
        {
            existingLease.Dispose();
            _workspaceLeases.Remove(saved.ServerId);
        }

        var existingIndex = Servers.ToList().FindIndex(x => string.Equals(x.ServerId, saved.ServerId, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            Servers[existingIndex] = saved;
        else
            Servers.Add(saved);

        SelectedServer = Servers.FirstOrDefault(x => string.Equals(x.ServerId, saved.ServerId, StringComparison.OrdinalIgnoreCase));
        StatusText = $"Saved server '{saved.DisplayName}'.";
    }

    private async Task RemoveSelectedServerAsync()
    {
        var selected = SelectedServer;
        if (selected == null)
            return;

        var result = await _dispatcher.SendAsync(new RemoveServerRegistrationRequest(
            Guid.NewGuid(),
            selected.ServerId,
            selected.DisplayName));

        if (!result.Success)
        {
            StatusText = result.ErrorMessage ?? "Remove failed.";
            return;
        }

        if (_workspaceLeases.TryGetValue(selected.ServerId, out var lease))
        {
            lease.Dispose();
            _workspaceLeases.Remove(selected.ServerId);
        }

        Servers.Remove(selected);
        SelectedServer = Servers.FirstOrDefault();

        if (SelectedServer == null)
            NewServerDraft();

        StatusText = $"Removed server '{selected.DisplayName}'.";
    }

    private async Task CheckLocalServerAsync()
    {
        LocalServerStatusText = "Checking local server status...";
        StatusText = "Checking local server status...";

        var result = await _dispatcher.SendAsync(new CheckLocalServerRequest(Guid.NewGuid()));
        if (!result.Success)
        {
            StatusText = result.ErrorMessage ?? "Check failed.";
            return;
        }

        var probe = result.Data!;
        _localServerRunning = probe.IsRunning;
        LocalServerActionLabel = probe.RecommendedActionLabel;
        CanApplyLocalServerAction = probe.CanApplyAction;
        LocalServerStatusText = probe.Message;
        StatusText = probe.Message;
    }

    private async Task ApplyLocalServerActionAsync()
    {
        if (_localServerRunning)
        {
            LocalServerStatusText = "Stopping local server...";
            StatusText = "Stopping local server...";
        }
        else
        {
            LocalServerStatusText = "Starting local server...";
            StatusText = "Starting local server...";
        }

        var result = await _dispatcher.SendAsync(
            new ApplyLocalServerActionRequest(Guid.NewGuid(), _localServerRunning));

        if (!result.Success)
        {
            StatusText = result.ErrorMessage ?? "Action failed.";
            LocalServerStatusText = result.ErrorMessage ?? "Action failed.";
            return;
        }

        var probe = result.Data!;
        _localServerRunning = probe.IsRunning;
        LocalServerActionLabel = probe.RecommendedActionLabel;
        CanApplyLocalServerAction = probe.CanApplyAction;
        LocalServerStatusText = probe.Message;
        StatusText = probe.Message;
    }

    private void LoadServers()
    {
        Servers.Clear();
        var rows = _serverRegistrationStore.GetAll();
        foreach (var row in rows)
            Servers.Add(row);

        if (Servers.Count == 0)
        {
            var created = _serverRegistrationStore.Upsert(new ServerRegistration
            {
                DisplayName = "Local Server",
                Host = "127.0.0.1",
                Port = 5243,
                ApiKey = ""
            });
            Servers.Add(created);
        }

        SelectedServer = Servers[0];
        StatusText = $"Loaded {Servers.Count} registered server(s).";
    }

    private void PopulateEditorFromSelection()
    {
        if (SelectedServer == null)
        {
            NewServerDraft();
            return;
        }

        EditDisplayName = SelectedServer.DisplayName;
        EditHost = SelectedServer.Host;
        EditPort = SelectedServer.Port.ToString();
        EditApiKey = SelectedServer.ApiKey;
    }

    private void EnsureCurrentServerWorkspace()
    {
        var selected = SelectedServer;
        if (selected == null)
        {
            CurrentServerId = "";
            CurrentServerViewModel = null;
            return;
        }

        CurrentServerId = selected.ServerId;
        if (!_workspaceLeases.TryGetValue(CurrentServerId, out var lease))
        {
            lease = _serverWorkspaceFactory.Create(selected);
            _workspaceLeases[CurrentServerId] = lease;
        }

        CurrentServerViewModel = lease.ViewModel;
        StatusText = $"Current server: {selected.DisplayName} ({selected.Host}:{selected.Port}).";
    }

    private void NewServerDraft()
    {
        EditDisplayName = "";
        EditHost = "127.0.0.1";
        EditPort = "5243";
        EditApiKey = "";
        StatusText = "New server draft ready.";
    }

    public void ExpandStatusLogPanel()
    {
        IsStatusLogExpanded = true;
    }

    private void CollapseStatusLogPanel()
    {
        IsStatusLogExpanded = false;
    }

    private async Task ExecuteCopyStatusLogAsync()
    {
        var result = await _dispatcher.SendAsync(
            new CopyStatusLogRequest(Guid.NewGuid(), [.. StatusLogEntries]));
        StatusText = result.Success
            ? "Status log copied to clipboard as markdown."
            : $"Copy failed: {result.ErrorMessage}";
    }

    private void AppendStatusLogEntry(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StatusLogEntries.Insert(0, new StatusLogEntry(DateTimeOffset.Now, message));
        while (StatusLogEntries.Count > 500)
            StatusLogEntries.RemoveAt(StatusLogEntries.Count - 1);
    }

    private void OnCurrentServerViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(ServerWorkspaceViewModel.StatusText), StringComparison.Ordinal))
            return;

        if (sender is not ServerWorkspaceViewModel workspace)
            return;

        if (!string.IsNullOrWhiteSpace(workspace.StatusText))
            StatusText = workspace.StatusText;
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
}
