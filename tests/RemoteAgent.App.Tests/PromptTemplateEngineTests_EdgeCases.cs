using FluentAssertions;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Tests;

/// <summary>Prompt template engine: empty/missing variables, Handlebars edge cases. FR-14.2, TR-17.3.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-14.2")]
[Trait("Requirement", "TR-17.3")]
public sealed class PromptTemplateEngineTests_EdgeCases
{
    [Fact]
    public void ExtractVariables_ReturnsEmpty_WhenTemplateNullOrWhitespace()
    {
        PromptTemplateEngine.ExtractVariables(null!).Should().BeEmpty();
        PromptTemplateEngine.ExtractVariables("").Should().BeEmpty();
        PromptTemplateEngine.ExtractVariables("   ").Should().BeEmpty();
    }

    [Fact]
    public void ExtractVariables_ReturnsEmpty_WhenNoPlaceholders()
    {
        var vars = PromptTemplateEngine.ExtractVariables("Hello world, no placeholders here.");
        vars.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{{a}}", "a")]
    [InlineData("{{ a }}", "a")]
    [InlineData("{{  incident_id  }}", "incident_id")]
    public void ExtractVariables_HandlesWhitespaceInsideBraces(string template, string expectedVar)
    {
        var vars = PromptTemplateEngine.ExtractVariables(template);
        vars.Should().ContainSingle().Which.Should().Be(expectedVar);
    }

    [Fact]
    public void Render_WithMissingVariable_ReplacesWithEmpty()
    {
        // Handlebars renders missing keys as empty by default
        var result = PromptTemplateEngine.Render(
            "Issue {{id}} for {{missing}}",
            new Dictionary<string, string?> { ["id"] = "INC-1" });

        result.Should().Contain("INC-1");
        result.Should().Contain("for ");
    }

    [Fact]
    public void Render_WithNullValue_ReplacesWithEmpty()
    {
        var result = PromptTemplateEngine.Render(
            "Hello {{name}}",
            new Dictionary<string, string?> { ["name"] = null! });

        result.Should().Contain("Hello ");
    }

    [Fact]
    public void Render_EmptyTemplate_ReturnsEmpty()
    {
        var result = PromptTemplateEngine.Render("", new Dictionary<string, string?>());
        result.Should().BeEmpty();
    }
}
