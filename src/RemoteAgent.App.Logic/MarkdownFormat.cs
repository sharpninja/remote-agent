using System.Net;
using Markdig;

namespace RemoteAgent.App.Services;

/// <summary>Renders markdown or plain text to HTML for display in the chat (FR-2.3, TR-5.3). Agent output is parsed with Markdig; user and event text are escaped as plain paragraphs.</summary>
/// <remarks>Uses a minimal inline stylesheet so output is readable in a WebView. Error text is HTML-escaped only (no markdown).</remarks>
/// <example><code>
/// string html = MarkdownFormat.ToHtml("**bold** and `code`");  // agent output
/// string plain = MarkdownFormat.PlainToHtml("User said: hello");  // user or event
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements</see>
public static class MarkdownFormat
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private const string HtmlTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
        body { font-family: sans-serif; font-size: 14px; margin: 0; padding: 4px; color: #333; line-height: 1.5; }
        pre, code { background: #f5f5f5; border-radius: 4px; font-family: monospace; }
        pre { padding: 8px; overflow-x: auto; }
        code { padding: 2px 4px; }
        pre code { padding: 0; background: none; }
        ul, ol { margin: 0.25em 0; padding-left: 1.25em; }
        p { margin: 0.25em 0; }
        p:first-child { margin-top: 0; }
        p:last-child { margin-bottom: 0; }
        strong { font-weight: 600; }
        a { color: #1976d2; }
        blockquote { border-left: 3px solid #ccc; margin: 0.25em 0; padding-left: 0.75em; color: #555; }
        </style>
        </head>
        <body>
        BODY
        </body>
        </html>
        """;

    /// <summary>Converts markdown to HTML for agent output, or escapes plain text when <paramref name="isError"/> is true.</summary>
    /// <param name="markdown">Markdown source or error text.</param>
    /// <param name="isError">If true, content is HTML-escaped only (no markdown parsing).</param>
    /// <returns>Full HTML document fragment with inline styles, suitable for a WebView.</returns>
    public static string ToHtml(string? markdown, bool isError = false)
    {
        if (string.IsNullOrEmpty(markdown))
            return WrapBody("<p></p>");

        if (isError)
            return WrapBody($"<p>{WebUtility.HtmlEncode(markdown)}</p>");

        var html = Markdown.ToHtml(markdown, Pipeline);
        return WrapBody(string.IsNullOrEmpty(html) ? "<p></p>" : html);
    }

    /// <summary>Wraps plain text in a paragraph with HTML encoding. Use for user messages and session events (no markdown).</summary>
    /// <param name="text">Plain text to display.</param>
    /// <returns>Full HTML document fragment.</returns>
    public static string PlainToHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return WrapBody("<p></p>");
        return WrapBody($"<p>{WebUtility.HtmlEncode(text)}</p>");
    }

    private static string WrapBody(string body)
    {
        return HtmlTemplate.Replace("BODY", body);
    }
}
