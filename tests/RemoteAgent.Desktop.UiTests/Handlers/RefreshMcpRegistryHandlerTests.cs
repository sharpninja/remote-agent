using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RefreshMcpRegistryHandlerTests
{
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
