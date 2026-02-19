using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Services;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Shared test infrastructure: stub implementations and workspace factories used across all Desktop handler unit tests. TR-18.3, TR-18.4.</summary>
internal static class SharedWorkspaceFactory
{
    public static ServerWorkspaceViewModel CreateWorkspace(
        IServerCapacityClient? client = null,
        IDesktopSessionViewModelFactory? factory = null)
    {
        var context = new CurrentServerContext
        {
            Registration = new ServerRegistration
            {
                ServerId = "srv-test",
                DisplayName = "Test",
                Host = "127.0.0.1",
                Port = 5243,
                ApiKey = ""
            }
        };
        return new ServerWorkspaceViewModel(
            context,
            client ?? new StubCapacityClient(),
            new StubLogStore(),
            factory ?? new StubSessionFactory(),
            new NullDispatcher());
    }

    public static SecurityViewModel CreateSecurityViewModel(IServerCapacityClient? client = null)
        => new SecurityViewModel(new NullDispatcher(), StubConnectionContext.Default);

    public static AuthUsersViewModel CreateAuthUsersViewModel(IServerCapacityClient? client = null)
        => new AuthUsersViewModel(new NullDispatcher(), StubConnectionContext.Default);

    public static PluginsViewModel CreatePluginsViewModel(IServerCapacityClient? client = null)
        => new PluginsViewModel(new NullDispatcher(), StubConnectionContext.Default);

    public static McpRegistryDesktopViewModel CreateMcpRegistryViewModel(IServerCapacityClient? client = null)
        => new McpRegistryDesktopViewModel(new NullDispatcher(), StubConnectionContext.Default);

    public static PromptTemplatesViewModel CreatePromptTemplatesViewModel(IServerCapacityClient? client = null)
        => new PromptTemplatesViewModel(new NullDispatcher(), StubConnectionContext.Default);

    public static StructuredLogsViewModel CreateStructuredLogsViewModel(IServerCapacityClient? client = null)
        => new StructuredLogsViewModel(new NullDispatcher(), StubConnectionContext.Default, new StubLogStore());

    public static AppLogViewModel CreateAppLog(string logsFolder = "/tmp/test-logs") =>
        new AppLogViewModel(new NullDispatcher(), new NullFileSaveDialogService(), logsFolder);
}

internal sealed class StubConnectionContext : IServerConnectionContext
{
    public static readonly StubConnectionContext Default = new();
    public string Host => "127.0.0.1";
    public string Port => "5243";
    public string ApiKey => "";
    public string SelectedAgentId => "process";
    public string SelectedConnectionMode => "server";
    public string PerRequestContext => "";
    public string ServerId => "srv-test";
    public string ServerDisplayName => "Test";
}

internal sealed class NullDispatcher : IRequestDispatcher
{
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => Task.FromResult(default(TResponse)!);
}

internal sealed class NullFileSaveDialogService : IFileSaveDialogService
{
    public Task<string?> GetSaveFilePathAsync(string suggestedName, string extension, string filterDescription)
        => Task.FromResult<string?>(null);
}

internal sealed class StubCapacityClient : IServerCapacityClient
{
    public bool BanPeerResult { get; set; } = true;
    public bool UnbanPeerResult { get; set; } = true;
    public bool DeleteAuthUserResult { get; set; } = true;
    public bool DeleteMcpServerResult { get; set; } = true;
    public bool DeletePromptTemplateResult { get; set; } = true;
    public bool SetAgentMcpServersResult { get; set; } = true;
    public bool TerminateSessionResult { get; set; } = true;
    public string? LastTerminatedSessionId { get; private set; }
    public bool SeedSessionContextResult { get; set; } = true;
    public RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot? CapacitySnapshot { get; set; } = null;
    public AuthUserSnapshot? UpsertAuthUserResult { get; set; } = new AuthUserSnapshot("user1", "User One", "viewer", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    public McpServerDefinition? UpsertMcpServerResult { get; set; } = new McpServerDefinition { ServerId = "mcp1" };
    public PluginConfigurationSnapshot? GetPluginsResult { get; set; } = null;
    public PluginConfigurationSnapshot? UpdatePluginsResult { get; set; } = null;
    public PromptTemplateDefinition? UpsertPromptTemplateResult { get; set; } = new PromptTemplateDefinition { TemplateId = "tpl1" };

    public Task<RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot?> GetCapacityAsync(string host, int port, string? agentId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(CapacitySnapshot);
    public Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OpenServerSessionSnapshot>>([]);
    public Task<bool> TerminateSessionAsync(string host, int port, string sessionId, string? apiKey, CancellationToken cancellationToken = default)
    {
        LastTerminatedSessionId = sessionId;
        return Task.FromResult(TerminateSessionResult);
    }
    public Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AbandonedServerSessionSnapshot>>([]);
    public Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConnectedPeerSnapshot>>([]);
    public Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(string host, int port, int limit, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConnectionHistorySnapshot>>([]);
    public Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BannedPeerSnapshot>>([]);
    public Task<bool> BanPeerAsync(string host, int port, string peer, string? reason, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(BanPeerResult);
    public Task<bool> UnbanPeerAsync(string host, int port, string peer, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(UnbanPeerResult);
    public Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuthUserSnapshot>>([]);
    public Task<IReadOnlyList<string>> GetPermissionRolesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
    public Task<AuthUserSnapshot?> UpsertAuthUserAsync(string host, int port, AuthUserSnapshot user, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(UpsertAuthUserResult);
    public Task<bool> DeleteAuthUserAsync(string host, int port, string userId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(DeleteAuthUserResult);
    public Task<PluginConfigurationSnapshot?> GetPluginsAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(GetPluginsResult);
    public Task<PluginConfigurationSnapshot?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(UpdatePluginsResult);
    public Task<IReadOnlyList<McpServerDefinition>> ListMcpServersAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<McpServerDefinition>>([]);
    public Task<McpServerDefinition?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(UpsertMcpServerResult);
    public Task<bool> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(DeleteMcpServerResult);
    public Task<Proto.GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<Proto.GetAgentMcpServersResponse?>(null);
    public Task<bool> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(SetAgentMcpServersResult);
    public Task<IReadOnlyList<PromptTemplateDefinition>> ListPromptTemplatesAsync(string host, int port, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PromptTemplateDefinition>>([]);
    public Task<PromptTemplateDefinition?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(UpsertPromptTemplateResult);
    public Task<bool> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(DeletePromptTemplateResult);
    public Task<bool> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source, string? correlationId, string? apiKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(SeedSessionContextResult);
}

internal sealed class StubLogStore : IDesktopStructuredLogStore
{
    public long GetMaxEventId(string host, int port, string? serverId = null) => 0;
    public IReadOnlyList<DesktopStructuredLogRecord> Query(DesktopStructuredLogFilter? filter = null, int limit = 1000) => [];
    public void UpsertBatch(IEnumerable<DesktopStructuredLogRecord> logs) { }
}

internal sealed class StubSessionFactory : IDesktopSessionViewModelFactory
{
    public DesktopSessionViewModel Create(string title, string connectionMode, string agentId) =>
        new(new FakeAgentSession())
        {
            Title = title,
            ConnectionMode = connectionMode,
            AgentId = agentId,
            SessionId = Guid.NewGuid().ToString("N")[..12]
        };
}

internal sealed class StubAppLogStore : IAppLogStore
{
    private readonly List<AppLogEntry> _entries = [];
    public void Add(AppLogEntry entry) => _entries.Add(entry);
    public IReadOnlyList<AppLogEntry> GetAll() => _entries.AsReadOnly();
    public void Clear() => _entries.Clear();
}

internal sealed class FakeAgentSession : IAgentSessionClient
{
    public bool IsConnected { get; private set; }
    public bool CanAcceptInput => IsConnected;
    public string? CurrentSessionId => null;
    public ServerInfoResponse? ServerInfo => null;
    public string? PerRequestContext { get; set; }
    public bool ThrowOnConnect { get; set; }
    public bool ThrowOnSend { get; set; }
    public event Action? ConnectionStateChanged;
    public event Action<ChatMessage>? MessageReceived
    { add { } remove { } }

    public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
    {
        if (ThrowOnConnect) throw new InvalidOperationException("Connect failed (test)");
        IsConnected = true;
        ConnectionStateChanged?.Invoke();
        return Task.CompletedTask;
    }
    public void Disconnect() { IsConnected = false; ConnectionStateChanged?.Invoke(); }
    public Task SendInputAsync(string input, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendTextAsync(string text, CancellationToken ct = default)
    {
        if (ThrowOnSend) throw new InvalidOperationException("Send failed (test)");
        return Task.CompletedTask;
    }
    public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default) => Task.CompletedTask;
    public Task StopSessionAsync(CancellationToken ct = default) { Disconnect(); return Task.CompletedTask; }
}

internal sealed class StubStructuredLogClient : IStructuredLogClient
{
    public StructuredLogsSnapshotResponse? SnapshotResult { get; set; }
    public bool ThrowOnGet { get; set; }

    public Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(
        string host, int port, long fromOffset = 0, int limit = 5000,
        string? apiKey = null, CancellationToken cancellationToken = default, bool throwOnError = false)
    {
        if (ThrowOnGet) throw new InvalidOperationException("Log fetch failed (test)");
        return Task.FromResult(SnapshotResult);
    }

    public Task MonitorStructuredLogsAsync(string host, int port, long fromOffset,
        Func<StructuredLogEntry, Task> onEntry, string? apiKey = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class NullClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
}

internal sealed class CapturingClipboardService : IClipboardService
{
    public string? LastText { get; private set; }
    public Task SetTextAsync(string text) { LastText = text; return Task.CompletedTask; }
}

internal sealed class NullFolderOpenerService : IFolderOpenerService
{
    public void OpenFolder(string path) { }
}

internal sealed class CapturingFolderOpenerService : IFolderOpenerService
{
    public string? LastPath { get; private set; }
    public void OpenFolder(string path) => LastPath = path;
}
