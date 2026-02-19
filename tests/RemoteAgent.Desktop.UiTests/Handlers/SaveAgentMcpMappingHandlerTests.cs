using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SaveAgentMcpMappingHandler"/>. FR-12.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SaveAgentMcpMappingHandlerTests
{
    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSaveSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = true };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", new[] { "mcp1" }, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSaveFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = false };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", new[] { "mcp1" }, null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldSetMcpStatus()
    {
        var client = new StubCapacityClient { SetAgentMcpServersResult = true };
        var handler = new SaveAgentMcpMappingHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        await handler.HandleAsync(new SaveAgentMcpMappingRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "process", Array.Empty<string>(), null, workspace));

        workspace.McpStatus.Should().Contain("MCP server");
    }
}
