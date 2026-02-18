using FluentAssertions;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Tests;

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
