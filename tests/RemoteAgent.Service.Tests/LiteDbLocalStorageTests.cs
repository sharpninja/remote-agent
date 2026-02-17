using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Storage;

namespace RemoteAgent.Service.Tests;

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
