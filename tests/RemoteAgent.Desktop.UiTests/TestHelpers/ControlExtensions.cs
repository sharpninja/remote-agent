using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace RemoteAgent.Desktop.UiTests.TestHelpers;

/// <summary>
/// Test-only helpers for finding controls across UserControl NameScope boundaries.
/// </summary>
internal static class ControlExtensions
{
    /// <summary>
    /// Recursively searches all logical descendants (crossing UserControl NameScope
    /// boundaries) for the first control of type <typeparamref name="T"/> with the
    /// given <paramref name="name"/>.
    /// </summary>
    public static T? FindControlDeep<T>(this Control root, string name) where T : Control
        => root.GetLogicalDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);
}
