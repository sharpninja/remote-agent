using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="TerminateOpenServerSessionHandler"/>. FR-13.3; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.3")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class TerminateOpenServerSessionHandlerTests
{
    // FR-13.3, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenTerminateSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { TerminateSessionResult = true };
        var handler = new TerminateOpenServerSessionHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new TerminateOpenServerSessionRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-13.3, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenTerminateFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { TerminateSessionResult = false };
        var handler = new TerminateOpenServerSessionHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        var result = await handler.HandleAsync(new TerminateOpenServerSessionRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-13.3, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenTerminateSucceeds_ShouldSetStatusText()
    {
        var client = new StubCapacityClient { TerminateSessionResult = true };
        var handler = new TerminateOpenServerSessionHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);

        await handler.HandleAsync(new TerminateOpenServerSessionRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess-xyz", null, workspace));

        workspace.StatusText.Should().Contain("sess-xyz");
    }

    // FR-13.3, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenTerminateSucceeds_ShouldRefreshOpenSessions()
    {
        var client = new StubCapacityClient { TerminateSessionResult = true };
        var handler = new TerminateOpenServerSessionHandler(client);
        var workspace = SharedWorkspaceFactory.CreateSecurityViewModel(client);
        workspace.OpenServerSessions.Add(new OpenServerSessionSnapshot("sess1", "process", true));

        await handler.HandleAsync(new TerminateOpenServerSessionRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", null, workspace));

        workspace.OpenServerSessions.Should().BeEmpty();
    }
}
