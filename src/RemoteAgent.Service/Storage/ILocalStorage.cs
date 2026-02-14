namespace RemoteAgent.Service.Storage;

/// <summary>Local storage of requests and results (TR-11.1).</summary>
public interface ILocalStorage
{
    void LogRequest(string sessionId, string kind, string summary, string? mediaPath = null);
    void LogResponse(string sessionId, string kind, string summary, string? mediaPath = null);
}
