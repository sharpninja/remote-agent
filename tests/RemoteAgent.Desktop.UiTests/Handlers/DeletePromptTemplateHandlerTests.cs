using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="DeletePromptTemplateHandler"/>. FR-12.6; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.6")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class DeletePromptTemplateHandlerTests
{
    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenDeleteSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = true };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenDeleteFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = false };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldRefreshPromptTemplates()
    {
        var client = new StubCapacityClient { DeletePromptTemplateResult = true };
        var handler = new DeletePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new DeletePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "tpl1", null, workspace));

        workspace.PromptTemplates.Should().NotBeNull();
        workspace.PromptTemplateStatus.Should().Contain("template");
    }
}
