using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class SaveAgentMcpMappingHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSaveSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = true };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", new[] { "mcp1" }, null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenSaveFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = false };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", new[] { "mcp1" }, null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetMcpStatus()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = true };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", Array.Empty<string>(), null, workspace));

        workspace.McpStatus.Should().Contain("MCP server");
    }
}
