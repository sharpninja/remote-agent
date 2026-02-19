using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SaveMcpServerHandler"/>. FR-12.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SaveMcpServerHandlerTests
{
    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldReturnOk()
    {
        var server = new McpServerDefinition { ServerId = "mcp1", DisplayName = "Test MCP" };
        var client = new StubCapacityClient { UpsertMcpServerResult = server };
        var handler = new SaveMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new SaveMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, server, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertReturnsNull_ShouldReturnFail()
    {
        var server = new McpServerDefinition { ServerId = "mcp1" };
        var client = new StubCapacityClient { UpsertMcpServerResult = null };
        var handler = new SaveMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        var result = await handler.HandleAsync(new SaveMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, server, null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldSetMcpStatus()
    {
        var server = new McpServerDefinition { ServerId = "mcp1" };
        var client = new StubCapacityClient { UpsertMcpServerResult = server };
        var handler = new SaveMcpServerHandler(client);
        var workspace = SharedWorkspaceFactory.CreateMcpRegistryViewModel(client);

        await handler.HandleAsync(new SaveMcpServerRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, server, null, workspace));

        workspace.McpStatus.Should().Contain("MCP server");
    }
}
