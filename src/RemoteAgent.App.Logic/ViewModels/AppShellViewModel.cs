using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.App.Logic.ViewModels;

public sealed class AppShellViewModel : INotifyPropertyChanged
{
    private readonly ISessionListProvider _sessionListProvider;
    private readonly ISessionCommandBus _sessionBus;
    private readonly INavigationService _navigation;

    public AppShellViewModel(
        ISessionListProvider sessionListProvider,
        ISessionCommandBus sessionBus,
        INavigationService navigation)
    {
        _sessionListProvider = sessionListProvider;
        _sessionBus = sessionBus;
        _navigation = navigation;

        StartSessionCommand = new RelayCommand(() => _ = StartSessionAsync());
        SelectSessionCommand = new RelayCommand<string>(sid => _ = SelectSessionAsync(sid));
        TerminateSessionCommand = new RelayCommand<string>(sid => _ = TerminateSessionAsync(sid));
        NavigateToSettingsCommand = new RelayCommand(() => _ = NavigateAsync("//SettingsPage"));
        NavigateToAccountCommand = new RelayCommand(() => _ = NavigateAsync("//AccountManagementPage"));
        OpenSessionsCommand = new RelayCommand(() => _ = OpenSessionsAsync());

        RefreshSessions();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SessionSummary> SessionItems { get; } = [];

    public ICommand StartSessionCommand { get; }
    public ICommand SelectSessionCommand { get; }
    public ICommand TerminateSessionCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToAccountCommand { get; }
    public ICommand OpenSessionsCommand { get; }

    public void RefreshSessions()
    {
        SessionItems.Clear();
        foreach (var s in _sessionListProvider.GetSessions())
            SessionItems.Add(s);
    }

    private async Task StartSessionAsync()
    {
        _sessionBus.StartNewSession();
        RefreshSessions();
        _navigation.CloseFlyout();
        await _navigation.NavigateToAsync("//MainPage");
    }

    private async Task SelectSessionAsync(string? sessionId)
    {
        _sessionBus.SelectSession(sessionId);
        _navigation.CloseFlyout();
        await _navigation.NavigateToAsync("//MainPage");
    }

    private async Task TerminateSessionAsync(string? sessionId)
    {
        await _sessionBus.TerminateSessionAsync(sessionId);
        RefreshSessions();
    }

    private async Task OpenSessionsAsync()
    {
        RefreshSessions();
        _sessionBus.SelectSession(_sessionBus.GetCurrentSessionId());
        _navigation.CloseFlyout();
        await _navigation.NavigateToAsync("//MainPage");
    }

    private async Task NavigateAsync(string route)
    {
        _navigation.CloseFlyout();
        await _navigation.NavigateToAsync(route);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
