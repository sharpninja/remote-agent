using Avalonia.Headless.XUnit;
using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SendDesktopMessageHandler"/>. FR-2.1, FR-2.2; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-2.1")]
[Trait("Requirement", "FR-2.2")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SendDesktopMessageHandlerTests
{
    // FR-2.1, FR-2.2, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenSessionIsNull_ShouldReturnFail()
    {
        var handler = new SendDesktopMessageHandler();
        var result = await handler.HandleAsync(new SendDesktopMessageRequest(
            Guid.NewGuid(), null, "127.0.0.1", 5243, null, null));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No session");
    }

    // FR-2.1, FR-2.2, TR-18.4
    [AvaloniaFact]
    public async Task HandleAsync_WhenMessageEmpty_ShouldReturnFail()
    {
        var session = new DesktopSessionViewModel(new FakeAgentSession())
        {
            Title = "Test", PendingMessage = "   "
        };
        var handler = new SendDesktopMessageHandler();

        var result = await handler.HandleAsync(new SendDesktopMessageRequest(
            Guid.NewGuid(), session, "127.0.0.1", 5243, null, null));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    // FR-2.1, FR-2.2, TR-18.4
    [AvaloniaFact]
    public async Task HandleAsync_WhenConnected_ShouldSendAndClearMessage()
    {
        var fakeSession = new FakeAgentSession();
        await fakeSession.ConnectAsync("127.0.0.1", 5243);
        var session = new DesktopSessionViewModel(fakeSession)
        {
            Title = "Test", PendingMessage = "Hello"
        };
        var handler = new SendDesktopMessageHandler();

        var result = await handler.HandleAsync(new SendDesktopMessageRequest(
            Guid.NewGuid(), session, "127.0.0.1", 5243, null, null));

        result.Success.Should().BeTrue();
        session.PendingMessage.Should().BeEmpty();
        session.Messages.Should().Contain(m => m.Contains("Hello"));
    }

    // FR-2.1, FR-2.2, TR-18.4
    [AvaloniaFact]
    public async Task HandleAsync_WhenNotConnected_ShouldReconnectAndSend()
    {
        var fakeSession = new FakeAgentSession();
        var session = new DesktopSessionViewModel(fakeSession)
        {
            Title = "Test", PendingMessage = "Hello reconnect"
        };
        var handler = new SendDesktopMessageHandler();

        var result = await handler.HandleAsync(new SendDesktopMessageRequest(
            Guid.NewGuid(), session, "127.0.0.1", 5243, null, null));

        result.Success.Should().BeTrue();
        fakeSession.IsConnected.Should().BeTrue();
    }

    // FR-2.1, FR-2.2, TR-18.4
    [Fact]
    public async Task HandleAsync_WhenNotConnectedAndHostEmpty_ShouldReturnFail()
    {
        var fakeSession = new FakeAgentSession();
        var session = new DesktopSessionViewModel(fakeSession)
        {
            Title = "Test", PendingMessage = "Hello"
        };
        var handler = new SendDesktopMessageHandler();

        var result = await handler.HandleAsync(new SendDesktopMessageRequest(
            Guid.NewGuid(), session, "", 5243, null, null));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Host");
    }
}
