using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Services;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Tests for <see cref="PromptTemplateService"/>. FR-14.1, FR-14.2; TR-17.1, TR-17.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-14.1")]
[Trait("Requirement", "FR-14.2")]
[Trait("Requirement", "TR-17.1")]
[Trait("Requirement", "TR-17.2")]
public class PromptTemplateServiceTests
{
    [Fact]
    public void List_SeedsDefaults_WhenEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var service = new PromptTemplateService(Options.Create(new AgentOptions { DataDirectory = dir }));
        var rows = service.List();

        rows.Should().NotBeEmpty();
        rows.Should().Contain(x => x.TemplateId == "bug-triage");
    }

    [Fact]
    public void Upsert_ThenDelete_Works()
    {
        var dir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var service = new PromptTemplateService(Options.Create(new AgentOptions { DataDirectory = dir }));
        var row = service.Upsert(new PromptTemplateRecord
        {
            TemplateId = "custom-template",
            DisplayName = "Custom Template",
            TemplateContent = "Hello {{name}}"
        });

        row.TemplateId.Should().Be("custom-template");

        var listed = service.List();
        listed.Should().Contain(x => x.TemplateId == "custom-template");

        var deleted = service.Delete("custom-template");
        deleted.Should().BeTrue();
    }
}
