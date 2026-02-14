using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Integration tests with /bin/cat as agent (echo stdin to stdout). Unix only.</summary>
public class AgentGatewayServiceIntegrationTests_Echo : IClassFixture<CatWebApplicationFactory>
{
    private readonly CatWebApplicationFactory _factory;

    public AgentGatewayServiceIntegrationTests_Echo(CatWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Connect_StartThenSendText_ReceivesEchoFromAgent()
    {
        if (OperatingSystem.IsWindows())
            return;
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var grpcClient = new AgentGateway.AgentGatewayClient(channel);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var call = grpcClient.Connect(cancellationToken: cts.Token);

        var requestTask = Task.Run(async () =>
        {
            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Start }
            }, cts.Token);
            await Task.Delay(200, cts.Token);
            await call.RequestStream.WriteAsync(new ClientMessage { Text = "hello from test" }, cts.Token);
            await Task.Delay(300, cts.Token);
            await call.RequestStream.CompleteAsync();
        }, cts.Token);

        var outputs = new List<string>();
        var started = false;
        try
        {
            while (await call.ResponseStream.MoveNext(cts.Token))
            {
                var msg = call.ResponseStream.Current;
                if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event && msg.Event?.Kind == SessionEvent.Types.Kind.SessionStarted)
                    started = true;
                if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Output)
                    outputs.Add(msg.Output);
            }
        }
        catch (OperationCanceledException) { }

        await requestTask;
        started.Should().BeTrue();
        outputs.Should().Contain("hello from test");
    }
}
