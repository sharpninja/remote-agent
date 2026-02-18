using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class DeleteMcpServerHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDeleteSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { DeleteMcpServerResult = true };
        var handler = new DeleteMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new DeleteMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "mcp1", null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { DeleteMcpServerResult = false };
        var handler = new DeleteMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new DeleteMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "mcp1", null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldRefreshMcpRegistry()
    {
        var client = new StubCapacityClient { DeleteMcpServerResult = true };
        var handler = new DeleteMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        await handler.HandleAsync(new DeleteMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "mcp1", null, workspace));

        workspace.McpServers.Should().NotBeNull();
        workspace.McpStatus.Should().Contain("MCP server");
    }
}
