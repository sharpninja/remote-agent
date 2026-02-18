using FluentAssertions;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class ClearAppLogHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldClearStoreAndViewModel()
    {
        var store = new StubAppLogStore();
        store.Add(new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Cat", "msg", null));
        var workspace = SharedWorkspaceFactory.CreateAppLog();
        workspace.Append(new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Cat", "msg", null));
        var handler = new ClearAppLogHandler(store);

        var result = await handler.HandleAsync(new ClearAppLogRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
        store.GetAll().Should().BeEmpty();
        workspace.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetStatusText()
    {
        var store = new StubAppLogStore();
        var workspace = SharedWorkspaceFactory.CreateAppLog();
        var handler = new ClearAppLogHandler(store);

        await handler.HandleAsync(new ClearAppLogRequest(Guid.NewGuid(), workspace));

        workspace.StatusText.Should().Contain("cleared");
    }

    [Fact]
    public async Task HandleAsync_WhenStoreAlreadyEmpty_ShouldSucceed()
    {
        var store = new StubAppLogStore();
        var workspace = SharedWorkspaceFactory.CreateAppLog();
        var handler = new ClearAppLogHandler(store);

        var result = await handler.HandleAsync(new ClearAppLogRequest(Guid.NewGuid(), workspace));

        result.Success.Should().BeTrue();
    }
}
