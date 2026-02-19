using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RefreshPromptTemplatesHandler"/>. FR-12.6; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.6")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RefreshPromptTemplatesHandlerTests
{
    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldSetPromptTemplateStatus()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PromptTemplateStatus.Should().Contain("template");
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldClearAndRebuildTemplatesList()
    {
        var client = new StubCapacityClient();
        var handler = new RefreshPromptTemplatesHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new RefreshPromptTemplatesRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PromptTemplates.Should().BeEmpty();
    }
}
