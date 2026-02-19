using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Logging;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Tests for <see cref="StructuredLogService"/>. FR-1.5; TR-3.6, TR-13.1, TR-13.2, TR-18.1, TR-18.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.5")]
[Trait("Requirement", "TR-3.6")]
[Trait("Requirement", "TR-13.1")]
[Trait("Requirement", "TR-13.2")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
public class StructuredLogServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ra-logs-tests-" + Guid.NewGuid().ToString("N"));

    public StructuredLogServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Write_AssignsMonotonicEventIds_AndPersistsJsonl()
    {
        using var sut = NewService();
        var id1 = sut.Write("info", "evt1", "msg1", "comp", "s1", "c1", "{\"a\":1}");
        var id2 = sut.Write("warn", "evt2", "msg2", "comp", "s2", "c2", null);

        id2.Should().BeGreaterThan(id1);
        File.Exists(sut.FilePath).Should().BeTrue();

        var lines = File.ReadAllLines(sut.FilePath);
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("\"event_id\"");
        lines[0].Should().Contain("\"session_id\"");
        lines[0].Should().Contain("\"correlation_id\"");
    }

    [Fact]
    public void GetSnapshot_FiltersByOffset_AndHonorsLimit()
    {
        using var sut = NewService();
        var id1 = sut.Write("info", "evt1", "m1", "comp", "s1", "c1");
        var id2 = sut.Write("info", "evt2", "m2", "comp", "s2", "c2");
        _ = sut.Write("info", "evt3", "m3", "comp", "s3", "c3");

        var rows = sut.GetSnapshot(id1, 1);
        rows.Should().HaveCount(1);
        rows[0].EventId.Should().Be(id2);
    }

    [Fact]
    public void GetSnapshot_LimitZero_ReturnsAllFromOffset()
    {
        using var sut = NewService();
        var id1 = sut.Write("info", "evt1", "m1", "comp", "s1", "c1");
        _ = sut.Write("info", "evt2", "m2", "comp", "s2", "c2");
        _ = sut.Write("info", "evt3", "m3", "comp", "s3", "c3");

        var rows = sut.GetSnapshot(id1, 0);
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task StreamFromOffset_YieldsNewRows()
    {
        using var sut = NewService();
        var firstId = sut.Write("info", "evt1", "m1", "comp", "s1", "c1");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var readTask = Task.Run(async () =>
        {
            await foreach (var row in sut.StreamFromOffset(firstId, cts.Token))
                return row;
            throw new InvalidOperationException("No row streamed");
        }, cts.Token);

        await Task.Delay(150, cts.Token);
        var secondId = sut.Write("info", "evt2", "m2", "comp", "s2", "c2");
        var streamed = await readTask;
        streamed.EventId.Should().Be(secondId);
    }

    private StructuredLogService NewService()
    {
        var options = Options.Create(new AgentOptions { LogDirectory = _tempDir });
        return new StructuredLogService(options, NullLogger<StructuredLogService>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
