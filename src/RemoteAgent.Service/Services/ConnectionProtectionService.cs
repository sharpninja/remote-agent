using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Logging;

namespace RemoteAgent.Service.Services;

/// <summary>Per-peer connection/message rate-limiting, DoS detection, peer bans, and connection-history telemetry.</summary>
public sealed class ConnectionProtectionService(
    IOptions<AgentOptions> options,
    StructuredLogService structuredLogs)
{
    private readonly ConcurrentDictionary<string, PeerState> _peerStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _historyGate = new();
    private readonly object _banGate = new();
    private readonly Queue<ConnectionHistoryEntry> _history = new();
    private readonly Dictionary<string, BannedPeerEntry> _bannedPeers = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxHistoryRows = 5000;

    public ConnectionDecision TryOpenConnection(string? peer, string component)
    {
        var normalizedPeer = NormalizePeer(peer);
        if (IsPeerBanned(normalizedPeer))
        {
            AppendHistory(normalizedPeer, "connection_open", false, "Peer is banned.");
            structuredLogs.Write("WARN", "peer_banned_connection", "Connection denied for banned peer", component, null, null, $"{{\"peer\":\"{normalizedPeer}\"}}");
            return ConnectionDecision.CreateDenied(normalizedPeer, "Peer is banned.");
        }

        if (!options.Value.EnableConnectionProtection)
        {
            AppendHistory(normalizedPeer, "connection_open", true, null);
            return ConnectionDecision.CreateAllowed(normalizedPeer);
        }

        var now = DateTime.UtcNow;
        var state = _peerStates.GetOrAdd(normalizedPeer, _ => new PeerState());
        var attemptWindow = TimeSpan.FromSeconds(Math.Max(1, options.Value.ConnectionAttemptWindowSeconds));

        lock (state.Gate)
        {
            Prune(state.ConnectionAttemptsUtc, now - attemptWindow);
            if (state.BlockedUntilUtc > now)
            {
                var detail = $"{{\"peer\":\"{normalizedPeer}\",\"reason\":\"blocked\",\"blocked_until\":\"{state.BlockedUntilUtc:O}\"}}";
                structuredLogs.Write("WARN", "dos_blocked_connection", "Connection blocked for peer under DoS cooldown", component, null, null, detail);
                AppendHistory(normalizedPeer, "connection_open", false, "Peer is temporarily blocked.");
                return ConnectionDecision.CreateDenied(normalizedPeer, "Connection temporarily blocked due to suspicious traffic.");
            }

            if (state.ConnectionAttemptsUtc.Count >= Math.Max(1, options.Value.MaxConnectionAttemptsPerWindow))
            {
                RegisterViolation(state, now, normalizedPeer, component, "connection_attempt_rate_limited");
                AppendHistory(normalizedPeer, "connection_open", false, "Too many connection attempts.");
                return ConnectionDecision.CreateDenied(normalizedPeer, "Too many connection attempts.");
            }

            if (state.ActiveConnections >= Math.Max(1, options.Value.MaxConcurrentConnectionsPerPeer))
            {
                RegisterViolation(state, now, normalizedPeer, component, "connection_concurrency_limited");
                AppendHistory(normalizedPeer, "connection_open", false, "Too many active connections.");
                return ConnectionDecision.CreateDenied(normalizedPeer, "Too many active connections.");
            }

            state.ConnectionAttemptsUtc.Enqueue(now);
            state.ActiveConnections++;
            state.LastSeenUtc = now;
        }

        AppendHistory(normalizedPeer, "connection_open", true, null);
        return ConnectionDecision.CreateAllowed(normalizedPeer);
    }

    public void CloseConnection(string? normalizedPeer)
    {
        if (string.IsNullOrWhiteSpace(normalizedPeer))
            return;

        if (!_peerStates.TryGetValue(normalizedPeer, out var state))
            return;

        lock (state.Gate)
        {
            if (state.ActiveConnections > 0)
                state.ActiveConnections--;
            state.LastSeenUtc = DateTime.UtcNow;
        }

        AppendHistory(normalizedPeer, "connection_close", true, null);
    }

    public bool TryRegisterClientMessage(string? normalizedPeer, string component)
    {
        if (string.IsNullOrWhiteSpace(normalizedPeer))
            return true;

        if (IsPeerBanned(normalizedPeer))
        {
            AppendHistory(normalizedPeer, "client_message", false, "Peer is banned.");
            structuredLogs.Write("WARN", "peer_banned_message", "Client message denied for banned peer", component, null, null, $"{{\"peer\":\"{normalizedPeer}\"}}");
            return false;
        }

        if (!options.Value.EnableConnectionProtection)
        {
            AppendHistory(normalizedPeer, "client_message", true, null);
            return true;
        }

        var now = DateTime.UtcNow;
        var state = _peerStates.GetOrAdd(normalizedPeer, _ => new PeerState());
        var messageWindow = TimeSpan.FromSeconds(Math.Max(1, options.Value.ClientMessageWindowSeconds));

        lock (state.Gate)
        {
            if (state.BlockedUntilUtc > now)
            {
                var detail = $"{{\"peer\":\"{normalizedPeer}\",\"reason\":\"blocked\",\"blocked_until\":\"{state.BlockedUntilUtc:O}\"}}";
                structuredLogs.Write("WARN", "dos_blocked_message", "Client message blocked for peer under DoS cooldown", component, null, null, detail);
                AppendHistory(normalizedPeer, "client_message", false, "Peer is temporarily blocked.");
                return false;
            }

            Prune(state.MessageTimestampsUtc, now - messageWindow);
            if (state.MessageTimestampsUtc.Count >= Math.Max(1, options.Value.MaxClientMessagesPerWindow))
            {
                RegisterViolation(state, now, normalizedPeer, component, "message_rate_limited");
                AppendHistory(normalizedPeer, "client_message", false, "Message rate limited.");
                return false;
            }

            state.MessageTimestampsUtc.Enqueue(now);
            state.LastSeenUtc = now;
            AppendHistory(normalizedPeer, "client_message", true, null);
            return true;
        }
    }

    public IReadOnlyList<ConnectedPeerSnapshot> GetConnectedPeers()
    {
        var now = DateTime.UtcNow;
        var rows = new List<ConnectedPeerSnapshot>();
        foreach (var kvp in _peerStates)
        {
            lock (kvp.Value.Gate)
            {
                rows.Add(new ConnectedPeerSnapshot(
                    Peer: kvp.Key,
                    ActiveConnections: kvp.Value.ActiveConnections,
                    IsBlocked: kvp.Value.BlockedUntilUtc > now,
                    BlockedUntilUtc: kvp.Value.BlockedUntilUtc > now ? kvp.Value.BlockedUntilUtc : null,
                    LastSeenUtc: kvp.Value.LastSeenUtc));
            }
        }

        return rows
            .OrderByDescending(x => x.ActiveConnections)
            .ThenBy(x => x.Peer, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ConnectionHistoryEntry> GetConnectionHistory(int limit = 500)
    {
        var max = Math.Clamp(limit, 1, 5000);
        lock (_historyGate)
        {
            return _history
                .OrderByDescending(x => x.TimestampUtc)
                .Take(max)
                .ToList();
        }
    }

    public bool BanPeer(string peer, string? reason, string component)
    {
        var normalized = NormalizePeer(peer);
        if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "unknown", StringComparison.OrdinalIgnoreCase))
            return false;

        lock (_banGate)
        {
            _bannedPeers[normalized] = new BannedPeerEntry(
                Peer: normalized,
                Reason: string.IsNullOrWhiteSpace(reason) ? "Banned by administrator." : reason.Trim(),
                BannedUtc: DateTimeOffset.UtcNow);
        }

        AppendHistory(normalized, "peer_ban", true, reason);
        structuredLogs.Write("WARN", "peer_banned", "Peer banned by administrator", component, null, null, $"{{\"peer\":\"{normalized}\"}}");
        return true;
    }

    public bool UnbanPeer(string peer, string component)
    {
        var normalized = NormalizePeer(peer);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        bool removed;
        lock (_banGate)
        {
            removed = _bannedPeers.Remove(normalized);
        }

        if (removed)
        {
            AppendHistory(normalized, "peer_unban", true, null);
            structuredLogs.Write("INFO", "peer_unbanned", "Peer unbanned by administrator", component, null, null, $"{{\"peer\":\"{normalized}\"}}");
        }

        return removed;
    }

    public IReadOnlyList<BannedPeerEntry> GetBannedPeers()
    {
        lock (_banGate)
        {
            return _bannedPeers.Values
                .OrderByDescending(x => x.BannedUtc)
                .ToList();
        }
    }

    public bool IsPeerBanned(string peer)
    {
        var normalized = NormalizePeer(peer);
        lock (_banGate)
        {
            return _bannedPeers.ContainsKey(normalized);
        }
    }

    private void RegisterViolation(PeerState state, DateTime now, string peer, string component, string eventType)
    {
        state.ViolationCount++;
        state.LastSeenUtc = now;
        var threshold = Math.Max(1, options.Value.DosViolationThreshold);
        if (state.ViolationCount >= threshold)
        {
            state.BlockedUntilUtc = now.AddSeconds(Math.Max(1, options.Value.DosBlockSeconds));
            state.ViolationCount = 0;
            structuredLogs.Write("WARN", "dos_detected", "Potential DoS pattern detected; temporary peer block applied", component, null, null, $"{{\"peer\":\"{peer}\",\"blocked_until\":\"{state.BlockedUntilUtc:O}\"}}");
        }
        else
        {
            structuredLogs.Write("WARN", eventType, "Rate limit exceeded", component, null, null, $"{{\"peer\":\"{peer}\",\"violation_count\":{state.ViolationCount}}}");
        }
    }

    private void AppendHistory(string peer, string action, bool allowed, string? detail)
    {
        lock (_historyGate)
        {
            _history.Enqueue(new ConnectionHistoryEntry(
                TimestampUtc: DateTimeOffset.UtcNow,
                Peer: peer,
                Action: action,
                Allowed: allowed,
                Detail: detail));
            while (_history.Count > MaxHistoryRows)
                _history.Dequeue();
        }
    }

    private static void Prune(Queue<DateTime> queue, DateTime minTimestampUtc)
    {
        while (queue.Count > 0 && queue.Peek() < minTimestampUtc)
            queue.Dequeue();
    }

    public static string NormalizePeer(string? peer)
    {
        if (string.IsNullOrWhiteSpace(peer))
            return "unknown";

        if (peer.StartsWith("ipv4:", StringComparison.OrdinalIgnoreCase))
        {
            var value = peer[5..];
            var sep = value.LastIndexOf(':');
            return sep > 0 ? value[..sep] : value;
        }

        if (peer.StartsWith("ipv6:", StringComparison.OrdinalIgnoreCase))
        {
            var value = peer[5..];
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                var end = value.IndexOf(']');
                if (end > 1) return value[1..end];
            }
            var sep = value.LastIndexOf(':');
            return sep > 0 ? value[..sep] : value;
        }

        return peer.Trim();
    }

    private sealed class PeerState
    {
        public object Gate { get; } = new();
        public Queue<DateTime> ConnectionAttemptsUtc { get; } = new();
        public Queue<DateTime> MessageTimestampsUtc { get; } = new();
        public int ActiveConnections { get; set; }
        public int ViolationCount { get; set; }
        public DateTime BlockedUntilUtc { get; set; }
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    }
}

public readonly record struct ConnectionDecision(bool Allowed, string Peer, string? DeniedReason)
{
    public static ConnectionDecision CreateAllowed(string peer) => new(true, peer, null);
    public static ConnectionDecision CreateDenied(string peer, string reason) => new(false, peer, reason);
}

public sealed record ConnectedPeerSnapshot(
    string Peer,
    int ActiveConnections,
    bool IsBlocked,
    DateTime? BlockedUntilUtc,
    DateTime LastSeenUtc);

public sealed record ConnectionHistoryEntry(
    DateTimeOffset TimestampUtc,
    string Peer,
    string Action,
    bool Allowed,
    string? Detail);

public sealed record BannedPeerEntry(
    string Peer,
    string Reason,
    DateTimeOffset BannedUtc);
