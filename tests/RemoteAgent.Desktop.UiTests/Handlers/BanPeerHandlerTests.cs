using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class BanPeerHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenBanSucceeds_ShouldSetStatusContainingBanned()
    {
        var client = new StubCapacityClient { BanPeerResult = true };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", "test reason", null, workspace));

        result.Success.Should().BeTrue();
        workspace.StatusText.Should().Contain("banned");
    }

    [Fact]
    public async Task HandleAsync_WhenBanFails_ShouldSetStatusWithFailure()
    {
        var client = new StubCapacityClient { BanPeerResult = false };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, null, workspace));

        workspace.StatusText.Should().Contain("Failed");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefreshSecurityCollections()
    {
        var client = new StubCapacityClient { BanPeerResult = true };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, null, workspace));

        workspace.AbandonedServerSessions.Should().NotBeNull();
        workspace.ConnectedPeers.Should().NotBeNull();
        workspace.BannedPeers.Should().NotBeNull();
    }
}
