using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RefreshMcpRegistryHandler"/>. FR-12.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RefreshMcpRegistryHandlerTests
{
    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshMcpRegistryHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new RefreshMcpRegistryRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldSetMcpStatus()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshMcpRegistryHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        await handler.HandleAsync(new RefreshMcpRegistryRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.McpStatus.Should().Contain("MCP server");
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearMcpServersWhenNoneReturned()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshMcpRegistryHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        await handler.HandleAsync(new RefreshMcpRegistryRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.McpServers.Should().BeEmpty();
    }
}
