using FluentAssertions;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class SaveAppLogHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFilePathEmpty_ShouldReturnFail()
    {
        var handler = new SaveAppLogHandler();
        var workspace = SharedWorkspaceFactory.CreateAppLog();

        var result = await handler.HandleAsync(new SaveAppLogRequest(
            Guid.NewGuid(),
            Array.Empty<AppLogEntry>(),
            "txt",
            "",
            workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File path");
    }

    [Fact]
    public async Task HandleAsync_WhenPathIsInvalid_ShouldReturnFail()
    {
        var handler = new SaveAppLogHandler();
        var workspace = SharedWorkspaceFactory.CreateAppLog();

        var result = await handler.HandleAsync(new SaveAppLogRequest(
            Guid.NewGuid(),
            Array.Empty<AppLogEntry>(),
            "txt",
            "/nonexistent/path/that/cannot/exist/file.txt",
            workspace));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenPathIsValid_ShouldWriteFileAndReturnOk()
    {
        var path = Path.GetTempFileName();
        try
        {
            var handler = new SaveAppLogHandler();
            var workspace = SharedWorkspaceFactory.CreateAppLog();
            var entries = new[]
            {
                new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Cat", "Test message", null)
            };

            var result = await handler.HandleAsync(new SaveAppLogRequest(
                Guid.NewGuid(), entries, "txt", path, workspace));

            result.Success.Should().BeTrue();
            File.Exists(path).Should().BeTrue();
            var content = await File.ReadAllTextAsync(path);
            content.Should().Contain("Test message");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task HandleAsync_JsonFormat_ShouldWriteJsonContent()
    {
        var path = Path.GetTempFileName();
        try
        {
            var handler = new SaveAppLogHandler();
            var workspace = SharedWorkspaceFactory.CreateAppLog();
            var entries = new[]
            {
                new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Warning, "TestCat", "Warning msg", null)
            };

            var result = await handler.HandleAsync(new SaveAppLogRequest(
                Guid.NewGuid(), entries, "json", path, workspace));

            result.Success.Should().BeTrue();
            var content = await File.ReadAllTextAsync(path);
            content.Should().Contain("Warning msg");
            content.Should().StartWith("[");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task HandleAsync_CsvFormat_ShouldWriteCsvContent()
    {
        var path = Path.GetTempFileName();
        try
        {
            var handler = new SaveAppLogHandler();
            var workspace = SharedWorkspaceFactory.CreateAppLog();
            var entries = new[]
            {
                new AppLogEntry(DateTimeOffset.UtcNow, LogLevel.Error, "TestCat", "Error msg", "ex message")
            };

            var result = await handler.HandleAsync(new SaveAppLogRequest(
                Guid.NewGuid(), entries, "csv", path, workspace));

            result.Success.Should().BeTrue();
            var content = await File.ReadAllTextAsync(path);
            content.Should().Contain("Timestamp,Level,Category,Message");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
