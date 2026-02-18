using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SaveAuthUserRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    AuthUserSnapshot User,
    string? ApiKey,
    AuthUsersViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SaveAuthUserRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, UserId = {User.UserId}, ApiKey = [REDACTED] }}";
}
