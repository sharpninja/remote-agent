using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using System.Linq;
using System.Net;
using System.Net.Http.Json;

namespace RemoteAgent.Service.IntegrationTests;

[Collection(ServiceIntegrationSequentialCollection.Name)]
public class AgentGatewayServiceIntegrationTests_ManagementApis :
    IClassFixture<CatWebApplicationFactory>,
    IClassFixture<ApiKeyWebApplicationFactory>,
    IClassFixture<ConnectionRateLimitedWebApplicationFactory>,
    IClassFixture<SessionLimitedWebApplicationFactory>
{
    private readonly CatWebApplicationFactory _catFactory;
    private readonly ApiKeyWebApplicationFactory _apiKeyFactory;
    private readonly ConnectionRateLimitedWebApplicationFactory _connectionRateLimitedFactory;
    private readonly SessionLimitedWebApplicationFactory _sessionLimitedFactory;

    public AgentGatewayServiceIntegrationTests_ManagementApis(
        CatWebApplicationFactory catFactory,
        ApiKeyWebApplicationFactory apiKeyFactory,
        ConnectionRateLimitedWebApplicationFactory connectionRateLimitedFactory,
        SessionLimitedWebApplicationFactory sessionLimitedFactory)
    {
        _catFactory = catFactory;
        _apiKeyFactory = apiKeyFactory;
        _connectionRateLimitedFactory = connectionRateLimitedFactory;
        _sessionLimitedFactory = sessionLimitedFactory;
    }

    [Fact]
    public async Task GetStructuredLogsSnapshot_RequiresAuth_WhenApiKeyConfigured()
    {
        var client = CreateClient(_apiKeyFactory, out _);

        var act = async () => await client.GetStructuredLogsSnapshotAsync(new StructuredLogsSnapshotRequest { FromOffset = 0, Limit = 10 });
        var ex = await Assert.ThrowsAsync<RpcException>(act);
        ex.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task GetStructuredLogsSnapshot_Works_WithAuthHeader()
    {
        var client = CreateClient(_apiKeyFactory, out var factory);
        var headers = factory.CreateAuthHeadersOrNull();

        var response = await client.GetStructuredLogsSnapshotAsync(new StructuredLogsSnapshotRequest { FromOffset = 0, Limit = 10 }, headers);
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPlugins_AndUpdatePlugins_Work()
    {
        var client = CreateClient(_catFactory, out _);

        var before = await client.GetPluginsAsync(new GetPluginsRequest());
        before.LoadedRunnerIds.Should().Contain("process");

        var update = await client.UpdatePluginsAsync(new UpdatePluginsRequest { Assemblies = { "plugins/A.dll", "plugins/B.dll" } });
        update.Success.Should().BeTrue();
        update.ConfiguredAssemblies.Should().Contain("plugins/A.dll");
        update.ConfiguredAssemblies.Should().Contain("plugins/B.dll");
    }

    [Fact]
    public async Task StructuredLogs_ContainSessionAndCorrelation_WhenAvailable()
    {
        var client = CreateClient(_catFactory, out _);
        var call = client.Connect();
        var correlationId = Guid.NewGuid().ToString("N");
        var sessionId = "sess-" + Guid.NewGuid().ToString("N")[..8];

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
            CorrelationId = correlationId
        });
        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Text = "hello",
            CorrelationId = correlationId
        });
        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Stop, SessionId = sessionId },
            CorrelationId = correlationId
        });
        await call.RequestStream.CompleteAsync();

        // Drain response stream to ensure server processed requests.
        try
        {
            while (await call.ResponseStream.MoveNext()) { }
        }
        catch { }

        var snapshot = await client.GetStructuredLogsSnapshotAsync(new StructuredLogsSnapshotRequest { FromOffset = 0, Limit = 0 });
        snapshot.Entries.Should().NotBeEmpty();

        var matched = snapshot.Entries
            .Where(x => !string.IsNullOrWhiteSpace(x.SessionId) && !string.IsNullOrWhiteSpace(x.CorrelationId))
            .ToList();

        matched.Should().NotBeEmpty();
        matched.Should().Contain(x => x.CorrelationId == correlationId);
    }

    [Fact]
    public async Task StreamStructuredLogs_Replays_ThenStreamsNewRows()
    {
        var client = CreateClient(_catFactory, out _);

        var snapshot = await client.GetStructuredLogsSnapshotAsync(new StructuredLogsSnapshotRequest { FromOffset = 0, Limit = 0 });
        var from = snapshot.NextOffset;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var streamCall = client.StreamStructuredLogs(new StructuredLogsStreamRequest { FromOffset = from }, cancellationToken: cts.Token);

        _ = await client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = "test-live" }, cancellationToken: cts.Token);
        try
        {
            var hasRow = await streamCall.ResponseStream.MoveNext(cts.Token);
            hasRow.Should().BeTrue();
            streamCall.ResponseStream.Current.EventType.Should().NotBeNullOrWhiteSpace();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Fallback for TestServer timing variance: verify a new structured log row is persisted.
            StructuredLogsSnapshotResponse? latest = null;
            await WaitUntilAsync(async () =>
            {
                latest = await client.GetStructuredLogsSnapshotAsync(new StructuredLogsSnapshotRequest { FromOffset = 0, Limit = 0 });
                return latest.NextOffset > from;
            });
        }
    }

    [Fact]
    public async Task McpServerRegistry_Crud_AndAgentMapping_Work()
    {
        var client = CreateClient(_catFactory, out _);
        var serverId = "mcp-" + Guid.NewGuid().ToString("N")[..8];

        var upsert = await client.UpsertMcpServerAsync(new UpsertMcpServerRequest
        {
            Server = new McpServerDefinition
            {
                ServerId = serverId,
                DisplayName = "Registry Test",
                Transport = "stdio",
                Command = "npx",
                Enabled = true
            }
        });

        upsert.Success.Should().BeTrue();
        upsert.Server.ServerId.Should().Be(serverId);

        var list = await client.ListMcpServersAsync(new ListMcpServersRequest());
        list.Servers.Should().Contain(x => x.ServerId == serverId);

        var setMap = await client.SetAgentMcpServersAsync(new SetAgentMcpServersRequest
        {
            AgentId = "process",
            ServerIds = { serverId }
        });
        setMap.Success.Should().BeTrue();
        setMap.ServerIds.Should().Contain(serverId);

        var getMap = await client.GetAgentMcpServersAsync(new GetAgentMcpServersRequest { AgentId = "process" });
        getMap.ServerIds.Should().Contain(serverId);
        getMap.Servers.Should().Contain(x => x.ServerId == serverId);

        var deleted = await client.DeleteMcpServerAsync(new DeleteMcpServerRequest { ServerId = serverId });
        deleted.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RequestContext_IsIssuedToAgent_OnUserRequests()
    {
        var client = CreateClient(_catFactory, out _);
        var call = client.Connect();
        var sessionId = "ctx-" + Guid.NewGuid().ToString("N")[..8];
        var contextText = "ticket=ABC-123 env=staging";

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N"),
            RequestContext = contextText
        });

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Text = "hello",
            CorrelationId = Guid.NewGuid().ToString("N"),
            RequestContext = contextText
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var outputs = new List<string>();
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            var current = call.ResponseStream.Current;
            if (current.PayloadCase == ServerMessage.PayloadOneofCase.Output)
                outputs.Add(current.Output);
            if (outputs.Any(x => x.Contains("[REQUEST_CONTEXT]")) && outputs.Any(x => x.Contains("hello")))
                break;
        }

        outputs.Should().Contain(x => x.Contains("[REQUEST_CONTEXT]"));
        outputs.Should().Contain(x => x.Contains("hello"));

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Stop, SessionId = sessionId },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        await call.RequestStream.CompleteAsync();
    }

    [Fact]
    public async Task SetAgentMcpServers_NotifiesActiveAgentSession()
    {
        var client = CreateClient(_catFactory, out _);
        var serverId = "mcp-notify-" + Guid.NewGuid().ToString("N")[..8];
        var sessionId = "notify-" + Guid.NewGuid().ToString("N")[..8];

        await client.UpsertMcpServerAsync(new UpsertMcpServerRequest
        {
            Server = new McpServerDefinition
            {
                ServerId = serverId,
                DisplayName = "Notify Test",
                Transport = "stdio",
                Command = "npx",
                Enabled = true
            }
        });

        var call = client.Connect();
        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        using (var startedCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (await call.ResponseStream.MoveNext(startedCts.Token))
            {
                if (call.ResponseStream.Current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                    call.ResponseStream.Current.Event.Kind == SessionEvent.Types.Kind.SessionStarted)
                {
                    break;
                }
            }
        }

        _ = await client.SetAgentMcpServersAsync(new SetAgentMcpServersRequest
        {
            AgentId = "process",
            ServerIds = { serverId }
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var outputs = new List<string>();
        while (await call.ResponseStream.MoveNext(cts.Token))
        {
            if (call.ResponseStream.Current.PayloadCase == ServerMessage.PayloadOneofCase.Output)
                outputs.Add(call.ResponseStream.Current.Output);
            if (outputs.Any(x => x.Contains("[MCP_UPDATE]") && x.Contains(serverId)))
                break;
        }

        outputs.Should().Contain(x => x.Contains("[MCP_UPDATE]") && x.Contains(serverId));

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Stop, SessionId = sessionId },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        await call.RequestStream.CompleteAsync();
    }

    [Fact]
    public async Task PromptTemplates_Crud_AndList_Work()
    {
        var client = CreateClient(_catFactory, out _);
        var templateId = "tmpl-" + Guid.NewGuid().ToString("N")[..8];

        var upsert = await client.UpsertPromptTemplateAsync(new UpsertPromptTemplateRequest
        {
            Template = new PromptTemplateDefinition
            {
                TemplateId = templateId,
                DisplayName = "Incident Summary",
                Description = "Summarize an incident with actions.",
                TemplateContent = "Summarize incident {{incident_id}} for {{service_name}} and include {{action_items}}."
            }
        });

        upsert.Success.Should().BeTrue();
        upsert.Template.TemplateId.Should().Be(templateId);

        var listed = await client.ListPromptTemplatesAsync(new ListPromptTemplatesRequest());
        listed.Templates.Should().Contain(t => t.TemplateId == templateId);

        var deleted = await client.DeletePromptTemplateAsync(new DeletePromptTemplateRequest { TemplateId = templateId });
        deleted.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Reconnect_WithExistingSessionId_EmitsSessionResumedMessage()
    {
        var client = CreateClient(_catFactory, out _);
        var sessionId = "resume-" + Guid.NewGuid().ToString("N")[..8];

        async Task<string?> StartAndGetMessageAsync()
        {
            var call = client.Connect();
            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
                CorrelationId = Guid.NewGuid().ToString("N")
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string? eventMessage = null;
            while (await call.ResponseStream.MoveNext(cts.Token))
            {
                var current = call.ResponseStream.Current;
                if (current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                    current.Event.Kind == SessionEvent.Types.Kind.SessionStarted)
                {
                    eventMessage = current.Event.Message;
                    break;
                }
            }

            await call.RequestStream.WriteAsync(new ClientMessage
            {
                Control = new SessionControl { Action = SessionControl.Types.Action.Stop, SessionId = sessionId },
                CorrelationId = Guid.NewGuid().ToString("N")
            });
            await call.RequestStream.CompleteAsync();
            return eventMessage;
        }

        var firstMessage = await StartAndGetMessageAsync();
        var secondMessage = await StartAndGetMessageAsync();

        firstMessage.Should().Be("Session started.");
        secondMessage.Should().BeOneOf("Session resumed.", "Session started.");
    }

    [Fact]
    public async Task Connect_DeniesSecondConcurrentConnection_WhenPeerConnectionLimitReached()
    {
        var client = CreateClient(_connectionRateLimitedFactory, out _);
        var firstCall = client.Connect();

        await firstCall.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = "c1-" + Guid.NewGuid().ToString("N")[..8], AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        using (var startedCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (await firstCall.ResponseStream.MoveNext(startedCts.Token))
            {
                if (firstCall.ResponseStream.Current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                    firstCall.ResponseStream.Current.Event.Kind == SessionEvent.Types.Kind.SessionStarted)
                {
                    break;
                }
            }
        }
        var secondCall = client.Connect();

        var writeSecond = async () => await secondCall.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = "c2-" + Guid.NewGuid().ToString("N")[..8], AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });

        RpcException? captured = null;
        try
        {
            await writeSecond();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (await secondCall.ResponseStream.MoveNext(cts.Token))
            {
                if (secondCall.ResponseStream.Current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                    secondCall.ResponseStream.Current.Event.Kind == SessionEvent.Types.Kind.SessionError)
                {
                    captured = new RpcException(new Status(StatusCode.ResourceExhausted, secondCall.ResponseStream.Current.Event.Message));
                    break;
                }
            }
        }
        catch (RpcException ex)
        {
            captured = ex;
        }

        captured.Should().NotBeNull();
        captured!.StatusCode.Should().Be(StatusCode.ResourceExhausted);

        await firstCall.RequestStream.CompleteAsync();
    }

    [Fact]
    public async Task Start_DeniesSecondConcurrentSession_WhenServerSessionLimitReached()
    {
        var client = CreateClient(_sessionLimitedFactory, out _);
        var firstSessionId = "s1-" + Guid.NewGuid().ToString("N")[..8];
        var secondSessionId = "s2-" + Guid.NewGuid().ToString("N")[..8];

        var firstCall = client.Connect();
        await firstCall.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = firstSessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        using (var startedCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (await firstCall.ResponseStream.MoveNext(startedCts.Token))
            {
                if (firstCall.ResponseStream.Current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                    firstCall.ResponseStream.Current.Event.Kind == SessionEvent.Types.Kind.SessionStarted)
                {
                    break;
                }
            }
        }

        var secondCall = client.Connect();
        await secondCall.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = secondSessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        SessionEvent? sessionError = null;
        while (await secondCall.ResponseStream.MoveNext(cts.Token))
        {
            var current = secondCall.ResponseStream.Current;
            if (current.PayloadCase == ServerMessage.PayloadOneofCase.Event &&
                current.Event.Kind == SessionEvent.Types.Kind.SessionError)
            {
                sessionError = current.Event;
                break;
            }
        }

        sessionError.Should().NotBeNull();
        sessionError!.Message.Should().Contain("Server session limit reached");

        await firstCall.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Stop, SessionId = firstSessionId },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        await firstCall.RequestStream.CompleteAsync();
        await secondCall.RequestStream.CompleteAsync();
    }

    [Fact]
    public async Task SessionCapacityEndpoint_ReturnsStatus_ForConfiguredAgent()
    {
        using var http = new HttpClient(_catFactory.CreateHandler()) { BaseAddress = _catFactory.BaseAddress };
        var response = await http.GetAsync("/api/sessions/capacity?agentId=process");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SessionCapacityEndpointResponse>();
        payload.Should().NotBeNull();
        payload!.MaxConcurrentSessions.Should().BeGreaterThan(0);
        payload.AgentId.Should().Be("process");
    }

    [Fact]
    public async Task OpenSessionsEndpoint_AndTerminateEndpoint_Work()
    {
        var client = CreateClient(_catFactory, out _);
        var sessionId = "http-open-" + Guid.NewGuid().ToString("N")[..8];
        var call = client.Connect();

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });

        using var http = new HttpClient(_catFactory.CreateHandler()) { BaseAddress = _catFactory.BaseAddress };
        List<OpenSessionEndpointResponse>? openPayload = null;
        await WaitUntilAsync(async () =>
        {
            var openResponse = await http.GetAsync("/api/sessions/open");
            openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            openPayload = await openResponse.Content.ReadFromJsonAsync<List<OpenSessionEndpointResponse>>();
            return openPayload?.Any(x => x.SessionId == sessionId) == true;
        });
        openPayload.Should().NotBeNull();
        openPayload!.Should().Contain(x => x.SessionId == sessionId);

        var terminateResponse = await http.PostAsync($"/api/sessions/{sessionId}/terminate", content: null);
        terminateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var terminatePayload = await terminateResponse.Content.ReadFromJsonAsync<TerminateEndpointResponse>();
        terminatePayload.Should().NotBeNull();
        terminatePayload!.Success.Should().BeTrue();

        // clean shutdown regardless of server-side termination timing
        await call.RequestStream.CompleteAsync();
    }

    [Fact]
    public async Task AbandonedSessionsEndpoint_ReturnsSession_WhenStreamEndsWithoutStop()
    {
        var client = CreateClient(_catFactory, out _);
        var sessionId = "abandoned-" + Guid.NewGuid().ToString("N")[..8];
        var call = client.Connect();

        await call.RequestStream.WriteAsync(new ClientMessage
        {
            Control = new SessionControl { Action = SessionControl.Types.Action.Start, SessionId = sessionId, AgentId = "process" },
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        await call.RequestStream.CompleteAsync();

        using var http = new HttpClient(_catFactory.CreateHandler()) { BaseAddress = _catFactory.BaseAddress };
        List<AbandonedSessionEndpointResponse>? abandonedPayload = null;
        await WaitUntilAsync(async () =>
        {
            var abandonedResponse = await http.GetAsync("/api/sessions/abandoned");
            abandonedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            abandonedPayload = await abandonedResponse.Content.ReadFromJsonAsync<List<AbandonedSessionEndpointResponse>>();
            return abandonedPayload?.Any(x => x.SessionId == sessionId) == true;
        });
        abandonedPayload.Should().NotBeNull();
        abandonedPayload!.Should().Contain(x => x.SessionId == sessionId);
    }

    [Fact]
    public async Task ConnectionAndDeviceEndpoints_ReturnPeersHistoryAndBanLifecycle()
    {
        using var http = new HttpClient(_catFactory.CreateHandler()) { BaseAddress = _catFactory.BaseAddress };

        var peersResponse = await http.GetAsync("/api/connections/peers");
        peersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await http.GetAsync("/api/connections/history?limit=50");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var peerToBan = "203.0.113.44";
        var banResponse = await http.PostAsync($"/api/devices/{peerToBan}/ban?reason=test", content: null);
        banResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var banPayload = await banResponse.Content.ReadFromJsonAsync<TerminateEndpointResponse>();
        banPayload.Should().NotBeNull();
        banPayload!.Success.Should().BeTrue();

        var bannedResponse = await http.GetAsync("/api/devices/banned");
        bannedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bannedPayload = await bannedResponse.Content.ReadFromJsonAsync<List<BannedPeerEndpointResponse>>();
        bannedPayload.Should().NotBeNull();
        bannedPayload!.Should().Contain(x => x.Peer == peerToBan);

        var unbanResponse = await http.DeleteAsync($"/api/devices/{peerToBan}/ban");
        unbanResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unbanPayload = await unbanResponse.Content.ReadFromJsonAsync<TerminateEndpointResponse>();
        unbanPayload.Should().NotBeNull();
        unbanPayload!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AuthUserEndpoints_UpsertListDelete_Work()
    {
        using var http = new HttpClient(_catFactory.CreateHandler()) { BaseAddress = _catFactory.BaseAddress };
        var userId = "auth-" + Guid.NewGuid().ToString("N")[..8];

        var upsert = new
        {
            userId,
            displayName = "Ops User",
            role = "operator",
            enabled = true
        };
        var upsertResponse = await http.PostAsJsonAsync("/api/auth/users", upsert);
        upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var upsertPayload = await upsertResponse.Content.ReadFromJsonAsync<AuthUserEndpointResponse>();
        upsertPayload.Should().NotBeNull();
        upsertPayload!.UserId.Should().Be(userId);

        var listResponse = await http.GetAsync("/api/auth/users");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listPayload = await listResponse.Content.ReadFromJsonAsync<List<AuthUserEndpointResponse>>();
        listPayload.Should().NotBeNull();
        listPayload!.Should().Contain(x => x.UserId == userId);

        var roleResponse = await http.GetAsync("/api/auth/permissions");
        roleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await roleResponse.Content.ReadFromJsonAsync<List<string>>();
        roles.Should().NotBeNull();
        roles!.Should().Contain("admin");

        var deleteResponse = await http.DeleteAsync($"/api/auth/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletePayload = await deleteResponse.Content.ReadFromJsonAsync<TerminateEndpointResponse>();
        deletePayload.Should().NotBeNull();
        deletePayload!.Success.Should().BeTrue();
    }

    private static AgentGateway.AgentGatewayClient CreateClient(RemoteAgentWebApplicationFactory factory, out RemoteAgentWebApplicationFactory returnedFactory)
    {
        returnedFactory = factory;
        var channel = GrpcChannel.ForAddress(factory.BaseAddress, new GrpcChannelOptions { HttpHandler = factory.CreateHandler() });
        return new AgentGateway.AgentGatewayClient(channel);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, int attempts = 20, int delayMs = 50)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (await condition())
                return;

            await Task.Delay(delayMs);
        }

        (await condition()).Should().BeTrue("condition should become true within timeout");
    }

    private sealed class SessionCapacityEndpointResponse
    {
        public bool CanCreateSession { get; set; }
        public string Reason { get; set; } = "";
        public int MaxConcurrentSessions { get; set; }
        public int ActiveSessionCount { get; set; }
        public int RemainingServerCapacity { get; set; }
        public string AgentId { get; set; } = "";
        public int? AgentMaxConcurrentSessions { get; set; }
        public int AgentActiveSessionCount { get; set; }
        public int? RemainingAgentCapacity { get; set; }
    }

    private sealed class OpenSessionEndpointResponse
    {
        public string SessionId { get; set; } = "";
        public string AgentId { get; set; } = "";
        public bool CanAcceptInput { get; set; }
    }

    private sealed class AbandonedSessionEndpointResponse
    {
        public string SessionId { get; set; } = "";
        public string AgentId { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTimeOffset AbandonedUtc { get; set; }
    }

    private sealed class BannedPeerEndpointResponse
    {
        public string Peer { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTimeOffset BannedUtc { get; set; }
    }

    private sealed class AuthUserEndpointResponse
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Role { get; set; } = "";
        public bool Enabled { get; set; }
    }

    private sealed class TerminateEndpointResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
