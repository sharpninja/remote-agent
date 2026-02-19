using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SavePluginsHandler"/>. FR-12.5; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SavePluginsHandlerTests
{
    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpdateReturnsNull_ShouldReturnFail()
    {
        var client = new StubCapacityClient { UpdatePluginsResult = null };
        var handler = new SavePluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePluginsViewModel(client);

        var result = await handler.HandleAsync(new SavePluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, new[] { "Assembly.dll" }, null, workspace));

        result.Success.Should().BeFalse();
        workspace.PluginStatus.Should().Contain("Failed");
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpdateSucceeds_ShouldReturnOk()
    {
        var config = new PluginConfigurationSnapshot(
            new[] { "Assembly.dll" }, new[] { "runner1" }, true, "Updated.");
        var client = new StubCapacityClient { UpdatePluginsResult = config, GetPluginsResult = config };
        var handler = new SavePluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePluginsViewModel(client);

        var result = await handler.HandleAsync(new SavePluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, new[] { "Assembly.dll" }, null, workspace));

        result.Success.Should().BeTrue();
    }

    // FR-12.5, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenUpdateSucceeds_ShouldRefreshAssemblies()
    {
        var config = new PluginConfigurationSnapshot(
            new[] { "Assembly.One.dll", "Assembly.Two.dll" }, new[] { "runner1" }, true, "ok");
        var client = new StubCapacityClient { UpdatePluginsResult = config, GetPluginsResult = config };
        var handler = new SavePluginsHandler(client);
        var workspace = SharedWorkspaceFactory.CreatePluginsViewModel(client);

        await handler.HandleAsync(new SavePluginsRequest(
            Guid.NewGuid(), "127.0.0.1", 5243, new[] { "Assembly.One.dll" }, null, workspace));

        workspace.ConfiguredPluginAssemblies.Should().HaveCount(2);
    }
}
