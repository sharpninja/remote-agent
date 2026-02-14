using System.Globalization;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Converters;

public class ChatMessageToHtmlSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return null;
        return new HtmlWebViewSource { Html = msg.RenderedHtml };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
