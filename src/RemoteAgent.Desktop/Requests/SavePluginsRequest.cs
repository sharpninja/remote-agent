using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SavePluginsRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    IReadOnlyList<string> Assemblies,
    string? ApiKey,
    PluginsViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SavePluginsRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, Assemblies = [{Assemblies.Count}], ApiKey = [REDACTED] }}";
}
