using RemoteAgent.App.ViewModels;
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
        InitializeComponent();
        BindingContext = _vm;
        WireMessageInputShortcuts();
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
