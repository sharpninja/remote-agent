using FluentAssertions;
using RemoteAgent.App.Handlers;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Tests;

/// <summary>
/// Tests for server-profile CQRS handlers: SaveServerProfile, DeleteServerProfile, ClearServerApiKey.
/// FR-19.1, FR-19.2; TR-21.1.
/// </summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-19.1")]
[Trait("Requirement", "FR-19.2")]
[Trait("Requirement", "TR-21.1")]
public sealed class ServerProfileHandlerTests
{
    // -----------------------------------------------------------------------
    // Stubs
    // -----------------------------------------------------------------------

    private sealed class InMemoryProfileStore : IServerProfileStore
    {
        private readonly List<ServerProfile> _profiles = [];

        public IReadOnlyList<ServerProfile> GetAll() => _profiles.AsReadOnly();

        public ServerProfile? GetByHostPort(string host, int port) =>
            _profiles.FirstOrDefault(p => p.Host == host && p.Port == port);

        public void Upsert(ServerProfile profile)
        {
            var existing = GetByHostPort(profile.Host, profile.Port);
            if (existing != null)
                _profiles.Remove(existing);
            _profiles.Add(profile);
        }

        public bool Delete(string host, int port)
        {
            var existing = GetByHostPort(host, port);
            return existing != null && _profiles.Remove(existing);
        }
    }

    private sealed class NullRequestDispatcher : IRequestDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
            Task.FromResult(default(TResponse)!);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (SettingsPageViewModel vm, InMemoryProfileStore store) CreateWorkspace(params ServerProfile[] seed)
    {
        var store = new InMemoryProfileStore();
        foreach (var p in seed) store.Upsert(p);
        var vm = new SettingsPageViewModel(store, new NullRequestDispatcher());
        return (vm, store);
    }

    private static ServerProfile MakeProfile(string host = "10.0.0.1", int port = 5243,
        string apiKey = "test-key", string displayName = "Test") =>
        new() { Host = host, Port = port, ApiKey = apiKey, DisplayName = displayName };

    // -----------------------------------------------------------------------
    // SaveServerProfileHandler
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Save_PersistsEditedFields()
    {
        var profile = MakeProfile();
        var (vm, store) = CreateWorkspace(profile);
        vm.SelectedProfile = vm.Profiles[0];
        vm.EditDisplayName = "Updated Name";
        vm.EditPerRequestContext = "ctx";
        vm.EditDefaultSessionContext = "session ctx";

        var handler = new SaveServerProfileHandler(store);
        var result = await handler.HandleAsync(new SaveServerProfileRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        var saved = store.GetByHostPort("10.0.0.1", 5243);
        saved.Should().NotBeNull();
        saved!.DisplayName.Should().Be("Updated Name");
        saved.PerRequestContext.Should().Be("ctx");
        saved.DefaultSessionContext.Should().Be("session ctx");
    }

    [Fact]
    public async Task Save_FailsWhenNoSelection()
    {
        var (vm, store) = CreateWorkspace();
        var handler = new SaveServerProfileHandler(store);
        var result = await handler.HandleAsync(new SaveServerProfileRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // DeleteServerProfileHandler
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_RemovesProfile()
    {
        var profile = MakeProfile();
        var (vm, store) = CreateWorkspace(profile);
        vm.SelectedProfile = vm.Profiles[0];

        var handler = new DeleteServerProfileHandler(store);
        var result = await handler.HandleAsync(new DeleteServerProfileRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        store.GetAll().Should().BeEmpty();
        vm.SelectedProfile.Should().BeNull();
    }

    [Fact]
    public async Task Delete_FailsWhenNoSelection()
    {
        var (vm, store) = CreateWorkspace();
        var handler = new DeleteServerProfileHandler(store);
        var result = await handler.HandleAsync(new DeleteServerProfileRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // ClearServerApiKeyHandler
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ClearApiKey_RemovesKeyFromProfile()
    {
        var profile = MakeProfile(apiKey: "secret-123");
        var (vm, store) = CreateWorkspace(profile);
        vm.SelectedProfile = vm.Profiles[0];

        var handler = new ClearServerApiKeyHandler(store);
        var result = await handler.HandleAsync(new ClearServerApiKeyRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.HasApiKey.Should().BeFalse();
        store.GetByHostPort("10.0.0.1", 5243)!.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearApiKey_FailsWhenNoSelection()
    {
        var (vm, store) = CreateWorkspace();
        var handler = new ClearServerApiKeyHandler(store);
        var result = await handler.HandleAsync(new ClearServerApiKeyRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ClearApiKey_IdempotentWhenAlreadyEmpty()
    {
        var profile = MakeProfile(apiKey: "");
        var (vm, store) = CreateWorkspace(profile);
        vm.SelectedProfile = vm.Profiles[0];

        var handler = new ClearServerApiKeyHandler(store);
        var result = await handler.HandleAsync(new ClearServerApiKeyRequest(Guid.NewGuid(), vm));

        result.Success.Should().BeTrue();
        vm.HasApiKey.Should().BeFalse();
    }
}
