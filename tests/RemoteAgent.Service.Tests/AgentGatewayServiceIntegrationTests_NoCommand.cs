using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Integration tests with no agent command configured.</summary>
public class AgentGatewayServiceIntegrationTests_NoCommand : IClassFixture<NoCommandWebApplicationFactory>
{
    private readonly NoCommandWebApplicationFactory _factory;

    public AgentGatewayServiceIntegrationTests_NoCommand(NoCommandWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Connect_WhenNoAgentCommandConfigured_ReceivesSessionErrorEvent()
    {
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var grpcClient = new AgentGateway.AgentGatewayClient(channel);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var call = grpcClient.Connect(cancellationToken: cts.Token);

        var requestTask = Task.Run(async () =>
        {
            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Start }
            }, cts.Token);
            await call.RequestStream.CompleteAsync();
        }, cts.Token);

        var events = new List<ServerMessage>();
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            var msg = call.ResponseStream.Current;
            events.Add(msg);
            if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                msg.Event.Kind == SessionEvent.Types.Kind.SessionError)
                break;
        }

        await requestTask;
        events.Should().ContainSingle(m => m.PayloadCase == ServerMessage.PayloadOneofCase.Event && m.Event.Kind == SessionEvent.Types.Kind.SessionError);
        events.First(m => m.Event?.Kind == SessionEvent.Types.Kind.SessionError).Event!.Message.Should().Contain("not configured");
    }
}
