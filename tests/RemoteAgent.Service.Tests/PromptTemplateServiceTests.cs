using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

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
