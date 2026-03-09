using System.Diagnostics;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Opens a folder in the platform file manager (Explorer on Windows, Finder on macOS, xdg-open on Linux).</summary>
public sealed class SystemFolderOpenerService : IFolderOpenerService
{
    public void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (OperatingSystem.IsWindows())
            Process.Start("explorer.exe", path);
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", path);
        else
            Process.Start("xdg-open", path);
    }
}
