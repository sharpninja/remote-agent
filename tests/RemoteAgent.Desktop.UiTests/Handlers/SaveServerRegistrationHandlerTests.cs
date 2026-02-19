using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.UiTests.TestHelpers;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SaveServerRegistrationHandler"/>. FR-12.9; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.9")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SaveServerRegistrationHandlerTests
{
    private readonly InMemoryServerRegistrationStore _store = new();

    private SaveServerRegistrationHandler CreateHandler() => new(_store);

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithValidData_ShouldReturnOk()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test Server", "192.168.1.1", 5243, "secret");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Host.Should().Be("192.168.1.1");
        result.Data.Port.Should().Be(5243);
        result.Data.DisplayName.Should().Be("Test Server");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithExistingServerId_ShouldUpsert()
    {
        var handler = CreateHandler();
        var existing = _store.Upsert(new ServerRegistration
        {
            ServerId = "srv-1",
            DisplayName = "Old Name",
            Host = "127.0.0.1",
            Port = 5243,
            ApiKey = ""
        });

        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), "srv-1", "Updated Name", "10.0.0.1", 8080, "newkey");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.ServerId.Should().Be("srv-1");
        result.Data.DisplayName.Should().Be("Updated Name");
        result.Data.Host.Should().Be("10.0.0.1");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithEmptyHost_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test", "", 5243, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("host");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithWhitespaceHost_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test", "   ", 5243, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithInvalidPort_Zero_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test", "localhost", 0, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("port");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithInvalidPort_TooHigh_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test", "localhost", 70000, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("port");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithNegativePort_ShouldReturnFail()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "Test", "localhost", -1, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeFalse();
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithBlankDisplayName_ShouldUseHostPort()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "", "myhost", 9999, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.DisplayName.Should().Be("myhost:9999");
    }

    // FR-12.9, TR-18.4
    [Fact]
    public async Task HandleAsync_WithNullServerId_ShouldGenerateNewId()
    {
        var handler = CreateHandler();
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), null, "New", "host", 1234, "");

        var result = await handler.HandleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.ServerId.Should().NotBeNullOrWhiteSpace();
    }

    // FR-12.9, TR-18.4
    [Fact]
    public void Request_ToString_ShouldRedactApiKey()
    {
        var request = new SaveServerRegistrationRequest(
            Guid.NewGuid(), "srv-1", "Test", "host", 5243, "supersecret");

        var str = request.ToString();

        str.Should().Contain("[REDACTED]");
        str.Should().NotContain("supersecret");
    }
}
