using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class AuthUsersViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private AuthUserSnapshot? _selectedAuthUser;
    private string _authUserId = "";
    private string _authDisplayName = "";
    private string _authRole = "viewer";
    private bool _authEnabled = true;
    private string _statusText = "";

    public AuthUsersViewModel(IRequestDispatcher dispatcher, IServerConnectionContext context)
    {
        _dispatcher = dispatcher;
        _context = context;

        RefreshAuthUsersCommand = new RelayCommand(
            () => _ = RunCommandAsync("Refreshing auth users...", RefreshAuthUsersAsync));
        SaveAuthUserCommand = new RelayCommand(
            () => _ = RunCommandAsync("Saving auth user...", SaveAuthUserAsync));
        DeleteAuthUserCommand = new RelayCommand(
            () => _ = RunCommandAsync("Deleting auth user...", DeleteSelectedAuthUserAsync),
            () => SelectedAuthUser != null);

        ObserveBackgroundTask(RefreshAuthUsersAsync(), "initial auth users refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AuthUserSnapshot> AuthUsers { get; } = [];
    public ObservableCollection<string> PermissionRoles { get; } = [];

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

    public ICommand RefreshAuthUsersCommand { get; }
    public ICommand SaveAuthUserCommand { get; }
    public ICommand DeleteAuthUserCommand { get; }

    private async Task RefreshAuthUsersAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshAuthUsersRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task SaveAuthUserAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        var payload = new AuthUserSnapshot(
            UserId: AuthUserId,
            DisplayName: AuthDisplayName,
            Role: string.IsNullOrWhiteSpace(AuthRole) ? "viewer" : AuthRole,
            Enabled: AuthEnabled,
            CreatedUtc: DateTimeOffset.UtcNow,
            UpdatedUtc: DateTimeOffset.UtcNow);
        await _dispatcher.SendAsync(new SaveAuthUserRequest(Guid.NewGuid(), host, port, payload, _context.ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedAuthUserAsync()
    {
        var selected = SelectedAuthUser;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { StatusText = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { StatusText = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new DeleteAuthUserRequest(Guid.NewGuid(), host, port, selected.UserId, _context.ApiKey, Workspace: this));
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
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
