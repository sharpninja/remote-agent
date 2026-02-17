using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IServerRegistrationStore _serverRegistrationStore;
    private readonly IServerWorkspaceFactory _serverWorkspaceFactory;
    private readonly Dictionary<string, ServerWorkspaceLease> _workspaceLeases = [];

    private ServerRegistration? _selectedServer;
    private ServerWorkspaceViewModel? _currentServerViewModel;
    private string _currentServerId = "";
    private string _statusText = "Ready.";

    private string _editDisplayName = "";
    private string _editHost = "127.0.0.1";
    private string _editPort = "5243";
    private string _editApiKey = "";

    public MainWindowViewModel(
        IServerRegistrationStore serverRegistrationStore,
        IServerWorkspaceFactory serverWorkspaceFactory)
    {
        _serverRegistrationStore = serverRegistrationStore;
        _serverWorkspaceFactory = serverWorkspaceFactory;

        NewServerCommand = new RelayCommand(NewServerDraft);
        SaveServerCommand = new RelayCommand(SaveServer);
        RemoveServerCommand = new RelayCommand(RemoveSelectedServer, () => SelectedServer != null);

        LoadServers();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ServerRegistration> Servers { get; } = [];

    public ServerRegistration? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (_selectedServer == value) return;
            _selectedServer = value;
            OnPropertyChanged();
            ((RelayCommand)RemoveServerCommand).RaiseCanExecuteChanged();
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
            _currentServerViewModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCurrentServer));
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

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
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

    public ICommand NewServerCommand { get; }
    public ICommand SaveServerCommand { get; }
    public ICommand RemoveServerCommand { get; }

    public void Dispose()
    {
        foreach (var lease in _workspaceLeases.Values)
            lease.Dispose();
        _workspaceLeases.Clear();
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

    private void SaveServer()
    {
        var host = (EditHost ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Server host is required.";
            return;
        }

        if (!int.TryParse((EditPort ?? "").Trim(), out var port) || port <= 0 || port > 65535)
        {
            StatusText = "Server port must be 1-65535.";
            return;
        }

        var current = SelectedServer;
        var registration = new ServerRegistration
        {
            ServerId = current?.ServerId ?? Guid.NewGuid().ToString("N"),
            DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? $"{host}:{port}" : EditDisplayName.Trim(),
            Host = host,
            Port = port,
            ApiKey = EditApiKey ?? ""
        };

        var saved = _serverRegistrationStore.Upsert(registration);

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

    private void RemoveSelectedServer()
    {
        var selected = SelectedServer;
        if (selected == null)
            return;

        if (!_serverRegistrationStore.Delete(selected.ServerId))
        {
            StatusText = $"Failed to remove server '{selected.DisplayName}'.";
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
