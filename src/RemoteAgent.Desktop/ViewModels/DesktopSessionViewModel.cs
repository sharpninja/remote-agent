using System.Collections.ObjectModel;
using System.ComponentModel;
using RemoteAgent.App.Logic;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class DesktopSessionViewModel(IAgentSessionClient sessionClient) : INotifyPropertyChanged
{
    private string _title = "New Session";
    private string _pendingMessage = "";
    private bool _isConnected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IAgentSessionClient SessionClient { get; } = sessionClient;

    public string SessionId { get; set; } = Guid.NewGuid().ToString("N")[..12];

    public string ConnectionMode { get; set; } = "server";

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public string AgentId { get; set; } = "";

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value) return;
            _isConnected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }
    }

    public string PendingMessage
    {
        get => _pendingMessage;
        set
        {
            if (_pendingMessage == value) return;
            _pendingMessage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingMessage)));
        }
    }

    public ObservableCollection<string> Messages { get; } = [];
}
