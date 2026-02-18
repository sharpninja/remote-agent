using FluentAssertions;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class ApplyLocalServerActionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRunning_ShouldStop()
    {
        var manager = new TrackingLocalServerManager(running: true);
        var handler = new ApplyLocalServerActionHandler(manager);

        var result = await handler.HandleAsync(
            new ApplyLocalServerActionRequest(Guid.NewGuid(), IsCurrentlyRunning: true));

        result.Success.Should().BeTrue();
        manager.LastAction.Should().Be("stop");
        result.Data!.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenStopped_ShouldStart()
    {
        var manager = new TrackingLocalServerManager(running: false);
        var handler = new ApplyLocalServerActionHandler(manager);

        var result = await handler.HandleAsync(
            new ApplyLocalServerActionRequest(Guid.NewGuid(), IsCurrentlyRunning: false));

        result.Success.Should().BeTrue();
        manager.LastAction.Should().Be("start");
        result.Data!.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenActionFails_ShouldReturnFail()
    {
        var manager = new FailingLocalServerManager();
        var handler = new ApplyLocalServerActionHandler(manager);

        var result = await handler.HandleAsync(
            new ApplyLocalServerActionRequest(Guid.NewGuid(), IsCurrentlyRunning: false));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed");
    }

    [Fact]
    public async Task HandleAsync_AfterAction_ShouldProbe()
    {
        var manager = new TrackingLocalServerManager(running: false);
        var handler = new ApplyLocalServerActionHandler(manager);

        await handler.HandleAsync(
            new ApplyLocalServerActionRequest(Guid.NewGuid(), IsCurrentlyRunning: false));

        manager.ProbeCount.Should().BeGreaterThan(0);
    }

    private sealed class TrackingLocalServerManager(bool running) : ILocalServerManager
    {
        private bool _running = running;
        public string LastAction { get; private set; } = "";
        public int ProbeCount { get; private set; }

        public Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        {
            ProbeCount++;
            return Task.FromResult(_running
                ? new LocalServerProbeResult(true, true, true, "Stop Local Server", "Running.")
                : new LocalServerProbeResult(false, false, true, "Start Local Server", "Not running."));
        }

        public Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default)
        {
            _running = true;
            LastAction = "start";
            return Task.FromResult(new LocalServerActionResult(true, "Started."));
        }

        public Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default)
        {
            _running = false;
            LastAction = "stop";
            return Task.FromResult(new LocalServerActionResult(true, "Stopped."));
        }
    }

    private sealed class FailingLocalServerManager : ILocalServerManager
    {
        public Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerProbeResult(false, false, false, "", ""));

        public Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(false, "Failed to start."));

        public Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(false, "Failed to stop."));
    }
}
