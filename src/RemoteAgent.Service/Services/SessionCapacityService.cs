using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Agents;

namespace RemoteAgent.Service.Services;

/// <summary>Tracks active sessions and evaluates session-capacity limits.</summary>
public sealed class SessionCapacityService(IOptions<AgentOptions> options)
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IAgentSession>> _activeSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AbandonedSessionSnapshot> _abandonedSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public bool TryRegisterSession(string agentId, string sessionId, IAgentSession session, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(sessionId))
            return true;

        lock (_gate)
        {
            var serverLimit = Math.Max(1, options.Value.MaxConcurrentSessions);
            var totalActive = _activeSessions.Values.Sum(x => x.Count);
            if (totalActive >= serverLimit)
            {
                reason = $"Server session limit reached ({serverLimit}).";
                return false;
            }

            var agentLimit = ResolveAgentLimit(agentId, serverLimit);
            var bySession = _activeSessions.GetOrAdd(agentId, _ => new ConcurrentDictionary<string, IAgentSession>(StringComparer.OrdinalIgnoreCase));
            if (bySession.Count >= agentLimit)
            {
                reason = $"Agent '{agentId}' session limit reached ({agentLimit}).";
                return false;
            }

            bySession[sessionId] = session;
            _abandonedSessions.TryRemove(sessionId, out _);
            return true;
        }
    }

    public void UnregisterSession(string agentId, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(sessionId))
            return;

        lock (_gate)
        {
            if (!_activeSessions.TryGetValue(agentId, out var bySession))
                return;

            bySession.TryRemove(sessionId, out _);
            if (bySession.IsEmpty)
                _activeSessions.TryRemove(agentId, out _);
        }
    }

    public SessionCapacityStatus GetStatus(string? agentId)
    {
        var normalizedAgentId = string.IsNullOrWhiteSpace(agentId) ? "" : agentId.Trim();
        lock (_gate)
        {
            var serverLimit = Math.Max(1, options.Value.MaxConcurrentSessions);
            var totalActive = _activeSessions.Values.Sum(x => x.Count);
            var remainingServer = Math.Max(0, serverLimit - totalActive);
            var canCreate = remainingServer > 0;
            var reason = canCreate ? "" : $"Server session limit reached ({serverLimit}).";
            int? agentLimit = null;
            int agentActive = 0;
            int? remainingAgent = null;

            if (!string.IsNullOrWhiteSpace(normalizedAgentId))
            {
                agentLimit = ResolveAgentLimit(normalizedAgentId, serverLimit);
                if (_activeSessions.TryGetValue(normalizedAgentId, out var bySession))
                    agentActive = bySession.Count;
                remainingAgent = Math.Max(0, agentLimit.Value - agentActive);
                if (remainingAgent <= 0)
                {
                    canCreate = false;
                    reason = $"Agent '{normalizedAgentId}' session limit reached ({agentLimit}).";
                }
            }

            return new SessionCapacityStatus(
                canCreate,
                reason,
                serverLimit,
                totalActive,
                remainingServer,
                normalizedAgentId,
                agentLimit,
                agentActive,
                remainingAgent);
        }
    }

    public IReadOnlyList<KeyValuePair<string, IAgentSession>> GetActiveSessionsForAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            return [];

        if (!_activeSessions.TryGetValue(agentId, out var bySession) || bySession.IsEmpty)
            return [];

        return bySession.ToArray();
    }

    public IReadOnlyList<ActiveSessionSnapshot> ListOpenSessions()
    {
        lock (_gate)
        {
            var rows = new List<ActiveSessionSnapshot>();
            foreach (var byAgent in _activeSessions)
            {
                foreach (var bySession in byAgent.Value)
                {
                    rows.Add(new ActiveSessionSnapshot(
                        SessionId: bySession.Key,
                        AgentId: byAgent.Key,
                        CanAcceptInput: !bySession.Value.HasExited));
                }
            }

            return rows
                .OrderBy(x => x.AgentId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SessionId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool TryTerminateSession(string sessionId, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            reason = "Session id is required.";
            return false;
        }

        lock (_gate)
        {
            foreach (var byAgent in _activeSessions)
            {
                if (!byAgent.Value.TryGetValue(sessionId, out var session))
                    continue;

                try
                {
                    session.Stop();
                    session.Dispose();
                }
                catch
                {
                    // best effort termination path
                }

                byAgent.Value.TryRemove(sessionId, out _);
                if (byAgent.Value.IsEmpty)
                    _activeSessions.TryRemove(byAgent.Key, out _);
                _abandonedSessions.TryRemove(sessionId, out _);

                return true;
            }
        }

        reason = "Session not found.";
        return false;
    }

    public void MarkSessionAbandoned(string sessionId, string agentId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        var snapshot = new AbandonedSessionSnapshot(
            SessionId: sessionId.Trim(),
            AgentId: string.IsNullOrWhiteSpace(agentId) ? "unknown" : agentId.Trim(),
            Reason: string.IsNullOrWhiteSpace(reason) ? "Connection closed unexpectedly." : reason.Trim(),
            AbandonedUtc: DateTimeOffset.UtcNow);
        _abandonedSessions[snapshot.SessionId] = snapshot;
    }

    public IReadOnlyList<AbandonedSessionSnapshot> ListAbandonedSessions()
    {
        return _abandonedSessions.Values
            .OrderByDescending(x => x.AbandonedUtc)
            .ThenBy(x => x.SessionId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int ResolveAgentLimit(string agentId, int serverLimit)
    {
        if (options.Value.AgentConcurrentSessionLimits.TryGetValue(agentId, out var configured))
            return Math.Max(1, Math.Min(configured, serverLimit));
        return serverLimit;
    }
}

public sealed record SessionCapacityStatus(
    bool CanCreateSession,
    string Reason,
    int MaxConcurrentSessions,
    int ActiveSessionCount,
    int RemainingServerCapacity,
    string AgentId,
    int? AgentMaxConcurrentSessions,
    int AgentActiveSessionCount,
    int? RemainingAgentCapacity);

public sealed record ActiveSessionSnapshot(
    string SessionId,
    string AgentId,
    bool CanAcceptInput);

public sealed record AbandonedSessionSnapshot(
    string SessionId,
    string AgentId,
    string Reason,
    DateTimeOffset AbandonedUtc);
