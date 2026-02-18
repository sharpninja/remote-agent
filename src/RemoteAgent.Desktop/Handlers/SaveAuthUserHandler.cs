using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SaveAuthUserHandler(IServerCapacityClient client)
    : IRequestHandler<SaveAuthUserRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        SaveAuthUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var saved = await client.UpsertAuthUserAsync(
            request.Host, request.Port, request.User, request.ApiKey, cancellationToken);

        if (saved is null)
        {
            request.Workspace.StatusText = "Failed to save auth user.";
            return CommandResult.Fail("Failed to save auth user.");
        }

        request.Workspace.StatusText = $"Saved auth user {saved.UserId} ({saved.Role}).";

        // Refresh auth users inline
        var roles = await client.GetPermissionRolesAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        var users = await client.GetAuthUsersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);

        request.Workspace.PermissionRoles.Clear();
        foreach (var role in roles)
            request.Workspace.PermissionRoles.Add(role);
        if (request.Workspace.PermissionRoles.Count == 0)
            request.Workspace.PermissionRoles.Add("viewer");

        if (!request.Workspace.PermissionRoles.Contains(request.Workspace.AuthRole, StringComparer.OrdinalIgnoreCase))
            request.Workspace.AuthRole = request.Workspace.PermissionRoles.First();

        request.Workspace.AuthUsers.Clear();
        foreach (var user in users)
            request.Workspace.AuthUsers.Add(user);

        var savedId = saved.UserId;
        request.Workspace.SelectedAuthUser =
            request.Workspace.AuthUsers.FirstOrDefault(x => string.Equals(x.UserId, savedId, StringComparison.OrdinalIgnoreCase))
            ?? request.Workspace.AuthUsers.FirstOrDefault();

        return CommandResult.Ok();
    }
}
