using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.UiTests.TestHelpers;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="RemoveServerRegistrationHandler"/>. FR-12.9; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.9")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class RemoveServerRegistrationHandlerTests
{
    private readonly InMemoryServerRegistrationStore _store = new();

    private RemoveServerRegistrationHandler CreateHandler() => new(_store);

    // FR-12.9, TR-18.4
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

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithNonExistentServer_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "does-not-exist", "Ghost");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Ghost");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithEmptyServerId_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "", "Empty");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Server ID");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithWhitespaceServerId_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new RemoveServerRegistrationRequest(Guid.NewGuid(), "   ", "Whitespace");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
    }

    // FR-12.9, TR-18.4
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
