using FluentAssertions;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using Xunit;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration tests for GetServerInfo handshake. FR-8.1, FR-9.1, FR-11.1.2, FR-12.5; TR-4.5, TR-10.1, TR-12.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-8.1")]
[Trait("Requirement", "FR-9.1")]
[Trait("Requirement", "FR-11.1.2")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "TR-4.5")]
[Trait("Requirement", "TR-10.1")]
[Trait("Requirement", "TR-12.2")]
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
