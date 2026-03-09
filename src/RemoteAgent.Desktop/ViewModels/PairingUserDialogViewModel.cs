using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.ViewModels;

/// <summary>ViewModel for the Set Pairing User dialog.</summary>
public sealed class PairingUserDialogViewModel : INotifyPropertyChanged
{
    private string _username = "";
    private string _password = "";
    private string _validationMessage = "";
    private bool _isAccepted;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool>? RequestClose;

    public PairingUserDialogViewModel()
    {
        SubmitCommand = new RelayCommand(Submit);
        CancelCommand = new RelayCommand(Cancel);
    }

    public string Username
    {
        get => _username;
        set { if (_username != value) { _username = value; OnPropertyChanged(); } }
    }

    public string Password
    {
        get => _password;
        set { if (_password != value) { _password = value; OnPropertyChanged(); } }
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

    private void Submit()
    {
        var username = (Username ?? "").Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            ValidationMessage = "Username is required.";
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            ValidationMessage = "Password is required.";
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
