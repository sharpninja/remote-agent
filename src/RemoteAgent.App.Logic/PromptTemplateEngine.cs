using System.Text.RegularExpressions;
using HandlebarsDotNet;

namespace RemoteAgent.App.Logic;

public static class PromptTemplateEngine
{
    private static readonly Regex PlaceholderRegex = new(@"{{\s*([a-zA-Z0-9_.-]+)\s*}}", RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractVariables(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) return [];
        return PlaceholderRegex
            .Matches(template)
            .Select(m => m.Groups[1].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string Render(string template, IReadOnlyDictionary<string, string?> data)
    {
        var compiled = Handlebars.Compile(template ?? string.Empty);
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in data)
            payload[kvp.Key] = kvp.Value ?? string.Empty;
        return compiled(payload);
    }
}
