using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RefreshOpenSessionsHandler"/>. FR-13.1; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.1")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RefreshOpenSessionsHandlerTests
{
    // FR-13.1, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshOpenSessionsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new RefreshOpenSessionsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.1, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldSetStatusText()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshOpenSessionsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new RefreshOpenSessionsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.StatusText.Should().Contain("open server session");
    }

    // FR-13.1, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearAndRebuildOpenSessions()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshOpenSessionsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);
        workspace.OpenServerSessions.Add(new OpenServerSessionSnapshot("sess1", "process", true));

        await handler.HandleAsync(new RefreshOpenSessionsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.OpenServerSessions.Should().BeEmpty();
    }
}
