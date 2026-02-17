using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;
using RemoteAgent.Proto;
using Microsoft.Maui.Storage;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace RemoteAgent.App;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _vm;
#if WINDOWS
    private TextBox? _messageTextBox;
#endif

    public MainPage(MainPageViewModel vm)
    {
        _vm = vm;
        _vm.ConnectionModeSelector = SelectConnectionModeAsync;
        _vm.AgentSelector = ShowAgentPickerAsync;
        _vm.AttachmentPicker = PickAttachmentAsync;
        _vm.PromptTemplateSelector = SelectPromptTemplateAsync;
        _vm.PromptVariableValueProvider = PromptTemplateVariableValueAsync;
        _vm.SessionTerminationConfirmation = ConfirmSessionTerminationAsync;
        _vm.NotifyMessage += ShowNotificationForMessage;

        InitializeComponent();
        BindingContext = _vm;
        WireMessageInputShortcuts();
    }

    private void ShowNotificationForMessage(ChatMessage msg)
    {
        var body = msg.IsEvent ? (msg.EventMessage ?? "Event") : (msg.Text.Length > 200 ? msg.Text[..200] + "…" : msg.Text);
#if ANDROID
        PlatformNotificationService.ShowNotification("Remote Agent", body);
#endif
    }

    private async Task<string?> SelectConnectionModeAsync()
    {
        var choice = await DisplayActionSheetAsync("Connection mode", "Cancel", null, "Direct", "Server");
        if (string.IsNullOrWhiteSpace(choice) || string.Equals(choice, "Cancel", StringComparison.OrdinalIgnoreCase))
            return null;
        return string.Equals(choice, "Direct", StringComparison.OrdinalIgnoreCase) ? "direct" : "server";
    }

    private async Task<string?> ShowAgentPickerAsync(ServerInfoResponse serverInfo)
    {
        var agents = serverInfo.AvailableAgents.ToList();
        if (agents.Count == 0)
            return "";
        if (agents.Count == 1)
            return agents[0];
        var choice = await DisplayActionSheetAsync("Select agent", "Cancel", null, agents.ToArray());
        return string.IsNullOrEmpty(choice) ? null : choice;
    }

    private async Task<PickedAttachment?> PickAttachmentAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "image/*", "video/*" },
                [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick image or video",
                FileTypes = customFileType
            });
            if (result == null)
                return null;

            await using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return new PickedAttachment(ms.ToArray(), result.ContentType ?? "application/octet-stream", result.FileName ?? "attachment");
        }
        catch
        {
            return null;
        }
    }

    private async Task<PromptTemplateDefinition?> SelectPromptTemplateAsync(IReadOnlyList<PromptTemplateDefinition> templates)
    {
        if (templates.Count == 0) return null;
        if (templates.Count == 1) return templates[0];

        var labels = templates.Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.TemplateId : x.DisplayName).ToArray();
        var choice = await DisplayActionSheetAsync("Select prompt template", "Cancel", null, labels);
        if (string.IsNullOrWhiteSpace(choice) || string.Equals(choice, "Cancel", StringComparison.OrdinalIgnoreCase))
            return null;

        return templates.FirstOrDefault(t =>
            string.Equals(t.DisplayName, choice, StringComparison.Ordinal) ||
            string.Equals(t.TemplateId, choice, StringComparison.Ordinal));
    }

    private async Task<string?> PromptTemplateVariableValueAsync(string variable)
    {
        return await DisplayPromptAsync(
            title: "Template Input",
            message: $"Value for '{variable}'",
            accept: "Apply",
            cancel: "Cancel",
            initialValue: "",
            keyboard: Keyboard.Text);
    }

    private async Task<bool> ConfirmSessionTerminationAsync(string sessionLabel)
    {
        return await DisplayAlertAsync(
            "Terminate Session",
            $"Terminate '{sessionLabel}'? This removes the session from local history.",
            "Terminate",
            "Cancel");
    }

    private void WireMessageInputShortcuts()
    {
#if WINDOWS
        MessageEditor.HandlerChanged += OnMessageEditorHandlerChanged;
#endif
    }

#if WINDOWS
    private void OnMessageEditorHandlerChanged(object? sender, EventArgs e)
    {
        if (_messageTextBox != null)
            _messageTextBox.KeyDown -= OnMessageTextBoxKeyDown;

        _messageTextBox = MessageEditor?.Handler?.PlatformView as TextBox;
        if (_messageTextBox != null)
            _messageTextBox.KeyDown += OnMessageTextBoxKeyDown;
    }

    private void OnMessageTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var ctrlDown = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        if (!ctrlDown)
            return;

        e.Handled = true;
        if (_vm.SendMessageCommand.CanExecute(null))
            _vm.SendMessageCommand.Execute(null);
    }
#endif

    private void OnSessionTitleFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused && SessionTitleEntry.Text is { } t)
        {
            SessionTitleEntry.CursorPosition = 0;
            SessionTitleEntry.SelectionLength = t.Length;
        }
    }

    private void OnSessionTitleUnfocused(object? sender, FocusEventArgs e)
    {
        if (!e.IsFocused)
            CommitSessionTitle();
    }

    private void OnSessionTitleCompleted(object? sender, EventArgs e)
    {
        CommitSessionTitle();
    }

    private void CommitSessionTitle()
    {
        _vm.CommitSessionTitle(SessionTitleEntry.Text ?? "");
        SessionTitleEntry.IsVisible = false;
        SessionTitleLabel.IsVisible = true;
    }

    private void OnSessionTitleLabelTapped(object? sender, TappedEventArgs e)
    {
        if (_vm.CurrentSession == null) return;
        SessionTitleLabel.IsVisible = false;
        SessionTitleEntry.Text = _vm.CurrentSessionTitle;
        SessionTitleEntry.IsVisible = true;
        SessionTitleEntry.Focus();
    }

    public void StartNewSessionFromShell()
    {
        _vm.StartNewSession();
    }

    public void SelectSessionFromShell(string? sessionId)
    {
        _vm.SelectSession(sessionId);
    }

    public async Task<bool> TerminateSessionFromShellAsync(string? sessionId)
    {
        return await _vm.TerminateSessionByIdAsync(sessionId);
    }

    public string? GetCurrentSessionId() => _vm.CurrentSession?.SessionId;
}
