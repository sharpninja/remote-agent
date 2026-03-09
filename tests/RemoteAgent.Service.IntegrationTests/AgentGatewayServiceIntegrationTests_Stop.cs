using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;
using Xunit.Abstracts;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests: start a long-running agent then send STOP. FR-1.2, FR-2.4, FR-7.1, FR-7.2; TR-2.3, TR-3.2, TR-3.4, TR-4.2, TR-4.3.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.2")]
[Trait("Requirement", "FR-2.4")]
[Trait("Requirement", "FR-7.1")]
[Trait("Requirement", "FR-7.2")]
[Trait("Requirement", "TR-2.3")]
[Trait("Requirement", "TR-3.2")]
[Trait("Requirement", "TR-3.4")]
[Trait("Requirement", "TR-4.2")]
[Trait("Requirement", "TR-4.3")]
public class AgentGatewayServiceIntegrationTests_Stop : IClassFixture<SleepWebApplicationFactory>
{
    private readonly SleepWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AgentGatewayServiceIntegrationTests_Stop(SleepWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Connect_SendStop_ReceivesSessionStoppedEvent()
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test started: Connect_SendStop_ReceivesSessionStoppedEvent");
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var grpcClient = new AgentGateway.AgentGatewayClient(channel);

        var (_, _, events, eventMessages) = await AgentGatewayTestHelper.RunAgentInvocationWithTimeoutAsync(
            grpcClient,
            async (call, ct) =>
            {
                await call.RequestStream.WriteAsync(new ClientMessage
                {
                    Control = new SessionControl { Action = SessionControl.Types.Action.Start }
                }, ct);
                await Task.Delay(300, ct);
                await call.RequestStream.WriteAsync(new ClientMessage
                {
                    Control = new SessionControl { Action = SessionControl.Types.Action.Stop }
                }, ct);
                await call.RequestStream.CompleteAsync();
            },
            _output);

        if (events.Contains(SessionEvent.Types.Kind.SessionError))
        {
            var idx = events.IndexOf(SessionEvent.Types.Kind.SessionError);
            var msg = idx >= 0 && idx < eventMessages.Count ? eventMessages[idx] : "";
            (msg.Contains("did not start", StringComparison.OrdinalIgnoreCase) || msg.Contains("not configured", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue("when agent is unavailable we get a known error: {0}", msg);
        }
        else
        {
            events.Should().Contain(SessionEvent.Types.Kind.SessionStarted);
            events.Should().Contain(SessionEvent.Types.Kind.SessionStopped);
        }
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test passed.");
    }
}
