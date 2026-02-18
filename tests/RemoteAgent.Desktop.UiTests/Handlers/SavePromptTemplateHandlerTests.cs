using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class SavePromptTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldReturnOk()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1", DisplayName = "Test Template" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = template };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenUpsertReturnsNull_ShouldReturnFail()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = null };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldSetPromptTemplateStatus()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = template };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        workspace.PromptTemplateStatus.Should().Contain("template");
    }
}
