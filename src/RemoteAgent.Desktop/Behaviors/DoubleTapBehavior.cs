using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace RemoteAgent.Desktop.Behaviors;

/// <summary>
/// Attached behavior that invokes a command on double-click (PointerPressed with ClickCount == 2).
/// </summary>
public static class DoubleTapBehavior
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<Control, ICommand?>(
            "Command", typeof(DoubleTapBehavior));

    static DoubleTapBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    public static ICommand? GetCommand(Control control) => control.GetValue(CommandProperty);
    public static void SetCommand(Control control, ICommand? value) => control.SetValue(CommandProperty, value);

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.PointerPressed -= OnPointerPressed;

        if (e.NewValue is ICommand)
            control.PointerPressed += OnPointerPressed;
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        if (sender is not Control control)
            return;

        var command = GetCommand(control);
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }
}
