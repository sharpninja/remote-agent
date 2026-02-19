using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="UnbanPeerHandler"/>. FR-13.4; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.4")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class UnbanPeerHandlerTests
{
    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUnbanSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUnbanFails_ShouldReturnOkWithFailureStatus()
    {
        var client = new StubCapacityClient { UnbanPeerResult = false };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        workspace.StatusText.Should().Contain("Failed");
    }

    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUnbanSucceeds_ShouldSetStatusContainingUnbanned()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.2", null, workspace));

        workspace.StatusText.Should().Contain("unbanned");
    }

    // FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldRefreshSecurityCollections()
    {
        var client = new StubCapacityClient { UnbanPeerResult = true };
        var handler = new UnbanPeerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new UnbanPeerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "10.0.0.1", null, workspace));

        workspace.BannedPeers.Should().NotBeNull();
        workspace.ConnectedPeers.Should().NotBeNull();
    }
}
