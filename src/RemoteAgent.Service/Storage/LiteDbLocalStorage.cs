using LiteDB;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Storage;

/// <summary>LiteDB implementation of local storage for requests/results (TR-11.1).</summary>
public sealed class LiteDbLocalStorage : ILocalStorage
{
    private readonly string _dbPath;
    private const string CollectionName = "request_results";

    public LiteDbLocalStorage(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "remote-agent.db");
    }

    public void LogRequest(string sessionId, string kind, string summary, string? mediaPath = null)
    {
        Insert(sessionId, isRequest: true, kind, summary, mediaPath);
    }

    public void LogResponse(string sessionId, string kind, string summary, string? mediaPath = null)
    {
        Insert(sessionId, isRequest: false, kind, summary, mediaPath);
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
