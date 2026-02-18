using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RefreshAuthUsersHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateDefaultViewerRole_WhenNoRolesReturned()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PermissionRoles.Should().Contain("viewer");
    }

    [Fact]
    public async Task HandleAsync_ShouldClearAndRebuildAuthUsersList()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);
        workspace.AuthUsers.Add(new AuthUserSnapshot("old-user", "Old", "viewer", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.AuthUsers.Should().BeEmpty();
    }
}
