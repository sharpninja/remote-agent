using System.Globalization;

namespace RemoteAgent.App.Converters;

/// <summary>MAUI value converter: inverts a boolean (true → false, false → true). Use for visibility or enabled state when the binding is the opposite of the desired value.</summary>
/// <example><code>
/// IsVisible="{Binding IsBusy, Converter={StaticResource InverseBool}}"
/// </code></example>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>Returns the negation of the value when it is a bool; otherwise returns the value unchanged.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    /// <summary>Returns the negation of the value (same as Convert for bool).</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}
