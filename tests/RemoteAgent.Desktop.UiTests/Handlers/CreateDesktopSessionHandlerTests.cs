using Avalonia.Headless.XUnit;
using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class CreateDesktopSessionHandlerTests
{
    [AvaloniaFact]
    public async Task HandleAsync_ServerMode_WhenCapacityAvailable_ShouldAddSession()
    {
        var snapshot = new RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot(true, "ok", 10, 0, 10, "process", null, 0, null);
        var client = new StubCapacityClient { CapacitySnapshot = snapshot };
        var factory = new StubSessionFactory();
        var handler = new CreateDesktopSessionHandler(client, factory);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);

        var result = await handler.HandleAsync(new CreateDesktopSessionRequest(
            Guid.NewGuid(), "Test Session", "127.0.0.1", 5243, "server", "process", null, null, workspace));

        result.Success.Should().BeTrue();
        workspace.Sessions.Should().HaveCount(1);
        workspace.SelectedSession.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task HandleAsync_ServerMode_WhenCapacityNull_ShouldReturnFail()
    {
        var client = new StubCapacityClient { CapacitySnapshot = null };
        var factory = new StubSessionFactory();
        var handler = new CreateDesktopSessionHandler(client, factory);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);

        var result = await handler.HandleAsync(new CreateDesktopSessionRequest(
            Guid.NewGuid(), "Test Session", "127.0.0.1", 5243, "server", "process", null, null, workspace));

        result.Success.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task HandleAsync_ServerMode_WhenCapacityFull_ShouldReturnFail()
    {
        var snapshot = new RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot(false, "No capacity", 10, 10, 0, "process", null, 1, null);
        var client = new StubCapacityClient { CapacitySnapshot = snapshot };
        var factory = new StubSessionFactory();
        var handler = new CreateDesktopSessionHandler(client, factory);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);

        var result = await handler.HandleAsync(new CreateDesktopSessionRequest(
            Guid.NewGuid(), "Test Session", "127.0.0.1", 5243, "server", "process", null, null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No capacity");
    }

    [AvaloniaFact]
    public async Task HandleAsync_DirectMode_ShouldSkipCapacityCheck()
    {
        var client = new StubCapacityClient { CapacitySnapshot = null };
        var factory = new StubSessionFactory();
        var handler = new CreateDesktopSessionHandler(client, factory);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);

        var result = await handler.HandleAsync(new CreateDesktopSessionRequest(
            Guid.NewGuid(), "Direct Session", "127.0.0.1", 5243, "direct", "process", null, null, workspace));

        result.Success.Should().BeTrue();
        workspace.Sessions.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task HandleAsync_WhenConnectThrows_ShouldReturnFail()
    {
        var fakeSession = new FakeAgentSession { ThrowOnConnect = true };
        var factory = new FailingSessionFactory(fakeSession);
        var client = new StubCapacityClient();
        var handler = new CreateDesktopSessionHandler(client, factory);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client, factory);

        var result = await handler.HandleAsync(new CreateDesktopSessionRequest(
            Guid.NewGuid(), "Test", "127.0.0.1", 5243, "direct", "process", null, null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection failed");
    }

    private sealed class FailingSessionFactory(FakeAgentSession session) : IDesktopSessionViewModelFactory
    {
        public RemoteAgent.Desktop.ViewModels.DesktopSessionViewModel Create(string title, string connectionMode, string agentId) =>
            new(session)
            {
                Title = title,
                ConnectionMode = connectionMode,
                AgentId = agentId,
                SessionId = Guid.NewGuid().ToString("N")[..12]
            };
    }
}
