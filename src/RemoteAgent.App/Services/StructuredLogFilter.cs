namespace RemoteAgent.App.Services;

/// <summary>Filtering options for structured log viewing.</summary>
public sealed class StructuredLogFilter
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Level { get; set; }
    public string? EventType { get; set; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Component { get; set; }
    public string? SearchText { get; set; }
    public string? SourceHost { get; set; }

    public bool Matches(StructuredLogRecord row)
    {
        if (FromUtc.HasValue && row.TimestampUtc < FromUtc.Value) return false;
        if (ToUtc.HasValue && row.TimestampUtc > ToUtc.Value) return false;
        if (!string.IsNullOrWhiteSpace(Level) && !StringEquals(row.Level, Level)) return false;
        if (!string.IsNullOrWhiteSpace(EventType) && !StringEquals(row.EventType, EventType)) return false;
        if (!string.IsNullOrWhiteSpace(SessionId) && !StringEquals(row.SessionId, SessionId)) return false;
        if (!string.IsNullOrWhiteSpace(CorrelationId) && !StringEquals(row.CorrelationId, CorrelationId)) return false;
        if (!string.IsNullOrWhiteSpace(Component) && !StringEquals(row.Component, Component)) return false;
        if (!string.IsNullOrWhiteSpace(SourceHost) && !StringEquals(row.SourceHost, SourceHost)) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            if (!Contains(row.Message, needle)
                && !Contains(row.DetailsJson, needle)
                && !Contains(row.EventType, needle)
                && !Contains(row.Component, needle)
                && !Contains(row.SessionId, needle)
                && !Contains(row.CorrelationId, needle))
                return false;
        }

        return true;
    }

    private static bool StringEquals(string? left, string? right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? source, string needle)
        => !string.IsNullOrEmpty(source)
           && source.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
