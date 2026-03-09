using Avalonia.Controls;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

/// <summary>CQRS request to show the Set Pairing User dialog and call the service RPC.</summary>
public sealed record SetPairingUserRequest(
    Guid CorrelationId,
    Func<Window?> OwnerWindowFactory,
    string Host,
    int Port,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    /// <inheritdoc />
    public override string ToString() =>
        $"SetPairingUserRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
