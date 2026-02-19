using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="BanPeerHandler"/>. FR-13.4; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.4")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class BanPeerHandlerTests
{
    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenBanSucceeds_ShouldSetStatusContainingBanned()
    {
        var client = new StubCapacityClient { BanPeerResult = true };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", "test reason", null, workspace));

        result.Success.Should().BeTrue();
        workspace.StatusText.Should().Contain("banned");
    }

    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenBanFails_ShouldSetStatusWithFailure()
    {
        var client = new StubCapacityClient { BanPeerResult = false };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, null, workspace));

        workspace.StatusText.Should().Contain("Failed");
    }

    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldRefreshSecurityCollections()
    {
        var client = new StubCapacityClient { BanPeerResult = true };
        var handler = new BanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new BanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, null, workspace));

        workspace.AbandonedServerSessions.Should().NotBeNull();
        workspace.ConnectedPeers.Should().NotBeNull();
        workspace.BannedPeers.Should().NotBeNull();
    }
}
