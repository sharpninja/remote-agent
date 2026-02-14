using FluentAssertions;
using RemoteAgent.App.Services;
using Xunit;

namespace RemoteAgent.App.Tests;

public class MarkdownFormatTests
{
    [Fact]
    public void ToHtml_Null_ReturnsWrappedEmptyParagraph()
    {
        var html = MarkdownFormat.ToHtml(null!);
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("<p></p>");
        html.Should().Contain("</body>");
    }

    [Fact]
    public void ToHtml_Empty_ReturnsWrappedEmptyParagraph()
    {
        var html = MarkdownFormat.ToHtml("");
        html.Should().Contain("<p></p>");
    }

    [Theory]
    [InlineData("hello", "<p>hello</p>")]
    [InlineData("**bold**", "<p><strong>bold</strong></p>")]
    [InlineData("# Heading", "<h1")] // Markdig may add id="heading"
    [InlineData("`code`", "<code>code</code>")]
    public void ToHtml_PlainOrMarkdown_RendersExpected(string input, string expectedFragment)
    {
        var html = MarkdownFormat.ToHtml(input);
        html.Should().Contain(expectedFragment);
        if (input.StartsWith("# "))
            html.Should().Contain("Heading</h1>");
    }

    [Fact]
    public void ToHtml_WithIsError_DoesNotParseMarkdown_EscapesHtml()
    {
        var html = MarkdownFormat.ToHtml("<script>alert(1)</script>", isError: true);
        html.Should().Contain("&lt;script&gt;");
        html.Should().NotContain("<script>");
    }

    [Fact]
    public void ToHtml_List_RendersListElements()
    {
        var md = "- one\n- two";
        var html = MarkdownFormat.ToHtml(md);
        html.Should().Contain("<ul>");
        html.Should().Contain("<li>");
        html.Should().Contain("one");
        html.Should().Contain("two");
    }

    [Fact]
    public void ToHtml_CodeBlock_RendersPreAndCode()
    {
        var md = "```\nvar x = 1;\n```";
        var html = MarkdownFormat.ToHtml(md);
        html.Should().Contain("<pre>");
        html.Should().Contain("<code>");
        html.Should().Contain("var x = 1;");
    }

    [Fact]
    public void ToHtml_Link_RendersAnchor()
    {
        var html = MarkdownFormat.ToHtml("[link](https://example.com)");
        html.Should().Contain("<a ");
        html.Should().Contain("href");
        html.Should().Contain("https://example.com");
    }

    [Fact]
    public void PlainToHtml_Null_ReturnsWrappedEmptyParagraph()
    {
        var html = MarkdownFormat.PlainToHtml(null!);
        html.Should().Contain("<p></p>");
    }

    [Fact]
    public void PlainToHtml_Empty_ReturnsWrappedEmptyParagraph()
    {
        var html = MarkdownFormat.PlainToHtml("");
        html.Should().Contain("<p></p>");
    }

    [Fact]
    public void PlainToHtml_Text_EscapesAndWrapsInParagraph()
    {
        var html = MarkdownFormat.PlainToHtml("Hello & <world>");
        html.Should().Contain("Hello &amp; &lt;world&gt;");
        html.Should().Contain("<p>");
    }

    [Fact]
    public void ToHtml_AlwaysWrapsInFullDocument()
    {
        var html = MarkdownFormat.ToHtml("x");
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html>");
        html.Should().Contain("<head>");
        html.Should().Contain("<style>");
        html.Should().Contain("</body>");
        html.Should().EndWith("</html>");
    }
}
