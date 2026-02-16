using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;
using Xunit.Abstractions;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests with no agent command configured.</summary>
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
