using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Stores uploaded images and videos alongside the LiteDB data directory (FR-10.1, TR-11.2). Files are saved under <c>data/media/</c>.</summary>
/// <remarks>Used when the app sends media as agent context. The returned full path can be passed to the agent (e.g. in stdin) so the agent can read the file.</remarks>
/// <example><code>
/// var (relativePath, fullPath) = mediaStorage.SaveUpload(sessionId, content, "image/jpeg", "photo.jpg");
/// await agentSession.SendInputAsync($"[Attachment: {fullPath}]", ct);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-10)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public class MediaStorageService
{
    private readonly string _mediaDir;

    /// <summary>Creates the service using <see cref="AgentOptions.DataDirectory"/>. Media is stored in <c>data/media/</c>.</summary>
    public MediaStorageService(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        _mediaDir = Path.Combine(dataDir, "media");
        Directory.CreateDirectory(_mediaDir);
    }

    /// <summary>Saves uploaded content to <c>media/{sessionId}_{guid}.{ext}</c>. Returns relative path (for logging) and full path (for agent).</summary>
    /// <param name="sessionId">Session identifier (used in filename).</param>
    /// <param name="content">Raw file content.</param>
    /// <param name="contentType">MIME type (e.g. image/jpeg, video/mp4); used to choose extension.</param>
    /// <param name="fileName">Original filename (optional); used as fallback for extension.</param>
    /// <returns>Relative path under data dir and full filesystem path.</returns>
    public (string RelativePath, string FullPath) SaveUpload(string sessionId, byte[] content, string contentType, string? fileName)
    {
        var ext = GetExtensionFromContentType(contentType) ?? Path.GetExtension(fileName ?? "")?.TrimStart('.') ?? "bin";
        var guid = Guid.NewGuid().ToString("N")[..8];
        var safeFileName = $"{sessionId}_{guid}.{ext}";
        var fullPath = Path.Combine(_mediaDir, safeFileName);
        File.WriteAllBytes(fullPath, content);
        var relativePath = Path.Combine("media", safeFileName);
        return (relativePath, fullPath);
    }

    private static string? GetExtensionFromContentType(string contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            _ => null
        };
    }
}
