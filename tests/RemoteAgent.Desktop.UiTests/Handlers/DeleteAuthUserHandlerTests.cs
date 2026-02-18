using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class DeleteAuthUserHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDeleteSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { DeleteAuthUserResult = true };
        var handler = new DeleteAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        var result = await handler.HandleAsync(new DeleteAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "user1", null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { DeleteAuthUserResult = false };
        var handler = new DeleteAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        var result = await handler.HandleAsync(new DeleteAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "user1", null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteSucceeds_ShouldSetStatusText()
    {
        var client = new StubCapacityClient { DeleteAuthUserResult = true };
        var handler = new DeleteAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        await handler.HandleAsync(new DeleteAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "user-abc", null, workspace));

        workspace.StatusText.Should().Contain("user-abc");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefreshAuthUsers()
    {
        var client = new StubCapacityClient { DeleteAuthUserResult = true };
        var handler = new DeleteAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        await handler.HandleAsync(new DeleteAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "user1", null, workspace));

        workspace.AuthUsers.Should().NotBeNull();
        workspace.PermissionRoles.Should().NotBeEmpty();
    }
}
