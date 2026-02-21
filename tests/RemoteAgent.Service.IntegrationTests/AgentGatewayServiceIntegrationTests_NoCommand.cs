using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;
using Xunit.Abstractions;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests with no agent command configured. FR-1.2, FR-16.1; TR-2.3, TR-3.2, TR-4.3, TR-8.3.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.2")]
[Trait("Requirement", "FR-16.1")]
[Trait("Requirement", "TR-2.3")]
[Trait("Requirement", "TR-3.2")]
[Trait("Requirement", "TR-4.3")]
[Trait("Requirement", "TR-8.3")]
public class AgentGatewayServiceIntegrationTests_NoCommand : IClassFixture<NoCommandWebApplicationFactory>
{
    private readonly NoCommandWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AgentGatewayServiceIntegrationTests_NoCommand(NoCommandWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent()
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test started: Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent");
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
                await call.RequestStream.CompleteAsync();
            },
            _output);

        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test assertions: events={events.Count}, eventMessages={eventMessages.Count}");
        events.Should().Contain(SessionEvent.Types.Kind.SessionError);
        var errorIndex = events.IndexOf(SessionEvent.Types.Kind.SessionError);
        eventMessages[errorIndex].Should().Contain("not configured");
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test passed.");
    }
}
