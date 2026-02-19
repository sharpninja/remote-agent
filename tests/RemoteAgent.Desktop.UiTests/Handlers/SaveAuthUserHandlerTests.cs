using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SaveAuthUserHandler"/>. FR-13.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SaveAuthUserHandlerTests
{
    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldReturnOk()
    {
        var user = new AuthUserSnapshot("user1", "User One", "admin", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var client = new StubCapacityClient { UpsertAuthUserResult = user };
        var handler = new SaveAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        var result = await handler.HandleAsync(new SaveAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, user, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertReturnsNull_ShouldReturnFail()
    {
        var user = new AuthUserSnapshot("user1", "User One", "admin", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var client = new StubCapacityClient { UpsertAuthUserResult = null };
        var handler = new SaveAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        var result = await handler.HandleAsync(new SaveAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, user, null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldSetStatusText()
    {
        var user = new AuthUserSnapshot("user1", "User One", "admin", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var client = new StubCapacityClient { UpsertAuthUserResult = user };
        var handler = new SaveAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        await handler.HandleAsync(new SaveAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, user, null, workspace));

        workspace.StatusText.Should().Contain("user1");
    }

    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldRefreshAuthUsers()
    {
        var user = new AuthUserSnapshot("user1", "User One", "viewer", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var client = new StubCapacityClient { UpsertAuthUserResult = user };
        var handler = new SaveAuthUserHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        await handler.HandleAsync(new SaveAuthUserRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, user, null, workspace));

        workspace.PermissionRoles.Should().NotBeEmpty();
    }
}
