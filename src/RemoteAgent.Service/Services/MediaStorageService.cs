using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Stores uploaded images/videos alongside the LiteDB data directory (TR-11.2).</summary>
public class MediaStorageService
{
    private readonly string _mediaDir;

    public MediaStorageService(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        _mediaDir = Path.Combine(dataDir, "media");
        Directory.CreateDirectory(_mediaDir);
    }

    /// <summary>Saves uploaded content to media/{sessionId}_{guid}.{ext}. Returns relative path and full path for agent (TR-11.2).</summary>
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
