using LiteDB;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Storage;

/// <summary>LiteDB implementation of local storage for requests and results (TR-11.1). Database file is under <see cref="AgentOptions.DataDirectory"/>.</summary>
/// <remarks>Uses collection <c>request_results</c>. Summaries longer than 2000 characters are truncated. Failures are ignored so logging does not break the request pipeline.</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public sealed class LiteDbLocalStorage : ILocalStorage
{
    private readonly string _dbPath;
    private const string CollectionName = "request_results";

    public string DbPath => _dbPath;

    /// <summary>Creates storage using <see cref="AgentOptions.DataDirectory"/> (defaults to <c>./data</c>). Database file: <c>remote-agent.db</c>.</summary>
    public LiteDbLocalStorage(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "remote-agent.db");
    }

    /// <inheritdoc />
    public void LogRequest(string sessionId, string kind, string summary, string? mediaPath = null)
    {
        Insert(sessionId, isRequest: true, kind, summary, mediaPath);
    }

    /// <inheritdoc />
    public void LogResponse(string sessionId, string kind, string summary, string? mediaPath = null)
    {
        Insert(sessionId, isRequest: false, kind, summary, mediaPath);
    }

    /// <inheritdoc />
    public bool SessionExists(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<RequestResultRecord>(CollectionName);
            return col.Exists(x => x.SessionId == sessionId.Trim());
        }
        catch
        {
            return false;
        }
    }

    private void Insert(string sessionId, bool isRequest, string kind, string summary, string? mediaPath)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<RequestResultRecord>(CollectionName);
            col.Insert(new RequestResultRecord
            {
                SessionId = sessionId,
                IsRequest = isRequest,
                Kind = kind,
                Summary = summary.Length > 2000 ? summary[..2000] + "…" : summary,
                Timestamp = DateTimeOffset.UtcNow,
                MediaPath = mediaPath
            });
        }
        catch
        {
            // best-effort; do not fail the request
        }
    }
}
