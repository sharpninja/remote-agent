namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Abstracts opening a folder in the platform file manager, enabling testability.</summary>
public interface IFolderOpenerService
{
    void OpenFolder(string path);
}
