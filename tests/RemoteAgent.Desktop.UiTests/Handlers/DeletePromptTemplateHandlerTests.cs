using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class DeletePromptTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDeleteSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = true };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = false };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldRefreshPromptTemplates()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = true };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        workspace.PromptTemplates.Should().NotBeNull();
        workspace.PromptTemplateStatus.Should().Contain("template");
    }
}
