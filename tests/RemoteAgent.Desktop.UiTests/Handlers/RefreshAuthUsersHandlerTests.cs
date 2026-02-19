using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RefreshAuthUsersHandler"/>. FR-13.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RefreshAuthUsersHandlerTests
{
    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        var result = await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldPopulateDefaultViewerRole_WhenNoRolesReturned()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);

        await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PermissionRoles.Should().Contain("viewer");
    }

    // FR-13.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearAndRebuildAuthUsersList()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshAuthUsersHandler(client);
        var workspace = SharedWorkspaceFactory.CreateAuthUsersViewModel(client);
        workspace.AuthUsers.Add(new AuthUserSnapshot("old-user", "Old", "viewer", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        await handler.HandleAsync(new RefreshAuthUsersRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.AuthUsers.Should().BeEmpty();
    }
}
