using FluentAssertions;
using RemoteAgent.App.Services;
using Xunit;

namespace RemoteAgent.App.Tests;

public class ChatMessageTests
{
    [Fact]
    public void DisplayText_WhenIsEvent_ReturnsEventMessage()
    {
        var msg = new ChatMessage { IsEvent = true, EventMessage = "Started" };
        msg.DisplayText.Should().Be("Started");
    }

    [Fact]
    public void DisplayText_WhenIsUser_ReturnsText()
    {
        var msg = new ChatMessage { IsUser = true, Text = "hello" };
        msg.DisplayText.Should().Be("hello");
    }

    [Fact]
    public void DisplayText_WhenAgent_ReturnsText()
    {
        var msg = new ChatMessage { IsUser = false, IsEvent = false, Text = "agent said" };
        msg.DisplayText.Should().Be("agent said");
    }

    [Fact]
    public void DisplayText_WhenEventAndNullEventMessage_ReturnsEmpty()
    {
        var msg = new ChatMessage { IsEvent = true, EventMessage = null };
        msg.DisplayText.Should().Be("");
    }

    [Fact]
    public void RenderedHtml_WhenIsUser_ReturnsPlainHtml()
    {
        var msg = new ChatMessage { IsUser = true, Text = "say hi" };
        msg.RenderedHtml.Should().Contain("<p>");
        msg.RenderedHtml.Should().Contain("say hi");
        msg.RenderedHtml.Should().NotContain("<strong>");
    }

    [Fact]
    public void RenderedHtml_WhenIsEvent_ReturnsPlainHtml()
    {
        var msg = new ChatMessage { IsEvent = true, EventMessage = "SessionStarted" };
        msg.RenderedHtml.Should().Contain("SessionStarted");
    }

    [Fact]
    public void RenderedHtml_WhenAgentOutput_RendersMarkdown()
    {
        var msg = new ChatMessage { IsUser = false, IsEvent = false, Text = "**bold**" };
        msg.RenderedHtml.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void RenderedHtml_WhenIsError_DoesNotRenderMarkdown()
    {
        var msg = new ChatMessage { IsUser = false, IsEvent = false, IsError = true, Text = "**not bold**" };
        msg.RenderedHtml.Should().NotContain("<strong>");
        msg.RenderedHtml.Should().Contain("**not bold**");
    }

    [Fact]
    public void RenderedHtml_WhenAgent_AlwaysFullDocument()
    {
        var msg = new ChatMessage { IsUser = false, IsEvent = false, Text = "ok" };
        msg.RenderedHtml.Should().StartWith("<!DOCTYPE html>");
        msg.RenderedHtml.Should().EndWith("</html>");
    }

    [Fact]
    public void Priority_DefaultsToNormal()
    {
        var msg = new ChatMessage { Text = "hi" };
        msg.Priority.Should().Be(ChatMessagePriority.Normal);
    }

    [Fact]
    public void IsArchived_WhenSet_RaisesPropertyChanged()
    {
        var msg = new ChatMessage { Text = "hi" };
        var raised = false;
        msg.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ChatMessage.IsArchived)) raised = true; };
        msg.IsArchived = true;
        raised.Should().BeTrue();
        msg.IsArchived.Should().BeTrue();
    }
}
