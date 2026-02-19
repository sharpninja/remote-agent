using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RefreshSecurityDataHandler"/>. FR-13.2, FR-13.4; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.2")]
[Trait("Requirement", "FR-13.4")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RefreshSecurityDataHandlerTests
{
    // FR-13.2, FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshSecurityDataHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new RefreshSecurityDataRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.2, FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldSetStatusText()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshSecurityDataHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new RefreshSecurityDataRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.StatusText.Should().Contain("refreshed");
    }

    // FR-13.2, FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearAbandonedSessionsWhenNoneReturned()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshSecurityDataHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);
        workspace.AbandonedServerSessions.Add(
            new AbandonedServerSessionSnapshot("s1", "proc", "reason", DateTimeOffset.UtcNow));

        await handler.HandleAsync(new RefreshSecurityDataRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.AbandonedServerSessions.Should().BeEmpty();
    }

    // FR-13.2, FR-13.4, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearBannedPeers()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshSecurityDataHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new RefreshSecurityDataRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.BannedPeers.Should().BeEmpty();
    }
}
