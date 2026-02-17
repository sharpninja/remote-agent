using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests for GetServerInfo handshake.</summary>
public class AgentGatewayServiceIntegrationTests_GetServerInfo : IClassFixture<NoCommandWebApplicationFactory>
{
    private readonly NoCommandWebApplicationFactory _factory;

    public AgentGatewayServiceIntegrationTests_GetServerInfo(NoCommandWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetServerInfo_ReturnsVersionAndCapabilities()
    {
        var channel = GrpcChannel.ForAddress(_factory.BaseAddress, new GrpcChannelOptions { HttpHandler = _factory.CreateHandler() });
        var client = new AgentGateway.AgentGatewayClient(channel);

        var response = await client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = "1.0" });

        response.ServerVersion.Should().NotBeNullOrEmpty();
        response.Capabilities.Should().Contain("scripts");
        response.Capabilities.Should().Contain("media_upload");
        response.Capabilities.Should().Contain("agents");
        response.AvailableAgents.Should().Contain("process");
    }
}
