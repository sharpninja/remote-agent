using FluentAssertions;
using Xunit;

namespace RemoteAgent.Service.IntegrationTests;

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

