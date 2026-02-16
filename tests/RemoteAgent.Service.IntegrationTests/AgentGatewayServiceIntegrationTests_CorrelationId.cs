using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;
using Xunit.Abstractions;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests for TR-4.5: correlation ID on each request is echoed on corresponding response(s).</summary>
public class AgentGatewayServiceIntegrationTests_CorrelationId : IClassFixture<NoCommandWebApplicationFactory>
{
    private readonly NoCommandWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AgentGatewayServiceIntegrationTests_CorrelationId(NoCommandWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Connect_ControlWithCorrelationId_ServerEchoesCorrelationIdOnEvent()
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test started: Connect_ControlWithCorrelationId_ServerEchoesCorrelationIdOnEvent");
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var client = new AgentGateway.AgentGatewayClient(channel);
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Channel and client created, calling Connect().");

        const string correlationId = "cid-control-001";
        var cts = new CancellationTokenSource();
        var call = client.Connect(cancellationToken: cts.Token);
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Connect() returned, writing START and CompleteAsync().");

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start },
            CorrelationId = correlationId
        }, cts.Token);
        await call.RequestStream.CompleteAsync();
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Request stream completed, reading response stream.");

        ServerMessage? eventMessage = null;
        var readCount = 0;
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            readCount++;
            var msg = call.ResponseStream.Current;
            _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Response #{readCount}: PayloadCase={msg.PayloadCase}");
            if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event)
            {
                eventMessage = msg;
                _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Found Event, breaking.");
                break;
            }
        }
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Response loop ended. eventMessage is {(eventMessage == null ? "null" : "set")}.");

        eventMessage.Should().NotBeNull("server should send SessionError when no agent is configured");
        eventMessage!.Event.Should().NotBeNull();
        eventMessage.Event!.Kind.Should().Be(SessionEvent.Types.Kind.SessionError);
        eventMessage.CorrelationId.Should().Be(correlationId, "server must echo the request correlation ID on the response (TR-4.5)");
    }

    [Fact]
    public async Task Connect_TextWithCorrelationId_ServerEchoesCorrelationIdOnError()
    {
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Test started: Connect_TextWithCorrelationId_ServerEchoesCorrelationIdOnError");
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var client = new AgentGateway.AgentGatewayClient(channel);
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Channel and client created, calling Connect().");

        const string correlationId = "cid-text-002";
        var cts = new CancellationTokenSource();
        var call = client.Connect(cancellationToken: cts.Token);
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Connect() returned, writing START.");

        // Start with "none" so no agent runs; then send text to get "Agent not running" error with our correlation ID.
        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start },
            CorrelationId = "cid-start-ignore"
        }, cts.Token);
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] START written, writing Text.");

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Text = "hello",
            CorrelationId = correlationId
        }, cts.Token);
        await call.RequestStream.CompleteAsync();
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Request stream completed, reading response stream.");

        ServerMessage? errorMessage = null;
        var readCount = 0;
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            readCount++;
            var msg = call.ResponseStream.Current;
            var errPreview = msg.Error != null && msg.Error.Length > 50 ? msg.Error.Substring(0, 50) + "..." : msg.Error ?? "";
            _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Response #{readCount}: PayloadCase={msg.PayloadCase}, Error={errPreview}");
            if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Error && msg.Error?.Contains("Agent not running", StringComparison.OrdinalIgnoreCase) == true)
            {
                errorMessage = msg;
                _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Found 'Agent not running' Error, breaking.");
                break;
            }
        }
        _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Response loop ended. errorMessage is {(errorMessage == null ? "null" : "set")}.");

        errorMessage.Should().NotBeNull("server should send error when sending text without an agent");
        errorMessage!.CorrelationId.Should().Be(correlationId, "server must echo the request correlation ID on the response (TR-4.5)");
    }
}
