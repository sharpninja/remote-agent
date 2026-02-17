using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

/// <summary>Session capacity limits: server-wide cap, per-agent cap, GetStatus. FR-13.7, FR-13.8; TR-3.7, TR-3.8.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.7")]
[Trait("Requirement", "FR-13.8")]
[Trait("Requirement", "TR-3.7")]
[Trait("Requirement", "TR-3.8")]
public sealed class SessionCapacityServiceTests_CapacityLimits
{
    [Fact]
    public void TryRegisterSession_Denies_WhenServerLimitReached()
    {
        // Arrange — FR-13.7: server enforces configurable max concurrent sessions
        var options = new AgentOptions { MaxConcurrentSessions = 2 };
        var service = new SessionCapacityService(Options.Create(options));
        var stub1 = new StubAgentSession();
        var stub2 = new StubAgentSession();
        var stub3 = new StubAgentSession();

        // Act
        var r1 = service.TryRegisterSession("process", "sess-1", stub1, out _);
        var r2 = service.TryRegisterSession("process", "sess-2", stub2, out _);
        var r3 = service.TryRegisterSession("process", "sess-3", stub3, out var reason3);

        // Assert
        r1.Should().BeTrue();
        r2.Should().BeTrue();
        r3.Should().BeFalse();
        reason3.Should().Contain("Server session limit");
        reason3.Should().Contain("2");
    }

    [Fact]
    public void TryRegisterSession_RespectsPerAgentLimit_WhenConfigured()
    {
        // Arrange — FR-13.8: per-agent cap; agent-level cannot exceed server cap (TR-3.8)
        var options = new AgentOptions
        {
            MaxConcurrentSessions = 10,
            AgentConcurrentSessionLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["process"] = 2 }
        };
        var service = new SessionCapacityService(Options.Create(options));

        var r1 = service.TryRegisterSession("process", "sess-1", new StubAgentSession(), out _);
        var r2 = service.TryRegisterSession("process", "sess-2", new StubAgentSession(), out _);
        var r3 = service.TryRegisterSession("process", "sess-3", new StubAgentSession(), out var reason3);

        r1.Should().BeTrue();
        r2.Should().BeTrue();
        r3.Should().BeFalse();
        reason3.Should().Contain("Agent 'process'");
        reason3.Should().Contain("limit reached");
    }

    [Fact]
    public void GetStatus_ReturnsCanCreateFalse_WhenServerAtCapacity()
    {
        var options = new AgentOptions { MaxConcurrentSessions = 1 };
        var service = new SessionCapacityService(Options.Create(options));
        service.TryRegisterSession("process", "sess-1", new StubAgentSession(), out _);

        var status = service.GetStatus("process");

        status.CanCreateSession.Should().BeFalse();
        status.ActiveSessionCount.Should().Be(1);
        status.MaxConcurrentSessions.Should().Be(1);
        status.Reason.Should().MatchRegex("Server session limit|Agent 'process' session limit reached");
    }

    [Fact]
    public void GetStatus_ReturnsRemainingAgentCapacity_WhenPerAgentLimitSet()
    {
        var options = new AgentOptions
        {
            MaxConcurrentSessions = 10,
            AgentConcurrentSessionLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["process"] = 3 }
        };
        var service = new SessionCapacityService(Options.Create(options));
        service.TryRegisterSession("process", "sess-1", new StubAgentSession(), out _);

        var status = service.GetStatus("process");

        status.CanCreateSession.Should().BeTrue();
        status.AgentMaxConcurrentSessions.Should().Be(3);
        status.AgentActiveSessionCount.Should().Be(1);
        status.RemainingAgentCapacity.Should().Be(2);
    }

    [Fact]
    public void UnregisterSession_FreesCapacity_SoNewSessionCanRegister()
    {
        var options = new AgentOptions { MaxConcurrentSessions = 1 };
        var service = new SessionCapacityService(Options.Create(options));
        service.TryRegisterSession("process", "sess-1", new StubAgentSession(), out _);
        service.TryRegisterSession("process", "sess-2", new StubAgentSession(), out _).Should().BeFalse();

        service.UnregisterSession("process", "sess-1");
        var registered = service.TryRegisterSession("process", "sess-2", new StubAgentSession(), out _);

        registered.Should().BeTrue();
    }

    private sealed class StubAgentSession : IAgentSession
    {
        public bool HasExited { get; private set; }
        public StreamReader StandardOutput { get; } = new(new MemoryStream());
        public StreamReader StandardError { get; } = new(new MemoryStream());
        public Task SendInputAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
        public void Stop() => HasExited = true;
        public void Dispose() { }
    }
}
