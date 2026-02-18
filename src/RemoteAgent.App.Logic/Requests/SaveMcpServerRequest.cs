using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.ViewModels;

namespace RemoteAgent.App.Logic.Requests;

public sealed record SaveMcpServerRequest(
    Guid CorrelationId,
    McpRegistryPageViewModel Workspace) : IRequest<CommandResult>;
