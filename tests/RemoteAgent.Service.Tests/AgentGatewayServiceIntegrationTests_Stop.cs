using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Integration tests: start a long-running agent then send STOP.</summary>
public class AgentGatewayServiceIntegrationTests_Stop : IClassFixture<SleepWebApplicationFactory>
{
    private readonly SleepWebApplicationFactory _factory;

    public AgentGatewayServiceIntegrationTests_Stop(SleepWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Connect_SendStop_ReceivesSessionStoppedEvent()
    {
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var grpcClient = new AgentGateway.AgentGatewayClient(channel);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var call = grpcClient.Connect(cancellationToken: cts.Token);

        var requestTask = Task.Run(async () =>
        {
            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Start }
            }, cts.Token);
            await Task.Delay(300, cts.Token);
            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Stop }
            }, cts.Token);
            await call.RequestStream.CompleteAsync();
        }, cts.Token);

        var events = new List<SessionEvent.Types.Kind>();
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            var msg = call.ResponseStream.Current;
            if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event && msg.Event != null)
                events.Add(msg.Event.Kind);
        }

        await requestTask;
        events.Should().Contain(SessionEvent.Types.Kind.SessionStarted);
        events.Should().Contain(SessionEvent.Types.Kind.SessionStopped);
    }
}
