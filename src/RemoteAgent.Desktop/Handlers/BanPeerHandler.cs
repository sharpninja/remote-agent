using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class BanPeerHandler(IServerCapacityClient client)
    : IRequestHandler<BanPeerRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(BanPeerRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.BanPeerAsync(
            request.Host, request.Port, request.Peer, request.Reason, request.ApiKey, cancellationToken);

        request.Workspace.StatusText = ok ? $"Peer banned: {request.Peer}" : $"Failed to ban peer: {request.Peer}";

        // Refresh security data inline
        var abandoned = await client.GetAbandonedSessionsAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        var peers = await client.GetConnectedPeersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        var history = await client.GetConnectionHistoryAsync(request.Host, request.Port, 500, request.ApiKey, cancellationToken);
        var banned = await client.GetBannedPeersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);

        request.Workspace.AbandonedServerSessions.Clear();
        foreach (var row in abandoned)
            request.Workspace.AbandonedServerSessions.Add(row);

        request.Workspace.ConnectedPeers.Clear();
        foreach (var peer in peers)
            request.Workspace.ConnectedPeers.Add(peer);
        request.Workspace.SelectedConnectedPeer = request.Workspace.ConnectedPeers.FirstOrDefault();

        request.Workspace.ConnectionHistory.Clear();
        foreach (var row in history)
            request.Workspace.ConnectionHistory.Add(row);

        request.Workspace.BannedPeers.Clear();
        foreach (var row in banned)
            request.Workspace.BannedPeers.Add(row);
        request.Workspace.SelectedBannedPeer = request.Workspace.BannedPeers.FirstOrDefault();

        return CommandResult.Ok();
    }
}
