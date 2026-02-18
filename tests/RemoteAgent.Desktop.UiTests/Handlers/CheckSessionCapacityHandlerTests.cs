using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class CheckSessionCapacityHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSnapshotIsNull_ShouldReturnFail()
    {
        var client = new StubCapacityClient { CapacitySnapshot = null };
        var handler = new CheckSessionCapacityHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new CheckSessionCapacityRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenSnapshotReturned_ShouldPopulateCapacitySummary()
    {
        var snapshot = new RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot(true, "ok", 10, 2, 8, "process", null, 1, null);
        var client = new StubCapacityClient { CapacitySnapshot = snapshot };
        var handler = new CheckSessionCapacityHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new CheckSessionCapacityRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", null, workspace));

        result.Success.Should().BeTrue();
        workspace.CapacitySummary.Should().Contain("active");
    }

    [Fact]
    public async Task HandleAsync_WhenCannotCreateSession_ShouldReturnOkWithReasonInStatus()
    {
        var snapshot = new RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot(false, "Server full", 10, 10, 0, "process", null, 1, null);
        var client = new StubCapacityClient { CapacitySnapshot = snapshot };
        var handler = new CheckSessionCapacityHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new CheckSessionCapacityRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", null, workspace));

        result.Success.Should().BeTrue();
        workspace.StatusText.Should().Be("Server full");
    }
}
