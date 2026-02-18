using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RefreshPluginsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenConfigNull_ShouldReturnFail()
    {
        var client = new StubCapacityClient { GetPluginsResult = null };
        var handler = new RefreshPluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new RefreshPluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeFalse();
        workspace.PluginStatus.Should().Contain("Failed");
    }

    [Fact]
    public async Task HandleAsync_WhenConfigReturned_ShouldPopulateAssemblies()
    {
        var config = new PluginConfigurationSnapshot(
            new[] { "Assembly.One.dll", "Assembly.Two.dll" },
            new[] { "runner1" },
            true,
            "ok");
        var client = new StubCapacityClient { GetPluginsResult = config };
        var handler = new RefreshPluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new RefreshPluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
        workspace.ConfiguredPluginAssemblies.Should().HaveCount(2);
        workspace.LoadedPluginRunnerIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WhenConfigReturned_ShouldSetPluginAssembliesText()
    {
        var config = new PluginConfigurationSnapshot(
            new[] { "Assembly.One.dll" },
            Array.Empty<string>(),
            true,
            "ok");
        var client = new StubCapacityClient { GetPluginsResult = config };
        var handler = new RefreshPluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new RefreshPluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, null, workspace));

        workspace.PluginAssembliesText.Should().Contain("Assembly.One.dll");
    }
}
