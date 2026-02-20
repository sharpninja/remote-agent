using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SetPairingUserHandler"/>.</summary>
[Trait("Category", "Requirements")]
public class SetPairingUserHandlerTests
{
    // When the dialog is cancelled (returns null), the handler should return Ok (no error).
    [AvaloniaFact]
    public async Task HandleAsync_WhenDialogCancelled_ShouldReturnOk()
    {
        var client = new StubCapacityClient();
        var dialog = new NullPairingUserDialog(); // always returns null
        var handler = new SetPairingUserHandler(client, dialog);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SetPairingUserRequest(
            Guid.NewGuid(), () => new Window(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // When SetPairingUsersAsync succeeds, the handler should return Ok.
    [AvaloniaFact]
    public async Task HandleAsync_WhenSetPairingUsersSucceeds_ShouldReturnOk()
    {
        var client = new StubCapacityClient { SetPairingUsersResult = true };
        var dialog = new StubPairingUserDialog { Result = new PairingUserDialogResult("alice", "hashvalue") };
        var handler = new SetPairingUserHandler(client, dialog);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SetPairingUserRequest(
            Guid.NewGuid(), () => new Window(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeTrue();
    }

    // When SetPairingUsersAsync returns false, the handler should return Fail.
    [AvaloniaFact]
    public async Task HandleAsync_WhenSetPairingUsersFails_ShouldReturnFail()
    {
        var client = new StubCapacityClient { SetPairingUsersResult = false };
        var dialog = new StubPairingUserDialog { Result = new PairingUserDialogResult("alice", "hashvalue") };
        var handler = new SetPairingUserHandler(client, dialog);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SetPairingUserRequest(
            Guid.NewGuid(), () => new Window(), "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeFalse();
    }

    // When SetPairingUsersAsync succeeds, the workspace StatusText should be updated with the username.
    [AvaloniaFact]
    public async Task HandleAsync_WhenSetPairingUsersSucceeds_ShouldUpdateWorkspaceStatus()
    {
        var client = new StubCapacityClient { SetPairingUsersResult = true };
        var dialog = new StubPairingUserDialog { Result = new PairingUserDialogResult("alice", "hashvalue") };
        var handler = new SetPairingUserHandler(client, dialog);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        await handler.HandleAsync(new SetPairingUserRequest(
            Guid.NewGuid(), () => new Window(), "127.0.0.1", 5243, null, workspace));

        workspace.StatusText.Should().Contain("alice");
    }

    // When no owner window is available, the handler should return Fail.
    [Fact]
    public async Task HandleAsync_WhenNoOwnerWindow_ShouldReturnFail()
    {
        var client = new StubCapacityClient();
        var dialog = new StubPairingUserDialog();
        var handler = new SetPairingUserHandler(client, dialog);
        var workspace = SharedWorkspaceFactory.CreateWorkspace(client);

        var result = await handler.HandleAsync(new SetPairingUserRequest(
            Guid.NewGuid(), () => null, "127.0.0.1", 5243, null, workspace));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("owner window");
    }
}
