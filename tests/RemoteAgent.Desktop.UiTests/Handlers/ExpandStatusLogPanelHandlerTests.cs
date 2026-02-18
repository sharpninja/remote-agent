using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class ExpandStatusLogPanelHandlerTests
{
    private readonly ExpandStatusLogPanelHandler _handler = new();

    [Fact]
    public async Task HandleAsync_ShouldReturnUnit()
    {
        var request = new ExpandStatusLogPanelRequest(Guid.NewGuid());

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public void Request_ShouldRequireCorrelationId()
    {
        var request = new ExpandStatusLogPanelRequest(Guid.NewGuid());

        request.CorrelationId.Should().NotBe(Guid.Empty);
    }
}
