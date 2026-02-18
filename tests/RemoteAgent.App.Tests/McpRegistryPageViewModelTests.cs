using FluentAssertions;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.Handlers;
using RemoteAgent.App.Logic.Requests;
using RemoteAgent.App.Logic.ViewModels;
using RemoteAgent.Proto;
using LogicRequests = RemoteAgent.App.Logic.Requests;
namespace RemoteAgent.App.Tests;

public sealed class McpRegistryPageViewModelTests
{
    private sealed class InMemoryPreferences : IAppPreferences
    {
        private readonly Dictionary<string, string> _store = new(StringComparer.Ordinal);

        public string Get(string key, string defaultValue) =>
            _store.TryGetValue(key, out var v) ? v : defaultValue;

        public void Set(string key, string value) => _store[key] = value;
    }

    private sealed class StubApiClient : IServerApiClient
    {
        public List<McpServerDefinition> McpServers { get; } = [];
        public bool ShouldReturnNull { get; set; }
        public bool SaveShouldFail { get; set; }

        public Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        {
            if (ShouldReturnNull) return Task.FromResult<ListMcpServersResponse?>(null);
            var response = new ListMcpServersResponse();
            response.Servers.AddRange(McpServers);
            return Task.FromResult<ListMcpServersResponse?>(response);
        }

        public Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default)
        {
            if (ShouldReturnNull) return Task.FromResult<UpsertMcpServerResponse?>(null);
            if (SaveShouldFail) return Task.FromResult<UpsertMcpServerResponse?>(new UpsertMcpServerResponse { Success = false, Message = "Validation failed" });
            var existing = McpServers.FindIndex(s => s.ServerId == server.ServerId);
            if (existing >= 0) McpServers[existing] = server;
            else McpServers.Add(server);
            return Task.FromResult<UpsertMcpServerResponse?>(new UpsertMcpServerResponse { Success = true, Server = server, Message = "OK" });
        }

        public Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default)
        {
            if (ShouldReturnNull) return Task.FromResult<DeleteMcpServerResponse?>(null);
            McpServers.RemoveAll(s => s.ServerId == serverId);
            return Task.FromResult<DeleteMcpServerResponse?>(new DeleteMcpServerResponse { Success = true, Message = "Deleted." });
        }

        public Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<ServerInfoResponse?>(null);
        public Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<ListPromptTemplatesResponse?>(null);
        public Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<GetPluginsResponse?>(null);
        public Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<UpdatePluginsResponse?>(null);
        public Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<UpsertPromptTemplateResponse?>(null);
        public Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<DeletePromptTemplateResponse?>(null);
        public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<GetAgentMcpServersResponse?>(null);
        public Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SetAgentMcpServersResponse?>(null);
        public Task<SeedSessionContextResponse?> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source = null, string? correlationId = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SeedSessionContextResponse?>(null);
        public Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(string host, int port, string? agentId = null, string? apiKey = null, CancellationToken ct = default) => Task.FromResult<SessionCapacitySnapshot?>(null);
    }

    private sealed class StubDeleteConfirmation(bool result) : ISessionTerminationConfirmation
    {
        public Task<bool> ConfirmAsync(string sessionLabel) => Task.FromResult(result);
    }

    private sealed class NullRequestDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            => Task.FromResult(default(TResponse)!);
    }

    private sealed class TestRequestDispatcher : IRequestDispatcher
    {
        private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();

        public TestRequestDispatcher Register<TReq, TResp>(IRequestHandler<TReq, TResp> handler)
            where TReq : IRequest<TResp>
        {
            _handlers[typeof(TReq)] = async (req, ct) => (await handler.HandleAsync((TReq)req, ct))!;
            return this;
        }

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            if (_handlers.TryGetValue(request.GetType(), out var h))
                return (TResponse)await h(request, ct);
            return default!;
        }
    }

    private static McpRegistryPageViewModel CreateVm(
        StubApiClient? apiClient = null,
        InMemoryPreferences? preferences = null,
        ISessionTerminationConfirmation? deleteConfirmation = null,
        IRequestDispatcher? dispatcher = null)
    {
        var api = apiClient ?? new StubApiClient();
        var del = deleteConfirmation ?? new StubDeleteConfirmation(true);
        var prefs = preferences ?? new InMemoryPreferences();
        var disp = dispatcher ?? new TestRequestDispatcher()
            .Register<LoadMcpServersRequest, CommandResult>(new LoadMcpServersHandler(api))
            .Register<SaveMcpServerRequest, CommandResult>(new SaveMcpServerHandler(api))
            .Register<LogicRequests.DeleteMcpServerRequest, CommandResult>(new DeleteMcpServerHandler(api, del));
        return new McpRegistryPageViewModel(api, prefs, del, disp);
    }

    [Fact]
    public void Constructor_LoadsHostAndPortFromPreferences()
    {
        var prefs = new InMemoryPreferences();
        prefs.Set("ServerHost", "192.168.1.100");
        prefs.Set("ServerPort", "9999");

        var vm = CreateVm(preferences: prefs);

        vm.Host.Should().Be("192.168.1.100");
        vm.Port.Should().Be("9999");
    }

    [Fact]
    public void Constructor_DefaultsPortTo5243_WhenNotInPreferences()
    {
        var vm = CreateVm();
        vm.Port.Should().Be("5243");
    }

    [Fact]
    public async Task RefreshAsync_PopulatesServers_WhenApiReturnsData()
    {
        var api = new StubApiClient();
        api.McpServers.Add(new McpServerDefinition { ServerId = "s1", DisplayName = "Alpha" });
        api.McpServers.Add(new McpServerDefinition { ServerId = "s2", DisplayName = "Beta" });

        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";

        await new LoadMcpServersHandler(api).HandleAsync(new LoadMcpServersRequest(Guid.NewGuid(), vm));

        vm.Servers.Should().HaveCount(2);
        vm.StatusText.Should().Contain("2 MCP server(s)");
    }

    [Fact]
    public async Task RefreshAsync_SetsErrorStatus_WhenApiReturnsNull()
    {
        var api = new StubApiClient { ShouldReturnNull = true };
        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";

        await new LoadMcpServersHandler(api).HandleAsync(new LoadMcpServersRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Failed to load");
    }

    [Fact]
    public async Task RefreshAsync_SetsHostRequiredStatus_WhenHostIsEmpty()
    {
        var api = new StubApiClient();
        var vm = CreateVm(apiClient: api);
        vm.Host = "";

        await new LoadMcpServersHandler(api).HandleAsync(new LoadMcpServersRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Be("Host is required.");
    }

    [Fact]
    public async Task RefreshAsync_SetsPortRequiredStatus_WhenPortIsInvalid()
    {
        var api = new StubApiClient();
        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";
        vm.Port = "abc";

        await new LoadMcpServersHandler(api).HandleAsync(new LoadMcpServersRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Valid port required");
    }

    [Fact]
    public void SelectServer_PopulatesAllFields()
    {
        var vm = CreateVm();
        var server = new McpServerDefinition
        {
            ServerId = "test-id",
            DisplayName = "Test Server",
            Transport = "stdio",
            Endpoint = "http://example.com",
            Command = "/bin/test",
            AuthType = "bearer",
            AuthConfigJson = "{\"token\":\"abc\"}",
            MetadataJson = "{\"key\":\"val\"}",
            Enabled = false
        };
        server.Arguments.Add("--flag");
        server.Arguments.Add("value");

        vm.SelectServer(server);

        vm.ServerId.Should().Be("test-id");
        vm.DisplayName.Should().Be("Test Server");
        vm.Transport.Should().Be("stdio");
        vm.Endpoint.Should().Be("http://example.com");
        vm.Command.Should().Be("/bin/test");
        vm.Arguments.Should().Be("--flag value");
        vm.AuthType.Should().Be("bearer");
        vm.AuthConfigJson.Should().Be("{\"token\":\"abc\"}");
        vm.MetadataJson.Should().Be("{\"key\":\"val\"}");
        vm.Enabled.Should().BeFalse();
        vm.StatusText.Should().Contain("Editing 'test-id'");
    }

    [Fact]
    public async Task SaveAsync_SetsFailedStatus_WhenApiReturnsNull()
    {
        var api = new StubApiClient { ShouldReturnNull = true };
        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";
        vm.ServerId = "s1";

        await new SaveMcpServerHandler(api).HandleAsync(new SaveMcpServerRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Failed to save");
    }

    [Fact]
    public async Task SaveAsync_SetsValidationMessage_WhenApiReturnsFailure()
    {
        var api = new StubApiClient { SaveShouldFail = true };
        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";
        vm.ServerId = "s1";

        await new SaveMcpServerHandler(api).HandleAsync(new SaveMcpServerRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Be("Validation failed");
    }

    [Fact]
    public async Task SaveAsync_RefreshesAndUpdatesStatus_OnSuccess()
    {
        var api = new StubApiClient();
        var vm = CreateVm(apiClient: api);
        vm.Host = "localhost";
        vm.ServerId = "s1";
        vm.DisplayName = "Server One";

        await new SaveMcpServerHandler(api).HandleAsync(new SaveMcpServerRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Saved 's1'");
    }

    [Fact]
    public async Task DeleteAsync_RemovesServer_WhenConfirmed()
    {
        var api = new StubApiClient();
        var del = new StubDeleteConfirmation(true);
        api.McpServers.Add(new McpServerDefinition { ServerId = "s1", DisplayName = "Alpha" });
        var vm = CreateVm(apiClient: api, deleteConfirmation: del);
        vm.Host = "localhost";
        vm.ServerId = "s1";

        await new DeleteMcpServerHandler(api, del).HandleAsync(new LogicRequests.DeleteMcpServerRequest(Guid.NewGuid(), vm));

        api.McpServers.Should().BeEmpty();
        vm.ServerId.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenCancelled()
    {
        var api = new StubApiClient();
        var del = new StubDeleteConfirmation(false);
        api.McpServers.Add(new McpServerDefinition { ServerId = "s1", DisplayName = "Alpha" });
        var vm = CreateVm(apiClient: api, deleteConfirmation: del);
        vm.Host = "localhost";
        vm.ServerId = "s1";

        await new DeleteMcpServerHandler(api, del).HandleAsync(new LogicRequests.DeleteMcpServerRequest(Guid.NewGuid(), vm));

        api.McpServers.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_SetsErrorStatus_WhenServerIdIsEmpty()
    {
        var api = new StubApiClient();
        var del = new StubDeleteConfirmation(true);
        var vm = CreateVm(apiClient: api, deleteConfirmation: del);
        vm.Host = "localhost";
        vm.ServerId = "";

        await new DeleteMcpServerHandler(api, del).HandleAsync(new LogicRequests.DeleteMcpServerRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Select a server");
    }

    [Fact]
    public async Task DeleteAsync_SetsErrorStatus_WhenApiReturnsNull()
    {
        var api = new StubApiClient { ShouldReturnNull = true };
        var del = new StubDeleteConfirmation(true);
        var vm = CreateVm(apiClient: api, deleteConfirmation: del);
        vm.Host = "localhost";
        vm.ServerId = "s1";

        await new DeleteMcpServerHandler(api, del).HandleAsync(new LogicRequests.DeleteMcpServerRequest(Guid.NewGuid(), vm));

        vm.StatusText.Should().Contain("Failed to delete");
    }

    [Fact]
    public void ClearCommand_ResetsAllFields()
    {
        var vm = CreateVm();
        vm.ServerId = "some-id";
        vm.DisplayName = "Some name";
        vm.Transport = "stdio";

        vm.ClearCommand.Execute(null);

        vm.ServerId.Should().BeEmpty();
        vm.DisplayName.Should().BeEmpty();
        vm.Transport.Should().BeEmpty();
        vm.Enabled.Should().BeTrue();
        vm.StatusText.Should().Be("Editor cleared.");
    }

    [Theory]
    [InlineData("--verbose --debug", new[] { "--verbose", "--debug" })]
    [InlineData("\"hello world\" test", new[] { "hello world", "test" })]
    [InlineData("", new string[0])]
    [InlineData(null, new string[0])]
    public void ParseArguments_HandlesVariousInputs(string? input, string[] expected)
    {
        var result = McpRegistryPageViewModel.ParseArguments(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void PropertyChanged_IsRaisedForEachProperty()
    {
        var vm = CreateVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Host = "newhost";
        vm.Port = "1234";
        vm.StatusText = "Updated";
        vm.ServerId = "id";
        vm.DisplayName = "name";
        vm.Transport = "sse";
        vm.Endpoint = "http://localhost";
        vm.Command = "cmd";
        vm.Arguments = "--arg";
        vm.AuthType = "api_key";
        vm.AuthConfigJson = "{}";
        vm.MetadataJson = "{}";
        vm.Enabled = false;

        changed.Should().Contain(new[]
        {
            "Host", "Port", "StatusText", "ServerId", "DisplayName",
            "Transport", "Endpoint", "Command", "Arguments",
            "AuthType", "AuthConfigJson", "MetadataJson", "Enabled"
        });
    }
}
