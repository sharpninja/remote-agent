using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;

namespace RemoteAgent.App.Handlers;

public sealed class SaveServerProfileHandler(IServerProfileStore profileStore)
    : IRequestHandler<SaveServerProfileRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(SaveServerProfileRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;
        var profile = workspace.SelectedProfile;
        if (profile == null)
            return Task.FromResult(CommandResult.Fail("No server selected."));

        profile.DisplayName = workspace.EditDisplayName;
        profile.PerRequestContext = workspace.EditPerRequestContext;
        profile.DefaultSessionContext = workspace.EditDefaultSessionContext;
        profileStore.Upsert(profile);
        workspace.RefreshProfiles();

        return Task.FromResult(CommandResult.Ok());
    }
}
