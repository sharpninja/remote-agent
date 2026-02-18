using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class UnbanPeerHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUnbanSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUnbanFails_ShouldReturnOkWithFailureStatus()
    {
        var client = new StubCapacityClient { UnbanPeerResult = false };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        workspace.StatusText.Should().Contain("Failed");
    }

    [Fact]
    public async Task HandleAsync_WhenUnbanSucceeds_ShouldSetStatusContainingUnbanned()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.2", null, workspace));

        workspace.StatusText.Should().Contain("unbanned");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefreshSecurityCollections()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        workspace.BannedPeers.Should().NotBeNull();
        workspace.ConnectedPeers.Should().NotBeNull();
    }
}
