using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class SeedSessionContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSeedSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = true };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "context content", null, null, workspace));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenSeedFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = false };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "content", null, null, workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenSeedSucceeds_ShouldSetSeedStatus()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = true };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess-abc", "text", "content", null, null, workspace));

        workspace.SeedStatus.Should().Contain("sess-abc");
    }

    [Fact]
    public async Task HandleAsync_WhenSeedFails_ShouldSetFailureStatus()
    {
        var client = new StubCapacityClient { SeedSessionContextResult = false };
        var handler = new SeedSessionContextHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new SeedSessionContextRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, "sess1", "text", "content", null, null, workspace));

        workspace.SeedStatus.Should().Contain("Failed");
    }
}
