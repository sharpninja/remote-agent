using System.Windows.Input;
using RemoteAgent.App.Services;

namespace RemoteAgent.App;

public partial class MainPage : ContentPage
{
    private readonly AgentGatewayClientService _gateway = new();

    public MainPage()
    {
        InitializeComponent();
        MessagesList.ItemsSource = _gateway.Messages;
        _gateway.ConnectionStateChanged += UpdateConnectionState;
        _gateway.MessageReceived += OnMessageReceived;
        UpdateConnectionState();
    }

    public ICommand ArchiveMessageCommand => new Command<ChatMessage>(msg =>
    {
        if (msg != null) msg.IsArchived = true;
    });

    private void OnMessageReceived(ChatMessage msg)
    {
        if (msg.Priority != ChatMessagePriority.Notify) return;
        ShowNotificationForMessage(msg);
    }

    private void ShowNotificationForMessage(ChatMessage msg)
    {
        var title = "Remote Agent";
        var body = msg.IsEvent ? (msg.EventMessage ?? "Event") : (msg.Text.Length > 200 ? msg.Text[..200] + "…" : msg.Text);
#if ANDROID
        PlatformNotificationService.ShowNotification(title, body);
#endif
    }

    private void UpdateConnectionState()
    {
        ConnectBtn.IsEnabled = !_gateway.IsConnected;
        DisconnectBtn.IsEnabled = _gateway.IsConnected;
        SendBtn.IsEnabled = _gateway.IsConnected;
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
        StatusLabel.Text = "Connecting...";
        try
        {
            await _gateway.ConnectAsync(host, port);
            StatusLabel.Text = "Connected.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Failed: {ex.Message}";
        }
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        _gateway.Disconnect();
        UpdateConnectionState();
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
        _gateway.Messages.Add(new ChatMessage { IsUser = true, Text = text });
        MessageEntry.Text = "";
        try
        {
            await _gateway.SendTextAsync(text);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }
}
