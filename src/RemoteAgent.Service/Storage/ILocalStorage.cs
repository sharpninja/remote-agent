namespace RemoteAgent.Service.Storage;

/// <summary>Local storage of requests and results for history and replay (TR-11.1).</summary>
/// <remarks>Implemented by <see cref="LiteDbLocalStorage"/>. Each request/response (text, control, script, media, output, error, event) is logged with session id, kind, and a truncated summary.</remarks>
/// <example><code>
/// localStorage.LogRequest(sessionId, "Text", msg.Text);
/// localStorage.LogResponse(sessionId, "Output", stdout);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public interface ILocalStorage
{
    /// <summary>Full path to the LiteDB database file used for request/response history (TR-11.1).</summary>
    string DbPath { get; }

    /// <summary>Logs a client request (e.g. text message, control, script request, media upload).</summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="kind">Kind of request (e.g. Text, Control, ScriptRequest, MediaUpload).</param>
    /// <param name="summary">Short summary or content (truncated if long).</param>
    /// <param name="mediaPath">Optional relative path to stored media (TR-11.2).</param>
    void LogRequest(string sessionId, string kind, string summary, string? mediaPath = null);

    /// <summary>Logs a server/agent response (e.g. output, error, event).</summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="kind">Kind of response (e.g. Output, Error, Event).</param>
    /// <param name="summary">Short summary or content.</param>
    /// <param name="mediaPath">Optional relative path to stored media.</param>
    void LogResponse(string sessionId, string kind, string summary, string? mediaPath = null);

    /// <summary>Returns true when persisted records already exist for the given session id.</summary>
    bool SessionExists(string sessionId);
}
