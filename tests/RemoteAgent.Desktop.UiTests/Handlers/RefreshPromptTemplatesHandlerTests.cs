using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RefreshPromptTemplatesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetPromptTemplateStatus()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PromptTemplateStatus.Should().Contain("template");
    }

    [Fact]
    public async Task HandleAsync_ShouldClearAndRebuildTemplatesList()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PromptTemplates.Should().BeEmpty();
    }
}
