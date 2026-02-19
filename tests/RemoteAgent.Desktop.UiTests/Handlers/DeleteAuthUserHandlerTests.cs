using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="DeleteAuthUserHandler"/>. FR-13.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class DeleteAuthUserHandlerTests
{
    // FR-13.5, TR-18.4
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

    // FR-13.5, TR-18.4
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

    // FR-13.5, TR-18.4
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

    // FR-13.5, TR-18.4
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
