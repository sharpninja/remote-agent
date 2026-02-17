using System.Text.Json.Serialization;

namespace RemoteAgent.Service.Logging;

/// <summary>Structured operational log event written as one JSON object per line.</summary>
public sealed class StructuredLogEntryRecord
{
    [JsonPropertyName("event_id")]
    public long EventId { get; set; }

    [JsonPropertyName("timestamp_utc")]
    public DateTimeOffset TimestampUtc { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; } = "INFO";

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = "event";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("component")]
    public string Component { get; set; } = "";

    // Always present in JSON payload. Null means unavailable for this event.
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    // Always present in JSON payload. Null means unavailable for this event.
    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("details_json")]
    public string? DetailsJson { get; set; }
}
