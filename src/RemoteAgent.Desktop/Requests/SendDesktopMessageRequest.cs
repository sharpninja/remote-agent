using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SendDesktopMessageRequest(
    Guid CorrelationId,
    DesktopSessionViewModel? Session,
    string Host,
    int Port,
    string? ApiKey,
    string? PerRequestContext) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SendDesktopMessageRequest {{ CorrelationId = {CorrelationId}, Session = {Session?.Title ?? "(null)"}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
