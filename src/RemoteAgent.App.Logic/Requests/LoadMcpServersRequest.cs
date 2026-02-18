using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.ViewModels;

namespace RemoteAgent.App.Logic.Requests;

public sealed record LoadMcpServersRequest(
    Guid CorrelationId,
    McpRegistryPageViewModel Workspace) : IRequest<CommandResult>;
