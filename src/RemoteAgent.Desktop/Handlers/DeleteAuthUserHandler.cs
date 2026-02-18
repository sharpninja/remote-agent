using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class DeleteAuthUserHandler(IServerCapacityClient client)
    : IRequestHandler<DeleteAuthUserRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(DeleteAuthUserRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.DeleteAuthUserAsync(
            request.Host, request.Port, request.UserId, request.ApiKey, cancellationToken);

        request.Workspace.StatusText = ok
            ? $"Deleted auth user {request.UserId}."
            : $"Failed to delete auth user {request.UserId}.";

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
        request.Workspace.SelectedAuthUser = request.Workspace.AuthUsers.FirstOrDefault();

        return ok ? CommandResult.Ok() : CommandResult.Fail($"Failed to delete auth user {request.UserId}.");
    }
}
