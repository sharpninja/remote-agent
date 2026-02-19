using FluentAssertions;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Tests;

/// <summary>Core tests for <see cref="RemoteAgent.App.Logic.PromptTemplateEngine"/> variable extraction and substitution. FR-14.2; TR-17.3.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-14.2")]
[Trait("Requirement", "TR-17.3")]
public class PromptTemplateEngineTests
{
    [Fact]
    public void ExtractVariables_ReturnsDistinctVariables()
    {
        var vars = PromptTemplateEngine.ExtractVariables("Incident {{incident_id}} in {{service}} and {{service}}");

        vars.Should().Contain("incident_id");
        vars.Should().Contain("service");
        vars.Count.Should().Be(2);
    }

    [Fact]
    public void ExtractVariables_SupportsDotAndDashNames()
    {
        var vars = PromptTemplateEngine.ExtractVariables("Use {{ticket.id}} with {{service-name}}");

        vars.Should().Contain("ticket.id");
        vars.Should().Contain("service-name");
        vars.Count.Should().Be(2);
    }

    [Fact]
    public void Render_RendersHandlebarsTemplate()
    {
        var result = PromptTemplateEngine.Render(
            "Issue {{id}} affects {{service}}",
            new Dictionary<string, string?>
            {
                ["id"] = "INC-123",
                ["service"] = "payments"
            });

        result.Should().Contain("INC-123");
        result.Should().Contain("payments");
    }
}
