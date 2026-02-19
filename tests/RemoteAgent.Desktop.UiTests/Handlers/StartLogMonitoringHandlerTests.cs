using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="StartLogMonitoringHandler"/>. FR-12.11; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.11")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class StartLogMonitoringHandlerTests
{
    // FR-12.11, TR-18.4
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

    // FR-12.11, TR-18.4
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

    // FR-12.11, TR-18.4
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

    // FR-12.11, TR-18.4
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

    // FR-12.11, TR-18.4
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
