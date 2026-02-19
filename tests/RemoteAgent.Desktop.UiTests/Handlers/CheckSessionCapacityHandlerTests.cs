using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="CheckSessionCapacityHandler"/>. FR-13.7, FR-13.8; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.7")]
[Trait("Requirement", "FR-13.8")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class CheckSessionCapacityHandlerTests
{
    // FR-13.7, FR-13.8, TR-18.4
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

    // FR-13.7, FR-13.8, TR-18.4
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

    // FR-13.7, FR-13.8, TR-18.4
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
