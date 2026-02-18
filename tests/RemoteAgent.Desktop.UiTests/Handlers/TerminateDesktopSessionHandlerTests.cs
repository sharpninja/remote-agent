using Avalonia.Headless.XUnit;
using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class TerminateDesktopSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSessionNull_ShouldReturnFail()
    {
        var handler = new TerminateDesktopSessionHandler();
        var workspace = SharedWorkspaceFactory.CreateWorkspace();

        var result = await handler.HandleAsync(new TerminateDesktopSessionRequest(
            Guid.NewGuid(), null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No session");
    }

    [AvaloniaFact]
    public async Task HandleAsync_ShouldRemoveSessionFromWorkspace()
    {
        var fakeSession = new FakeAgentSession();
        await fakeSession.ConnectAsync("127.0.0.1", 5243);
        var client = new StubCapacityClient();
        var factory = new StubSessionFactory();
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);
        var session = factory.Create("Test Session", "direct", "process");
        workspace.Sessions.Add(session);
        workspace.SelectedSession = session;
        var handler = new TerminateDesktopSessionHandler();

        var result = await handler.HandleAsync(new TerminateDesktopSessionRequest(
            Guid.NewGuid(), session, workspace));

        result.Success.Should().BeTrue();
        workspace.Sessions.Should().NotContain(session);
    }

    [AvaloniaFact]
    public async Task HandleAsync_ShouldSetStatusText()
    {
        var factory = new StubSessionFactory();
        var workspace = SharedWorkspaceFactory.CreateWorkspace(factory: factory);
        var session = factory.Create("My Session", "direct", "process");
        workspace.Sessions.Add(session);
        var handler = new TerminateDesktopSessionHandler();

        await handler.HandleAsync(new TerminateDesktopSessionRequest(
            Guid.NewGuid(), session, workspace));

        workspace.StatusText.Should().Contain("My Session");
    }

    [AvaloniaFact]
    public async Task HandleAsync_ShouldSelectNextSessionAfterRemoval()
    {
        var factory = new StubSessionFactory();
        var workspace = SharedWorkspaceFactory.CreateWorkspace(factory: factory);
        var session1 = factory.Create("Session 1", "direct", "process");
        var session2 = factory.Create("Session 2", "direct", "process");
        workspace.Sessions.Add(session1);
        workspace.Sessions.Add(session2);
        workspace.SelectedSession = session1;
        var handler = new TerminateDesktopSessionHandler();

        await handler.HandleAsync(new TerminateDesktopSessionRequest(
            Guid.NewGuid(), session1, workspace));

        workspace.SelectedSession.Should().Be(session2);
    }
}
