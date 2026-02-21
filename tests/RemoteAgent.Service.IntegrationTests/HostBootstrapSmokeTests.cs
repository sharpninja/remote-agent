using FluentAssertions;
using Xunit;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Integration smoke tests for service bootstrap. FR-1.1, FR-1.2; TR-1.3, TR-2.2, TR-3.1.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.1")]
[Trait("Requirement", "FR-1.2")]
[Trait("Requirement", "TR-1.3")]
[Trait("Requirement", "TR-2.2")]
[Trait("Requirement", "TR-3.1")]
public class HostBootstrapSmokeTests
{
    [Fact]
    public async Task RootEndpoint_RespondsQuickly()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var factoryTask = Task.Run(() => new NoCommandWebApplicationFactory(), cts.Token);
        var completed = await Task.WhenAny(factoryTask, Task.Delay(Timeout.Infinite, cts.Token));
        completed.Should().Be(factoryTask, "factory construction should complete quickly");

        var factory = await factoryTask;
        using var _ = factory;
        using var client = new HttpClient(factory.CreateHandler())
        {
            BaseAddress = factory.BaseAddress,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var text = await client.GetStringAsync("/");
        text.Should().Contain("RemoteAgent gRPC service");
    }
}

