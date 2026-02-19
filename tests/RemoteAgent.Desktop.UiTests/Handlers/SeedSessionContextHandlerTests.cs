using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SeedSessionContextHandler"/>. FR-12.6; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.6")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SeedSessionContextHandlerTests
{
    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSeedSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = true };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "context content", null, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSeedFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = false };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        var result = await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "content", null, null, workspace));

        result.Success.Should().BeFalse();
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSeedSucceeds_ShouldSetSeedStatus()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = true };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess-abc", "text", "content", null, null, workspace));

        workspace.SeedStatus.Should().Contain("sess-abc");
    }

    // FR-12.6, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSeedFails_ShouldSetFailureStatus()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = false };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePromptTemplatesViewModel(client);

        await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "content", null, null, workspace));

        workspace.SeedStatus.Should().Contain("Failed");
    }
}
