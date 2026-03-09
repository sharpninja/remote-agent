using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

/// <summary>Opens the application logs folder in the platform file manager.</summary>
public sealed class OpenLogsFolderHandler(IFolderOpenerService folderOpener)
    : IRequestHandler<OpenLogsFolderRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(
        OpenLogsFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(request.FolderPath))
            return Task.FromResult(CommandResult.Fail($"Logs folder not found: {request.FolderPath}"));

        folderOpener.OpenFolder(request.FolderPath);
        return Task.FromResult(CommandResult.Ok());
    }
}
