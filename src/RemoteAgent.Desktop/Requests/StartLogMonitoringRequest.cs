using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record StartLogMonitoringResult(long NextOffset);

public sealed record StartLogMonitoringRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    string ServerId,
    long ReplayFromOffset,
    StructuredLogsViewModel Workspace) : IRequest<CommandResult<StartLogMonitoringResult>>
{
    public override string ToString() =>
        $"StartLogMonitoringRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED], ServerId = {ServerId}, ReplayFromOffset = {ReplayFromOffset} }}";
}
