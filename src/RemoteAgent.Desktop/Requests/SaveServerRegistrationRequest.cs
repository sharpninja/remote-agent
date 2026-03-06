using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.Requests;

public sealed record SaveServerRegistrationRequest(
    Guid CorrelationId,
    string? ExistingServerId,
    string DisplayName,
    string Host,
    int Port,
    string ApiKey,
    string PerRequestContext = "",
    string DefaultSessionContext = "") : IRequest<CommandResult<ServerRegistration>>
{
    public override string ToString() =>
        $"SaveServerRegistrationRequest {{ CorrelationId = {CorrelationId}, ExistingServerId = {ExistingServerId}, DisplayName = {DisplayName}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
