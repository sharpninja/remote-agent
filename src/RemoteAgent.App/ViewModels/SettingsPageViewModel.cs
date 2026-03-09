using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;

namespace RemoteAgent.App.ViewModels;

/// <summary>ViewModel for the Settings page — manages saved server profiles.</summary>
public sealed class SettingsPageViewModel : INotifyPropertyChanged
{
    private readonly IServerProfileStore _profileStore;
    private readonly IRequestDispatcher _dispatcher;
    private ServerProfile? _selectedProfile;
    private string _editDisplayName = "";
    private string _editPerRequestContext = "";
    private string _editDefaultSessionContext = "";
    private bool _hasApiKey;

    public SettingsPageViewModel(IServerProfileStore profileStore, IRequestDispatcher dispatcher)
    {
        _profileStore = profileStore;
        _dispatcher = dispatcher;
        SaveCommand = new Command(async () => await RunAsync(new SaveServerProfileRequest(Guid.NewGuid(), this)),
            () => _selectedProfile != null);
        DeleteCommand = new Command(async () => await RunAsync(new DeleteServerProfileRequest(Guid.NewGuid(), this)),
            () => _selectedProfile != null);
        ClearApiKeyCommand = new Command(async () => await RunAsync(new ClearServerApiKeyRequest(Guid.NewGuid(), this)),
            () => _selectedProfile != null && _hasApiKey);
        RefreshProfiles();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ServerProfile> Profiles { get; } = [];

    public ServerProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (_selectedProfile == value) return;
            _selectedProfile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            LoadSelectedProfile();
            ((Command)SaveCommand).ChangeCanExecute();
            ((Command)DeleteCommand).ChangeCanExecute();
        }
    }

    public bool HasSelection => _selectedProfile != null;

    public bool HasApiKey
    {
        get => _hasApiKey;
        set => Set(ref _hasApiKey, value);
    }

    public string EditDisplayName
    {
        get => _editDisplayName;
        set => Set(ref _editDisplayName, value);
    }

    public string EditPerRequestContext
    {
        get => _editPerRequestContext;
        set => Set(ref _editPerRequestContext, value);
    }

    public string EditDefaultSessionContext
    {
        get => _editDefaultSessionContext;
        set => Set(ref _editDefaultSessionContext, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearApiKeyCommand { get; }

    public void RefreshProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileStore.GetAll())
            Profiles.Add(p);
    }

    private void LoadSelectedProfile()
    {
        if (_selectedProfile == null)
        {
            EditDisplayName = "";
            EditPerRequestContext = "";
            EditDefaultSessionContext = "";
            HasApiKey = false;
            ((Command)ClearApiKeyCommand).ChangeCanExecute();
            return;
        }

        EditDisplayName = _selectedProfile.DisplayName;
        EditPerRequestContext = _selectedProfile.PerRequestContext;
        EditDefaultSessionContext = _selectedProfile.DefaultSessionContext;
        HasApiKey = !string.IsNullOrEmpty(_selectedProfile.ApiKey);
        ((Command)ClearApiKeyCommand).ChangeCanExecute();
    }

    private async Task RunAsync<TResponse>(IRequest<TResponse> request)
    {
        try
        {
            await _dispatcher.SendAsync(request);
        }
        catch
        {
            // Best effort — profile operations are local-only.
        }
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
