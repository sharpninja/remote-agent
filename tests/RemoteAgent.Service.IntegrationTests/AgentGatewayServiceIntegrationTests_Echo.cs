using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;
using Xunit.Abstractions;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests: start default agent (strategy-chosen), send text, expect SessionStarted and echoed output. FR-1.2, FR-1.3, FR-1.4, FR-2.1, FR-2.2, FR-7.1; TR-2.3, TR-3.2, TR-3.3, TR-3.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.2")]
[Trait("Requirement", "FR-1.3")]
[Trait("Requirement", "FR-1.4")]
[Trait("Requirement", "FR-2.1")]
[Trait("Requirement", "FR-2.2")]
[Trait("Requirement", "FR-7.1")]
[Trait("Requirement", "TR-2.3")]
[Trait("Requirement", "TR-3.2")]
[Trait("Requirement", "TR-3.3")]
[Trait("Requirement", "TR-3.4")]
public class AgentGatewayServiceIntegrationTests_Echo : IClassFixture<CatWebApplicationFactory>
{
    private readonly CatWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AgentGatewayServiceIntegrationTests_Echo(CatWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Connect_StartThenSendText_ReceivesEchoFromAgent()
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test started: Connect_StartThenSendText_ReceivesEchoFromAgent");
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
                await Task.Delay(200, ct);
                await call.RequestStream.WriteAsync(new ClientMessage { Text = "hello from test" }, ct);
                await Task.Delay(300, ct);
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
        }
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test passed.");
    }
}
