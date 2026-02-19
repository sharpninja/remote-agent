using FluentAssertions;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Tests;

/// <summary>Error-handling tests for <see cref="RemoteAgent.App.Logic.IServerApiClient"/> implementations. FR-1.1, FR-12.2; TR-8.1, TR-8.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.1")]
[Trait("Requirement", "FR-12.2")]
[Trait("Requirement", "TR-8.1")]
[Trait("Requirement", "TR-8.2")]
public sealed class ServerApiClientErrorHandlingTests
{
    [Fact]
    public async Task GetPluginsAsync_WhenThrowOnErrorFalse_ShouldReturnNullOnConnectionFailure()
    {
        var response = await ServerApiClient.GetPluginsAsync(
            host: "127.0.0.1",
            port: 1,
            apiKey: null,
            ct: CancellationToken.None,
            throwOnError: false);

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetPluginsAsync_WhenThrowOnErrorTrue_ShouldThrowDetailedExceptionOnConnectionFailure()
    {
        var act = async () => await ServerApiClient.GetPluginsAsync(
            host: "127.0.0.1",
            port: 1,
            apiKey: null,
            ct: CancellationToken.None,
            throwOnError: true);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("Get plugins failed");
    }
}
