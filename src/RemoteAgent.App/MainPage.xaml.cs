using System.Windows.Input;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;
using Microsoft.Maui.Storage;

namespace RemoteAgent.App;

public partial class MainPage : ContentPage
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string DefaultPort = "5243";

    private readonly ILocalMessageStore _store = new LocalMessageStore(Path.Combine(FileSystem.AppDataDirectory, "remote-agent.db"));
    private readonly AgentGatewayClientService _gateway;

    public MainPage()
    {
        _gateway = new AgentGatewayClientService(_store);
        InitializeComponent();
        LoadSavedServerDetails();
        _gateway.LoadFromStore();
        MessagesList.ItemsSource = _gateway.Messages;
        _gateway.ConnectionStateChanged += UpdateConnectionState;
        _gateway.MessageReceived += OnMessageReceived;
        UpdateConnectionState();
    }

    private void LoadSavedServerDetails()
    {
        var host = Preferences.Default.Get(PrefServerHost, "");
        var port = Preferences.Default.Get(PrefServerPort, DefaultPort);
        if (!string.IsNullOrEmpty(host))
            HostEntry.Text = host;
        PortEntry.Text = string.IsNullOrEmpty(port) ? DefaultPort : port;
    }

    private void SaveServerDetails(string host, int port)
    {
        Preferences.Default.Set(PrefServerHost, host ?? "");
        Preferences.Default.Set(PrefServerPort, port.ToString());
    }

    public ICommand ArchiveMessageCommand => new Command<ChatMessage>(msg =>
    {
        if (msg != null)
        {
            msg.IsArchived = true;
            _gateway.SetArchived(msg, true);
        }
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
        AttachBtn.IsEnabled = _gateway.IsConnected;
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
            SaveServerDetails(host, port);
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
        _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = text });
        MessageEntry.Text = "";
        try
        {
            if (TryParseScriptRun(text, out var pathOrCommand, out var scriptType))
                await _gateway.SendScriptRequestAsync(pathOrCommand, scriptType);
            else
                await _gateway.SendTextAsync(text);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }

    private async void OnAttachClicked(object? sender, EventArgs e)
    {
        if (!_gateway.IsConnected) return;
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "image/*", "video/*" },
                [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" }
            });
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Pick image or video", FileTypes = customFileType });
            if (result == null) return;
            await using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var content = ms.ToArray();
            var contentType = result.ContentType ?? "application/octet-stream";
            var fileName = result.FileName ?? "attachment";
            _gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = $"[Attachment: {fileName}]" });
            await _gateway.SendMediaAsync(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            _gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
        }
    }

    /// <summary>Parse /run bash &lt;path&gt; or /run pwsh &lt;path&gt; (FR-9.1).</summary>
    private static bool TryParseScriptRun(string text, out string pathOrCommand, out ScriptType scriptType)
    {
        pathOrCommand = "";
        scriptType = ScriptType.Bash;
        const string prefix = "/run ";
        if (text.Length <= prefix.Length || !text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = text[prefix.Length..].TrimStart();
        if (rest.StartsWith("bash ", StringComparison.OrdinalIgnoreCase))
        {
            scriptType = ScriptType.Bash;
            pathOrCommand = rest["bash ".Length..].Trim();
        }
        else if (rest.StartsWith("pwsh ", StringComparison.OrdinalIgnoreCase))
        {
            scriptType = ScriptType.Pwsh;
            pathOrCommand = rest["pwsh ".Length..].Trim();
        }
        else
        {
            pathOrCommand = rest;
        }
        return pathOrCommand.Length > 0;
    }
}
