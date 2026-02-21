using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Saves files received via <see cref="FileTransfer"/> to the local filesystem,
/// preserving the relative path hierarchy under a dedicated output directory.</summary>
public static class DesktopFileSaveService
{
    /// <summary>Default base directory for saved file transfers. Uses the user's home directory.</summary>
    private static string DefaultBasePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "RemoteAgent",
            "Files");

    /// <summary>Saves a file transfer to local storage preserving the directory hierarchy from
    /// <see cref="FileTransfer.RelativePath"/>. Returns the saved path, or null on failure.</summary>
    public static string? SaveFileTransfer(FileTransfer fileTransfer, string? basePath = null)
    {
        if (fileTransfer.Content == null || fileTransfer.Content.Length == 0)
            return null;

        var relativePath = fileTransfer.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
            relativePath = $"file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";

        // Normalize to forward slashes
        relativePath = relativePath.Replace('\\', '/');

        var outputBase = string.IsNullOrWhiteSpace(basePath) ? DefaultBasePath : basePath;
        var fullPath = Path.Combine(outputBase, relativePath.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(fullPath, fileTransfer.Content.ToByteArray());
            return fullPath;
        }
        catch
        {
            return null;
        }
    }
}
