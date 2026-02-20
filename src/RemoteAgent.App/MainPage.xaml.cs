using System.ComponentModel;
using RemoteAgent.App.ViewModels;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
#endif
#if ANDROID
using AndroidX.AppCompat.Widget;
#endif

namespace RemoteAgent.App;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _vm;
#if WINDOWS
    private TextBox? _messageTextBox;
#endif
#if ANDROID
    private AppCompatEditText? _androidEditText;
#endif

    public MainPage(MainPageViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        BindingContext = _vm;
        WireMessageInputShortcuts();
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.IsEditingTitle) && _vm.IsEditingTitle)
            SessionTitleEntry.Focus();
    }

    private void WireMessageInputShortcuts()
    {
#if WINDOWS
        MessageEditor.HandlerChanged += OnMessageEditorHandlerChanged;
#endif
#if ANDROID
        MessageEditor.HandlerChanged += OnMessageEditorHandlerChangedAndroid;
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

#if ANDROID
    private void OnMessageEditorHandlerChangedAndroid(object? sender, EventArgs e)
    {
        if (_androidEditText != null)
            _androidEditText.EditorAction -= OnAndroidEditorAction;

        _androidEditText = MessageEditor?.Handler?.PlatformView as AppCompatEditText;
        if (_androidEditText != null)
        {
            // Multi-line EditText ignores ImeOptions by default (keyboard shows Enter).
            // SetRawInputType overrides the IME input type without changing the view's
            // multi-line wrapping behaviour, so the keyboard shows a Send action button.
            _androidEditText.SetRawInputType(Android.Text.InputTypes.ClassText);
            _androidEditText.ImeOptions = Android.Views.InputMethods.ImeAction.Send;
            _androidEditText.SetImeActionLabel("Send", Android.Views.InputMethods.ImeAction.Send);
            _androidEditText.EditorAction += OnAndroidEditorAction;
        }
    }

    private void OnAndroidEditorAction(object? sender, Android.Widget.TextView.EditorActionEventArgs e)
    {
        if (e.ActionId == Android.Views.InputMethods.ImeAction.Send ||
            e.ActionId == Android.Views.InputMethods.ImeAction.Done ||
            e.ActionId == Android.Views.InputMethods.ImeAction.Unspecified)
        {
            e.Handled = true;
            if (_vm.SendMessageCommand.CanExecute(null))
                _vm.SendMessageCommand.Execute(null);
            return;
        }
        e.Handled = false;
    }
#endif

    private void CommitSessionTitle()
    {
        _vm.CommitSessionTitle(SessionTitleEntry.Text ?? "");
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
}
