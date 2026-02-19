using FluentAssertions;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="CheckLocalServerHandler"/>. FR-1.1; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.1")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class CheckLocalServerHandlerTests
{
    // FR-1.1, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenServerRunning_ShouldReturnRunningProbe()
    {
        var manager = new StubLocalServerManager(running: true);
        var handler = new CheckLocalServerHandler(manager);

        var result = await handler.HandleAsync(new CheckLocalServerRequest(Guid.NewGuid()));

        result.Success.Should().BeTrue();
        result.Data!.IsRunning.Should().BeTrue();
        result.Data.CanApplyAction.Should().BeTrue();
    }

    // FR-1.1, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenServerStopped_ShouldReturnStoppedProbe()
    {
        var manager = new StubLocalServerManager(running: false);
        var handler = new CheckLocalServerHandler(manager);

        var result = await handler.HandleAsync(new CheckLocalServerRequest(Guid.NewGuid()));

        result.Success.Should().BeTrue();
        result.Data!.IsRunning.Should().BeFalse();
    }

    // FR-1.1, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var manager = new CancellationAwareLocalServerManager();
        var handler = new CheckLocalServerHandler(manager);

        var act = () => handler.HandleAsync(new CheckLocalServerRequest(Guid.NewGuid()), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class StubLocalServerManager(bool running) : ILocalServerManager
    {
        public Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(running
                ? new LocalServerProbeResult(true, true, true, "Stop Local Server", "Running.")
                : new LocalServerProbeResult(false, false, true, "Start Local Server", "Not running."));
        }

        public Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(true, "Started."));

        public Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(true, "Stopped."));
    }

    private sealed class CancellationAwareLocalServerManager : ILocalServerManager
    {
        public Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new LocalServerProbeResult(false, false, false, "", ""));
        }

        public Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(true, ""));

        public Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LocalServerActionResult(true, ""));
    }
}
