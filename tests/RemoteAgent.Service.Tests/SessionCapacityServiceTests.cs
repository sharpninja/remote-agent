using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

public sealed class SessionCapacityServiceTests
{
    [Fact]
    public void MarkSessionAbandoned_ShouldTrackAndClearOnRegister()
    {
        var service = new SessionCapacityService(Options.Create(new AgentOptions()));
        service.MarkSessionAbandoned("sess-1", "process", "disconnect");

        var abandoned = service.ListAbandonedSessions();
        abandoned.Should().Contain(x => x.SessionId == "sess-1");

        var fakeSession = new StubAgentSession();
        service.TryRegisterSession("process", "sess-1", fakeSession, out _).Should().BeTrue();

        service.ListAbandonedSessions().Should().NotContain(x => x.SessionId == "sess-1");
    }

    private sealed class StubAgentSession : IAgentSession
    {
        public bool HasExited { get; private set; }
        public StreamReader StandardOutput { get; } = new(new MemoryStream());
        public StreamReader StandardError { get; } = new(new MemoryStream());
        public Task SendInputAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
        public void Stop() => HasExited = true;
        public void Dispose()
        {
        }
    }
}
