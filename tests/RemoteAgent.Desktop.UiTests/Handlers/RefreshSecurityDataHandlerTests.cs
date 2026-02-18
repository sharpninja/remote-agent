using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RefreshSecurityDataHandlerTests
{
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
