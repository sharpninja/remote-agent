using FluentAssertions;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="OpenLogsFolderHandler"/>.</summary>
public class OpenLogsFolderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFolderExists_ShouldReturnSuccess()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Path.GetTempPath();

        var result = await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenFolderExists_ShouldCallOpenFolder()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Path.GetTempPath();

        await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));

        folder.LastPath.Should().Be(path);
    }

    [Fact]
    public async Task HandleAsync_WhenFolderDoesNotExist_ShouldReturnFail()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");

        var result = await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenFolderDoesNotExist_ShouldNotCallOpenFolder()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");

        await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));

        folder.LastPath.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenFolderDoesNotExist_ShouldIncludePathInError()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");

        var result = await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));

        result.ErrorMessage.Should().Contain(path);
    }

    [Fact]
    public async Task HandleAsync_WithRealTempFolder_ShouldOpenExactPath()
    {
        var folder = new CapturingFolderOpenerService();
        var handler = new OpenLogsFolderHandler(folder);
        var path = Directory.CreateTempSubdirectory("remote-agent-test-").FullName;

        try
        {
            await handler.HandleAsync(new OpenLogsFolderRequest(Guid.NewGuid(), path));
            folder.LastPath.Should().Be(path);
        }
        finally
        {
            Directory.Delete(path);
        }
    }
}
