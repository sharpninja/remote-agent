using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record AuthUsersData(
    IReadOnlyList<AuthUserSnapshot> Users,
    IReadOnlyList<string> Roles);

public sealed record RefreshAuthUsersRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    AuthUsersViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshAuthUsersRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
