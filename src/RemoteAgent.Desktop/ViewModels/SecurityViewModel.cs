using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class SecurityViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private OpenServerSessionSnapshot? _selectedOpenServerSession;
    private ConnectedPeerSnapshot? _selectedConnectedPeer;
    private BannedPeerSnapshot? _selectedBannedPeer;
    private string _banReason = "";
    private string _statusText = "";

    public SecurityViewModel(IRequestDispatcher dispatcher, IServerConnectionContext context)
    {
        _dispatcher = dispatcher;
        _context = context;

        RefreshSecurityDataCommand = new RelayCommand(

            () => _ = RunCommandAsync("Refreshing security data...", RefreshSecurityDataAsync));
        BanSelectedPeerCommand = new RelayCommand(
            () => _ = RunCommandAsync("Banning peer...", BanSelectedPeerAsync),
            () => SelectedConnectedPeer != null);
        UnbanSelectedPeerCommand = new RelayCommand(
            () => _ = RunCommandAsync("Unbanning peer...", UnbanSelectedPeerAsync),
            () => SelectedBannedPeer != null);
        RefreshOpenSessionsCommand = new RelayCommand(
            () => _ = RunCommandAsync("Refreshing open sessions...", RefreshOpenSessionsAsync));
        TerminateOpenServerSessionCommand = new RelayCommand(
            () => _ = RunCommandAsync("Terminating open session...", TerminateSelectedOpenServerSessionAsync),
            () => SelectedOpenServerSession != null);

        ObserveBackgroundTask(RefreshOpenSessionsAsync(), "initial open sessions refresh");
        ObserveBackgroundTask(RefreshSecurityDataAsync(), "initial security refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OpenServerSessionSnapshot> OpenServerSessions { get; } = [];
    public ObservableCollection<AbandonedServerSessionSnapshot> AbandonedServerSessions { get; } = [];
    public ObservableCollection<ConnectedPeerSnapshot> ConnectedPeers { get; } = [];
    public ObservableCollection<ConnectionHistorySnapshot> ConnectionHistory { get; } = [];
    public ObservableCollection<BannedPeerSnapshot> BannedPeers { get; } = [];

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

    public ICommand RefreshSecurityDataCommand { get; }
    public ICommand BanSelectedPeerCommand { get; }
    public ICommand UnbanSelectedPeerCommand { get; }
    public ICommand RefreshOpenSessionsCommand { get; }
    public ICommand TerminateOpenServerSessionCommand { get; }

    private async Task RefreshSecurityDataAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new RefreshSecurityDataRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task RefreshOpenSessionsAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new RefreshOpenSessionsRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task TerminateSelectedOpenServerSessionAsync()
    {
        var selected = SelectedOpenServerSession;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new TerminateOpenServerSessionRequest(Guid.NewGuid(), host, port, selected.SessionId, _context.ApiKey, Workspace: this));
    }

    private async Task BanSelectedPeerAsync()
    {
        var selected = SelectedConnectedPeer;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new BanPeerRequest(Guid.NewGuid(), host, port, selected.Peer, BanReason, _context.ApiKey, Workspace: this));
    }

    private async Task UnbanSelectedPeerAsync()
    {
        var selected = SelectedBannedPeer;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new UnbanPeerRequest(Guid.NewGuid(), host, port, selected.Peer, _context.ApiKey, Workspace: this));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void ObserveBackgroundTask(Task task, string operation)
    {
        _ = task.ContinueWith(
            completed =>
            {
                if (completed.IsCanceled || completed.Exception == null)
                    return;
                // Silently ignore initial-load failures; the user can refresh manually.
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
