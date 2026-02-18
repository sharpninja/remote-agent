// Minimal stubs for Microsoft.Maui.Controls types referenced by MainPageViewModel.
// This lets the MAUI ViewModel be compiled and tested in a plain net10.0 test project.
namespace Microsoft.Maui.Controls;

public class Command : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public Command(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void ChangeCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class Command<T> : System.Windows.Input.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public Command(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public void ChangeCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
