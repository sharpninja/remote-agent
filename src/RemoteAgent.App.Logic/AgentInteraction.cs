namespace RemoteAgent.App.Logic;

/// <summary>Transport-agnostic sink for agent input. Used by server and desktop to keep interaction behavior identical.</summary>
public interface IAgentInteractionSession
{
    /// <summary>True when the target can accept input.</summary>
    bool CanAcceptInput { get; }

    /// <summary>Sends one input line/message to the target agent.</summary>
    Task SendInputAsync(string input, CancellationToken cancellationToken = default);
}

/// <summary>Canonical wire text for synthetic system messages that should be interpreted by the agent.</summary>
public static class AgentInteractionProtocol
{
    public static string BuildRequestContextMessage(string requestContext)
    {
        return $"[REQUEST_CONTEXT] {requestContext?.Trim() ?? string.Empty}";
    }

    public static string BuildSeedContextMessage(string contextType, string content)
    {
        var normalizedType = string.IsNullOrWhiteSpace(contextType) ? "context" : contextType.Trim();
        return $"[SEED_CONTEXT:{normalizedType}] {content ?? string.Empty}";
    }

    public static string BuildMcpUpdateMessage(IEnumerable<string> enabledServerIds, IEnumerable<string> disabledServerIds)
    {
        var enabled = enabledServerIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        var disabled = disabledServerIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];

        var parts = new List<string>();
        if (enabled.Count > 0) parts.Add($"enabled={string.Join(',', enabled)}");
        if (disabled.Count > 0) parts.Add($"disabled={string.Join(',', disabled)}");
        return $"[MCP_UPDATE] {string.Join(' ', parts)}";
    }
}

/// <summary>Shared dispatcher that applies interaction protocol messages to a concrete session.</summary>
public static class AgentInteractionDispatcher
{
    public static async Task<bool> TryIssueRequestContextAsync(IAgentInteractionSession? session, string? requestContext, CancellationToken cancellationToken = default)
    {
        if (session == null || !session.CanAcceptInput) return false;
        var normalized = requestContext?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return false;
        await session.SendInputAsync(AgentInteractionProtocol.BuildRequestContextMessage(normalized), cancellationToken);
        return true;
    }

    public static async Task<bool> TryIssueSeedContextAsync(IAgentInteractionSession? session, string? contextType, string? content, CancellationToken cancellationToken = default)
    {
        if (session == null || !session.CanAcceptInput) return false;
        if (string.IsNullOrWhiteSpace(content)) return false;
        await session.SendInputAsync(AgentInteractionProtocol.BuildSeedContextMessage(contextType ?? "context", content), cancellationToken);
        return true;
    }

    public static async Task<bool> TryNotifyMcpUpdateAsync(IAgentInteractionSession? session, IEnumerable<string> enabledServerIds, IEnumerable<string> disabledServerIds, CancellationToken cancellationToken = default)
    {
        if (session == null || !session.CanAcceptInput) return false;
        var message = AgentInteractionProtocol.BuildMcpUpdateMessage(enabledServerIds, disabledServerIds);
        if (string.Equals(message, "[MCP_UPDATE]", StringComparison.Ordinal))
            return false;
        await session.SendInputAsync(message, cancellationToken);
        return true;
    }
}
