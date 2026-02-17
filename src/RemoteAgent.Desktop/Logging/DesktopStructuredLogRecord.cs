namespace RemoteAgent.Desktop.Logging;

public sealed class DesktopStructuredLogRecord
{
    public string Id { get; set; } = "";
    public string ServerId { get; set; } = "";
    public string ServerDisplayName { get; set; } = "";
    public long EventId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public string Level { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Message { get; set; } = "";
    public string Component { get; set; } = "";
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? DetailsJson { get; set; }
    public string SourceHost { get; set; } = "";
    public int SourcePort { get; set; }
}
