using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;

namespace RemoteAgent.App.Handlers;

public sealed class DeleteServerProfileHandler(IServerProfileStore profileStore)
    : IRequestHandler<DeleteServerProfileRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(DeleteServerProfileRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;
        var profile = workspace.SelectedProfile;
        if (profile == null)
            return Task.FromResult(CommandResult.Fail("No server selected."));

        profileStore.Delete(profile.Host, profile.Port);
        workspace.SelectedProfile = null;
        workspace.RefreshProfiles();

        return Task.FromResult(CommandResult.Ok());
    }
}
