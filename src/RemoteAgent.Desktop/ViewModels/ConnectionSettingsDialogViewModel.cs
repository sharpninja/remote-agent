using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Views;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class ConnectionSettingsDialogViewModel : INotifyPropertyChanged
{
    private string _host = "";
    private string _port = ServiceDefaults.PortString;
    private string _selectedConnectionMode = "";
    private string _selectedAgentId = "";
    private string _apiKey = "";
    private string _perRequestContext = "";
    private string _validationMessage = "";
    private bool _isAccepted;

    public ConnectionSettingsDialogViewModel(ConnectionSettingsDefaults defaults)
    {
        Host = defaults.Host;
        Port = defaults.Port;
        SelectedConnectionMode = defaults.ConnectionModes.Contains(defaults.SelectedConnectionMode, StringComparer.OrdinalIgnoreCase)
            ? defaults.SelectedConnectionMode
            : (defaults.ConnectionModes.Count > 0 ? defaults.ConnectionModes[0] : "");
        SelectedAgentId = defaults.SelectedAgentId;
        ApiKey = defaults.ApiKey;
        PerRequestContext = defaults.PerRequestContext;
        ConnectionModes = new ObservableCollection<string>(defaults.ConnectionModes);

        SubmitCommand = new RelayCommand(Submit);
        CancelCommand = new RelayCommand(Cancel);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool>? RequestClose;

    public ObservableCollection<string> ConnectionModes { get; }

    public string Host
    {
        get => _host;
        set { if (_host != value) { _host = value; OnPropertyChanged(); } }
    }

    public string Port
    {
        get => _port;
        set { if (_port != value) { _port = value; OnPropertyChanged(); } }
    }

    public string SelectedConnectionMode
    {
        get => _selectedConnectionMode;
        set { if (_selectedConnectionMode != value) { _selectedConnectionMode = value; OnPropertyChanged(); } }
    }

    public string SelectedAgentId
    {
        get => _selectedAgentId;
        set { if (_selectedAgentId != value) { _selectedAgentId = value; OnPropertyChanged(); } }
    }

    public string ApiKey
    {
        get => _apiKey;
        set { if (_apiKey != value) { _apiKey = value; OnPropertyChanged(); } }
    }

    public string PerRequestContext
    {
        get => _perRequestContext;
        set { if (_perRequestContext != value) { _perRequestContext = value; OnPropertyChanged(); } }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set { if (_validationMessage != value) { _validationMessage = value; OnPropertyChanged(); } }
    }

    public bool IsAccepted
    {
        get => _isAccepted;
        private set { if (_isAccepted != value) { _isAccepted = value; OnPropertyChanged(); } }
    }

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }

    public ConnectionSettingsDialogResult ToResult() =>
        new(Host.Trim(), Port.Trim(), SelectedConnectionMode.Trim(),
            SelectedAgentId.Trim(), ApiKey.Trim(), PerRequestContext.Trim());

    private void Submit()
    {
        var host = (Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            ValidationMessage = "Host is required.";
            return;
        }

        if (!int.TryParse((Port ?? "").Trim(), out var port) || port is <= 0 or > 65535)
        {
            ValidationMessage = "Port must be 1-65535.";
            return;
        }

        var mode = (SelectedConnectionMode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(mode))
        {
            ValidationMessage = "Mode is required.";
            return;
        }

        var agentId = (SelectedAgentId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            ValidationMessage = "Agent is required.";
            return;
        }

        ValidationMessage = "";
        IsAccepted = true;
        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        IsAccepted = false;
        RequestClose?.Invoke(false);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
