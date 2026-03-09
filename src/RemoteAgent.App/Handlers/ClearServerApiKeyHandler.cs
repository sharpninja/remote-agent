using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;

namespace RemoteAgent.App.Handlers;

public sealed class ClearServerApiKeyHandler(IServerProfileStore profileStore)
    : IRequestHandler<ClearServerApiKeyRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(ClearServerApiKeyRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;
        var profile = workspace.SelectedProfile;
        if (profile == null)
            return Task.FromResult(CommandResult.Fail("No server selected."));

        profile.ApiKey = "";
        profileStore.Upsert(profile);
        workspace.HasApiKey = false;

        return Task.FromResult(CommandResult.Ok());
    }
}
