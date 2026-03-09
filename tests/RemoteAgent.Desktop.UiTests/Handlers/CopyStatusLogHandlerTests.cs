using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="CopyStatusLogHandler"/>.</summary>
public class CopyStatusLogHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithEntries_ShouldReturnSuccess()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);
        var entries = MakeEntries("First", "Second");

        var result = await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), entries));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithEntries_ShouldWriteMarkdownHeader()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);

        await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), MakeEntries("msg")));

        clipboard.LastText.Should().StartWith("# Status Log");
    }

    [Fact]
    public async Task HandleAsync_WithEntries_ShouldIncludeAllMessages()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);
        var entries = MakeEntries("Alpha", "Beta", "Gamma");

        await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), entries));

        clipboard.LastText.Should().Contain("Alpha");
        clipboard.LastText.Should().Contain("Beta");
        clipboard.LastText.Should().Contain("Gamma");
    }

    [Fact]
    public async Task HandleAsync_WithEntries_ShouldIncludeFormattedTimestamps()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);
        var ts = new DateTimeOffset(2026, 2, 19, 14, 30, 45, TimeSpan.Zero);
        var entries = new List<StatusLogEntry> { new(ts, "Test message") };

        await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), entries));

        clipboard.LastText.Should().Contain("`2026-02-19 14:30:45`");
        clipboard.LastText.Should().Contain("Test message");
    }

    [Fact]
    public async Task HandleAsync_WithEntries_StoredNewestFirst_ShouldOutputOldestFirst()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);
        // StatusLogEntries inserts newest at index 0, so newest is first in the list
        var entries = new List<StatusLogEntry>
        {
            new(new DateTimeOffset(2026, 2, 19, 12, 0, 5, TimeSpan.Zero), "Second"),
            new(new DateTimeOffset(2026, 2, 19, 12, 0, 0, TimeSpan.Zero), "First"),
        };

        await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), entries));

        var firstIdx  = clipboard.LastText!.IndexOf("First",  StringComparison.Ordinal);
        var secondIdx = clipboard.LastText!.IndexOf("Second", StringComparison.Ordinal);
        firstIdx.Should().BeLessThan(secondIdx);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEntries_ShouldReturnFail()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);

        var result = await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), []));

        result.Success.Should().BeFalse();
        clipboard.LastText.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEntries_ShouldNotWriteToClipboard()
    {
        var clipboard = new CapturingClipboardService();
        var handler = new CopyStatusLogHandler(clipboard);

        await handler.HandleAsync(new CopyStatusLogRequest(Guid.NewGuid(), []));

        clipboard.LastText.Should().BeNull();
    }

    private static IReadOnlyList<StatusLogEntry> MakeEntries(params string[] messages) =>
        messages
            .Select((m, i) => new StatusLogEntry(DateTimeOffset.UtcNow.AddSeconds(i), m))
            .ToList();
}
