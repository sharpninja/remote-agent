using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshAuthUsersHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshAuthUsersRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        RefreshAuthUsersRequest request,
        CancellationToken cancellationToken = default)
    {
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
        request.Workspace.SelectedAuthUser = request.Workspace.AuthUsers.FirstOrDefault();

        return CommandResult.Ok();
    }
}
