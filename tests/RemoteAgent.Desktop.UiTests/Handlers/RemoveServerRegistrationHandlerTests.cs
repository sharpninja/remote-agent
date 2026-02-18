using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.UiTests.TestHelpers;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class RemoveServerRegistrationHandlerTests
{
    private readonly InMemoryServerRegistrationStore _store = new();

    private RemoveServerRegistrationHandler CreateHandler() => new(_store);

    [Fact]
    public async Task HandleAsync_WithExistingServer_ShouldReturnOk()
    {
        _store.Upsert(new ServerRegistration
        {
            ServerId = "srv-1",
            DisplayName = "Test",
            Host = "localhost",
            Port = 5243,
            ApiKey = ""
        });
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "srv-1", "Test");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentServer_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "does-not-exist", "Ghost");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Ghost");
    }

    [Fact]
    public async Task HandleAsync_WithEmptyServerId_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "", "Empty");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Server ID");
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceServerId_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "   ", "Whitespace");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveFromStore()
    {
        _store.Upsert(new ServerRegistration
        {
            ServerId = "srv-to-remove",
            DisplayName = "Remove Me",
            Host = "localhost",
            Port = 5243,
            ApiKey = ""
        });
        var handler = CreateHandler();

        await handler.HandleAsync(new RemoveServerRegistrationRequest(Guid.NewGuid(), "srv-to-remove", "Remove Me"));

        _store.GetAll().Should().NotContain(s => s.ServerId == "srv-to-remove");
    }
}
