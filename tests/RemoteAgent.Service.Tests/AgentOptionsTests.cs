using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Tests for <see cref="AgentOptions"/>. TR-1.3, TR-2.2, TR-3.1, TR-3.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "TR-1.3")]
[Trait("Requirement", "TR-2.2")]
[Trait("Requirement", "TR-3.1")]
[Trait("Requirement", "TR-3.2")]
public class AgentOptionsTests
{
    [Fact]
    public void SectionName_IsAgent()
    {
        AgentOptions.SectionName.Should().Be("Agent");
    }

    [Fact]
    public void Options_CanBeBound()
    {
        var options = new AgentOptions
        {
            Command = "/usr/bin/cat",
            Arguments = "-n",
            LogDirectory = "/var/log/agent"
        };
        options.Command.Should().Be("/usr/bin/cat");
        options.Arguments.Should().Be("-n");
        options.LogDirectory.Should().Be("/var/log/agent");
    }

    [Fact]
    public void Options_CanBeNull()
    {
        var options = new AgentOptions();
        options.Command.Should().BeNull();
        options.Arguments.Should().BeNull();
        options.LogDirectory.Should().BeNull();
    }

    [Fact]
    public void Options_WorksWithIOptions()
    {
        var options = Options.Create(new AgentOptions { Command = "echo" });
        options.Value.Command.Should().Be("echo");
    }
}
