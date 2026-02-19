using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Storage;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Tests for <see cref="LiteDbLocalStorage"/>. TR-3.6, TR-11.1, TR-18.1.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "TR-3.6")]
[Trait("Requirement", "TR-11.1")]
[Trait("Requirement", "TR-18.1")]
public class LiteDbLocalStorageTests
{
    [Fact]
    public void SessionExists_ReturnsTrue_WhenEntriesExist()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);

        var storage = new LiteDbLocalStorage(Options.Create(new AgentOptions { DataDirectory = dataDir }));
        storage.SessionExists("sess-1").Should().BeFalse();

        storage.LogRequest("sess-1", "Text", "hello");
        storage.SessionExists("sess-1").Should().BeTrue();
    }
}
