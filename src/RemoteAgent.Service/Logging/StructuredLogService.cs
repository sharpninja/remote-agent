using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Logging;

/// <summary>Writes structured JSONL logs in real time and exposes snapshot/stream readers.</summary>
public sealed class StructuredLogService : IDisposable
{
    private readonly ILogger<StructuredLogService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private long _nextEventId;

    public StructuredLogService(IOptions<AgentOptions> options, ILogger<StructuredLogService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        var logDir = options.Value.LogDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(logDir))
            logDir = Path.Combine(AppContext.BaseDirectory, "logs");

        Directory.CreateDirectory(logDir);
        FilePath = Path.Combine(logDir, "remote-agent-structured.jsonl");
        _nextEventId = ReadLastEventId(FilePath);

        var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
    }

    /// <summary>Full file path to structured JSONL log output.</summary>
    public string FilePath { get; }

    /// <summary>Current highest event id seen by this process.</summary>
    public long LatestOffset => Interlocked.Read(ref _nextEventId);

    /// <summary>Writes a single structured event to disk and returns its event id.</summary>
    public long Write(
        string level,
        string eventType,
        string message,
        string component,
        string? sessionId = null,
        string? correlationId = null,
        string? detailsJson = null)
    {
        var eventId = Interlocked.Increment(ref _nextEventId);
        var entry = new StructuredLogEntryRecord
        {
            EventId = eventId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = string.IsNullOrWhiteSpace(level) ? "INFO" : level.ToUpperInvariant(),
            EventType = string.IsNullOrWhiteSpace(eventType) ? "event" : eventType,
            Message = message ?? "",
            Component = component ?? "",
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId,
            DetailsJson = detailsJson
        };

        var line = JsonSerializer.Serialize(entry, _jsonOptions);
        lock (_sync)
        {
            _writer.WriteLine(line);
        }

        return eventId;
    }

    /// <summary>Reads historical rows from disk with optional cursor and limit.</summary>
    public IReadOnlyList<StructuredLogEntryRecord> GetSnapshot(long fromOffset, int limit)
    {
        var max = limit <= 0 ? int.MaxValue : limit;
        var rows = new List<StructuredLogEntryRecord>(limit > 0 ? Math.Min(limit, 5000) : 5000);
        if (!File.Exists(FilePath))
            return rows;

        try
        {
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var row = ParseLine(line);
                if (row == null || row.EventId <= fromOffset)
                    continue;
                rows.Add(row);
                if (rows.Count >= max)
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed reading structured log snapshot");
        }

        return rows;
    }

    /// <summary>Tails the JSONL file and yields new rows after <paramref name="fromOffset"/>.</summary>
    public async IAsyncEnumerable<StructuredLogEntryRecord> StreamFromOffset(
        long fromOffset,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var currentOffset = fromOffset;
        using var stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
            {
                await Task.Delay(200, ct);
                continue;
            }

            var row = ParseLine(line);
            if (row == null || row.EventId <= currentOffset)
                continue;

            currentOffset = row.EventId;
            yield return row;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer.Dispose();
        }
    }

    private StructuredLogEntryRecord? ParseLine(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<StructuredLogEntryRecord>(line, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static long ReadLastEventId(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;

        try
        {
            long last = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                using var doc = JsonDocument.Parse(line);
                if (!doc.RootElement.TryGetProperty("event_id", out var idElement))
                    continue;
                if (idElement.ValueKind != JsonValueKind.Number)
                    continue;
                if (idElement.TryGetInt64(out var id) && id > last)
                    last = id;
            }
            return last;
        }
        catch
        {
            return 0;
        }
    }
}
