using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Microsoft.AspNetCore.StaticFiles;
using RemoteAgent.Proto;

namespace RemoteAgent.Service.Services;

/// <summary>Detects file paths in agent output text, reads the files, and produces <see cref="FileTransfer"/> messages.
/// Uses SHA-256 content hashing to avoid re-sending unchanged files within a session.</summary>
public sealed class FilePathDetectorService
{
    // Match absolute Unix paths: /word/word/file.ext (at least 2 segments, last segment has an extension)
    private static readonly Regex UnixPathRegex = new(
        @"(?<![a-zA-Z0-9_.\-])(/(?:[a-zA-Z0-9_.@\-]+/)+[a-zA-Z0-9_.@\-]+\.[a-zA-Z0-9]+)(?![a-zA-Z0-9_.\-/])",
        RegexOptions.Compiled);

    // Match absolute Windows paths: C:\dir\file.ext or C:/dir/file.ext
    private static readonly Regex WindowsPathRegex = new(
        @"(?<![a-zA-Z0-9_.\-])([A-Za-z]:[/\\](?:[^\s:*?""<>|/\\]+[/\\])*[^\s:*?""<>|/\\]+\.[a-zA-Z0-9]+)(?![a-zA-Z0-9_.\-/\\])",
        RegexOptions.Compiled);

    private static readonly FileExtensionContentTypeProvider MimeProvider = new();

    private readonly ILogger<FilePathDetectorService> _logger;

    public FilePathDetectorService(ILogger<FilePathDetectorService> logger)
    {
        _logger = logger;
    }

    /// <summary>Detects absolute file paths in the given text. Returns distinct paths that exist on disk.</summary>
    public IReadOnlyList<string> DetectFilePaths(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UnixPathRegex.Matches(text))
        {
            var path = match.Groups[1].Value;
            if (File.Exists(path))
                paths.Add(path);
        }

        foreach (Match match in WindowsPathRegex.Matches(text))
        {
            var path = match.Groups[1].Value;
            if (File.Exists(path))
                paths.Add(path);
        }

        return [.. paths];
    }

    /// <summary>Reads a file and builds a <see cref="FileTransfer"/> message if the content has changed
    /// since the last send (tracked via <paramref name="sentFileHashes"/>).
    /// Returns null if the file cannot be read or content is unchanged.</summary>
    public FileTransfer? BuildFileTransferIfChanged(
        string absolutePath,
        Dictionary<string, byte[]> sentFileHashes)
    {
        try
        {
            var content = File.ReadAllBytes(absolutePath);
            var hash = SHA256.HashData(content);

            if (sentFileHashes.TryGetValue(absolutePath, out var previousHash) &&
                hash.AsSpan().SequenceEqual(previousHash))
            {
                return null; // unchanged
            }

            sentFileHashes[absolutePath] = hash;

            var relativePath = ToRelativePath(absolutePath);
            var contentType = GetContentType(absolutePath);

            return new FileTransfer
            {
                RelativePath = relativePath,
                Content = ByteString.CopyFrom(content),
                ContentType = contentType,
                TotalSize = content.LongLength
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read file for transfer: {Path}", absolutePath);
            return null;
        }
    }

    /// <summary>Converts an absolute path to a unix-style relative path from the drive root.
    /// E.g. "/home/user/file.txt" → "home/user/file.txt",
    /// "C:\Users\file.txt" → "Users/file.txt".</summary>
    public static string ToRelativePath(string absolutePath)
    {
        // Normalize to forward slashes
        var normalized = absolutePath.Replace('\\', '/');

        // Strip leading slash for Unix paths: /home/... → home/...
        if (normalized.StartsWith('/'))
            return normalized.TrimStart('/');

        // Strip drive letter for Windows paths: C:/Users/... → Users/...
        if (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
            return normalized[3..];

        return normalized;
    }

    /// <summary>Gets a MIME content type from the file extension, defaulting to application/octet-stream.</summary>
    private static string GetContentType(string path)
    {
        if (MimeProvider.TryGetContentType(path, out var contentType))
            return contentType;
        return "application/octet-stream";
    }
}
