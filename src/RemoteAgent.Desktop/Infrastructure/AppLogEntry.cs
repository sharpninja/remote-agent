using Microsoft.Extensions.Logging;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Represents a single captured log entry from the management app's ILogger pipeline.</summary>
public sealed record AppLogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    string? ExceptionMessage);
