using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Logging;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

public class ConnectionProtectionServiceTests : IDisposable
{
    private readonly StructuredLogService _structuredLogs;

    public ConnectionProtectionServiceTests()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);
        _structuredLogs = new StructuredLogService(
            Options.Create(new AgentOptions { LogDirectory = dataDir }),
            NullLogger<StructuredLogService>.Instance);
    }

    [Fact]
    public void TryOpenConnection_Denies_WhenConcurrentLimitReached()
    {
        var service = CreateService(new AgentOptions
        {
            MaxConcurrentConnectionsPerPeer = 1
        });

        var first = service.TryOpenConnection("ipv4:127.0.0.1:5243", "test");
        var second = service.TryOpenConnection("ipv4:127.0.0.1:6000", "test");

        first.Allowed.Should().BeTrue();
        second.Allowed.Should().BeFalse();
        second.DeniedReason.Should().Contain("active connections");
    }

    [Fact]
    public void TryRegisterClientMessage_Denies_WhenMessageRateExceeded()
    {
        var service = CreateService(new AgentOptions
        {
            MaxClientMessagesPerWindow = 1,
            ClientMessageWindowSeconds = 60
        });

        var opened = service.TryOpenConnection("ipv4:127.0.0.1:5001", "test");
        opened.Allowed.Should().BeTrue();

        service.TryRegisterClientMessage(opened.Peer, "test").Should().BeTrue();
        service.TryRegisterClientMessage(opened.Peer, "test").Should().BeFalse();
    }

    [Fact]
    public void TryOpenConnection_BlocksPeer_AfterDosThreshold()
    {
        var service = CreateService(new AgentOptions
        {
            MaxConnectionAttemptsPerWindow = 1,
            ConnectionAttemptWindowSeconds = 60,
            DosViolationThreshold = 1,
            DosBlockSeconds = 30
        });

        var first = service.TryOpenConnection("ipv4:127.0.0.1:5001", "test");
        var second = service.TryOpenConnection("ipv4:127.0.0.1:5002", "test");
        var third = service.TryOpenConnection("ipv4:127.0.0.1:5003", "test");

        first.Allowed.Should().BeTrue();
        second.Allowed.Should().BeFalse();
        third.Allowed.Should().BeFalse();
        third.DeniedReason.Should().Contain("temporarily blocked");
    }

    [Fact]
    public void BanPeer_DeniesFutureConnections_UntilUnbanned()
    {
        var service = CreateService(new AgentOptions());
        var peer = "192.0.2.25";

        service.BanPeer(peer, "test-ban", "test").Should().BeTrue();
        service.IsPeerBanned(peer).Should().BeTrue();

        var denied = service.TryOpenConnection($"ipv4:{peer}:5001", "test");
        denied.Allowed.Should().BeFalse();
        denied.DeniedReason.Should().Contain("banned");

        service.UnbanPeer(peer, "test").Should().BeTrue();
        service.IsPeerBanned(peer).Should().BeFalse();
    }

    [Fact]
    public void GetConnectionHistory_ReturnsRecentEvents()
    {
        var service = CreateService(new AgentOptions());
        var decision = service.TryOpenConnection("ipv4:127.0.0.1:5001", "test");

        decision.Allowed.Should().BeTrue();
        service.TryRegisterClientMessage(decision.Peer, "test").Should().BeTrue();
        service.CloseConnection(decision.Peer);

        var history = service.GetConnectionHistory(10);
        history.Should().NotBeEmpty();
        history.Should().Contain(x => x.Action == "connection_open");
        history.Should().Contain(x => x.Action == "client_message");
        history.Should().Contain(x => x.Action == "connection_close");
    }

    public void Dispose()
    {
        _structuredLogs.Dispose();
    }

    private ConnectionProtectionService CreateService(AgentOptions options)
    {
        options.LogDirectory ??= Path.GetTempPath();
        return new ConnectionProtectionService(Options.Create(options), _structuredLogs);
    }
}
