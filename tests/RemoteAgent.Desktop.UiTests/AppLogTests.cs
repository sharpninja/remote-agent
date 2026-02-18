using FluentAssertions;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests;

[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.12")]
[Trait("Requirement", "FR-12.12.1")]
[Trait("Requirement", "FR-12.12.3")]
[Trait("Requirement", "FR-12.12.4")]
public sealed class AppLogTests
{
    // ── FR-12.12.1: custom ILogger captures log entries ──────────────────────

    [Fact]
    public void AppLoggerProvider_ShouldCaptureLogEntries()
    {
        var store = new InMemoryAppLogStore();
        var provider = new AppLoggerProvider(store);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Hello {Name}", "world");
        logger.LogWarning("Something happened");
        logger.LogError(new InvalidOperationException("boom"), "Error detail");

        var entries = store.GetAll();
        entries.Should().HaveCount(3);

        entries[0].Level.Should().Be(LogLevel.Information);
        entries[0].Category.Should().Be("TestCategory");
        entries[0].Message.Should().Contain("Hello");

        entries[1].Level.Should().Be(LogLevel.Warning);

        entries[2].Level.Should().Be(LogLevel.Error);
        entries[2].ExceptionMessage.Should().Contain("boom");
    }

    // ── FR-12.12.3: ClearCommand empties the collection ──────────────────────

    [Fact]
    public async Task ClearAppLogHandler_ShouldEmptyCollection()
    {
        var store = new InMemoryAppLogStore();
        store.Add(new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Cat", "Msg", null));
        store.Add(new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Warning, "Cat", "Msg2", null));

        var vm = new AppLogViewModel(new NullDispatcher(), new NullFileSaveDialogService());
        vm.Entries.Add(store.GetAll()[0]);
        vm.Entries.Add(store.GetAll()[1]);

        var handler = new ClearAppLogHandler(store);
        var result = await handler.HandleAsync(new ClearAppLogRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.Entries.Should().BeEmpty();
        store.GetAll().Should().BeEmpty();
        vm.StatusText.Should().Be("App log cleared.");
    }

    // ── FR-12.12.4: SaveCommand writes txt, json, and csv ────────────────────

    [Theory]
    [InlineData("txt")]
    [InlineData("json")]
    [InlineData("csv")]
    public async Task SaveAppLogHandler_ShouldWriteAllThreeFormats(string format)
    {
        var entries = new List<AppLogEntry>
        {
            new(DateTimeOffset.UtcNow, LogLevel.Information, "TestCat", "Hello world", null),
            new(DateTimeOffset.UtcNow, LogLevel.Error, "TestCat", "An error occurred", "System.Exception: boom")
        };

        var vm = new AppLogViewModel(new NullDispatcher(), new NullFileSaveDialogService());
        var filePath = Path.Combine(Path.GetTempPath(), $"applog-test-{Guid.NewGuid():N}.{format}");

        try
        {
            var handler = new SaveAppLogHandler();
            var result = await handler.HandleAsync(
                new SaveAppLogRequest(Guid.NewGuid(), entries, format, filePath, vm));

            result.Success.Should().BeTrue();
            File.Exists(filePath).Should().BeTrue();

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("Hello world");
            content.Should().Contain("An error occurred");

            vm.StatusText.Should().Contain(filePath);
            vm.StatusText.Should().Contain(format.ToUpperInvariant());
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private sealed class NullDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResponse)!);
    }

    private sealed class NullFileSaveDialogService : IFileSaveDialogService
    {
        public Task<string?> GetSaveFilePathAsync(string suggestedName, string extension, string filterDescription)
            => Task.FromResult<string?>(null);
    }
}
