using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.ViewModels;

/// <summary>ViewModel for the Settings page — manages saved server profiles.</summary>
public sealed class SettingsPageViewModel : INotifyPropertyChanged
{
    private readonly IServerProfileStore _profileStore;
    private ServerProfile? _selectedProfile;
    private string _editDisplayName = "";
    private string _editPerRequestContext = "";
    private string _editDefaultSessionContext = "";

    public SettingsPageViewModel(IServerProfileStore profileStore)
    {
        _profileStore = profileStore;
        SaveCommand = new Command(Save, () => _selectedProfile != null);
        DeleteCommand = new Command(Delete, () => _selectedProfile != null);
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
            return;
        }

        EditDisplayName = _selectedProfile.DisplayName;
        EditPerRequestContext = _selectedProfile.PerRequestContext;
        EditDefaultSessionContext = _selectedProfile.DefaultSessionContext;
    }

    private void Save()
    {
        if (_selectedProfile == null) return;
        _selectedProfile.DisplayName = EditDisplayName;
        _selectedProfile.PerRequestContext = EditPerRequestContext;
        _selectedProfile.DefaultSessionContext = EditDefaultSessionContext;
        _profileStore.Upsert(_selectedProfile);
        RefreshProfiles();
    }

    private void Delete()
    {
        if (_selectedProfile == null) return;
        _profileStore.Delete(_selectedProfile.Host, _selectedProfile.Port);
        SelectedProfile = null;
        RefreshProfiles();
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
