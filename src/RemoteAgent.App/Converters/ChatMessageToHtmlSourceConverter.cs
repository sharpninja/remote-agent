using System.Globalization;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Converters;

/// <summary>MAUI value converter: binds a <see cref="ChatMessage"/> to an <see cref="HtmlWebViewSource"/> for display in a WebView (FR-2.3, TR-5.3).</summary>
/// <remarks>Uses <see cref="ChatMessage.RenderedHtml"/> (markdown-rendered for agent output, plain for user/event). Use in the chat item template.</remarks>
/// <example><code>
/// &lt;WebView&gt;
///   &lt;WebView.Source&gt;
///     &lt;HtmlWebViewSource Html="{Binding RenderedHtml}"/&gt;  // or use converter with Binding to ChatMessage
///   &lt;/WebView.Source&gt;
/// &lt;/WebView&gt;
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-2)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-5)</see>
public class ChatMessageToHtmlSourceConverter : IValueConverter
{
    /// <summary>Returns an <see cref="HtmlWebViewSource"/> with <see cref="ChatMessage.RenderedHtml"/> when value is a <see cref="ChatMessage"/>.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return null;
        return new HtmlWebViewSource { Html = msg.RenderedHtml };
    }

    /// <summary>Not used (one-way binding).</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
