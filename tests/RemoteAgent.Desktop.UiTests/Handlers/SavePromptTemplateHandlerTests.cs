using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SavePromptTemplateHandler"/>. FR-12.6; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.6")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SavePromptTemplateHandlerTests
{
    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldReturnOk()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1", DisplayName = "Test Template" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = template };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertReturnsNull_ShouldReturnFail()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = null };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpsertSucceeds_ShouldSetPromptTemplateStatus()
    {
        var template = new PromptTemplateDefinition { TemplateId = "tpl1" };
        var client = new StubCapacityClient { UpsertPromptTemplateResult = template };
        var handler = new SavePromptTemplateHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new SavePromptTemplateRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, template, null, workspace));

        workspace.PromptTemplateStatus.Should().Contain("template");
    }
}
