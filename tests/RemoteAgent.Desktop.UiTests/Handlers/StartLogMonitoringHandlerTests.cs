using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class StartLogMonitoringHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenHostEmpty_ShouldReturnFail()
    {
        var logClient = new StubStructuredLogClient();
        var handler = new StartLogMonitoringHandler(logClient);
        var workspace = SharedWorkspaceFactory.CreateStructuredLogsViewModel();

        var result = await handler.HandleAsync(new StartLogMonitoringRequest(
            Guid.NewGuid(), "", 5243, null, "srv1", 0, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Host");
    }

    [Fact]
    public async Task HandleAsync_WhenPortInvalid_ShouldReturnFail()
    {
        var logClient = new StubStructuredLogClient();
        var handler = new StartLogMonitoringHandler(logClient);
        var workspace = SharedWorkspaceFactory.CreateStructuredLogsViewModel();

        var result = await handler.HandleAsync(new StartLogMonitoringRequest(
            Guid.NewGuid(), "127.0.0.1", 0, null, "srv1", 0, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenSnapshotNull_ShouldReturnOkWithOriginalOffset()
    {
        var logClient = new StubStructuredLogClient { SnapshotResult = null };
        var handler = new StartLogMonitoringHandler(logClient);
        var workspace = SharedWorkspaceFactory.CreateStructuredLogsViewModel();

        var result = await handler.HandleAsync(new StartLogMonitoringRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, "srv1", 42, workspace));

        result.Success.Should().BeTrue();
        result.Data!.NextOffset.Should().Be(42);
    }

    [Fact]
    public async Task HandleAsync_WhenClientThrows_ShouldReturnFail()
    {
        var logClient = new StubStructuredLogClient { ThrowOnGet = true };
        var handler = new StartLogMonitoringHandler(logClient);
        var workspace = SharedWorkspaceFactory.CreateStructuredLogsViewModel();

        var result = await handler.HandleAsync(new StartLogMonitoringRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, "srv1", 0, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Log snapshot failed");
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_ShouldSetLogMonitorStatus()
    {
        var logClient = new StubStructuredLogClient { SnapshotResult = null };
        var handler = new StartLogMonitoringHandler(logClient);
        var workspace = SharedWorkspaceFactory.CreateStructuredLogsViewModel();

        await handler.HandleAsync(new StartLogMonitoringRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, "srv1", 0, workspace));

        workspace.LogMonitorStatus.Should().Contain("Monitoring");
    }
}
