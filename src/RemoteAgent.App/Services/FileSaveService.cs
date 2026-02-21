using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

/// <summary>Saves files received via <see cref="FileTransfer"/> to the device, preserving the relative path hierarchy
/// under the app data directory. On Android, files are stored under AppDataDirectory/RemoteAgent/Files/.</summary>
public static class FileSaveService
{
    /// <summary>Saves a file transfer to local storage preserving the directory hierarchy from
    /// <see cref="FileTransfer.RelativePath"/>. Returns the saved path for display, or null on failure.</summary>
    public static string? SaveFileTransfer(FileTransfer fileTransfer)
    {
        if (fileTransfer.Content == null || fileTransfer.Content.Length == 0)
            return null;

        var relativePath = fileTransfer.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath))
            relativePath = $"file_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";

        // Normalize to forward slashes and sanitize
        relativePath = relativePath.Replace('\\', '/');

        // Build the full path under AppDataDirectory/RemoteAgent/Files/
        var basePath = Path.Combine(FileSystem.AppDataDirectory, "RemoteAgent", "Files");
        var fullPath = Path.Combine(basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(fullPath, fileTransfer.Content.ToByteArray());
            return relativePath;
        }
        catch
        {
            return null;
        }
    }
}
