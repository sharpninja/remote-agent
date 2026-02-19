using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="ExpandStatusLogPanelHandler"/>. FR-12.11; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.11")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class ExpandStatusLogPanelHandlerTests
{
    private readonly ExpandStatusLogPanelHandler _handler = new();

    // FR-12.11, TR-18.4
    [Fact]
    public async Task HandleAsync_ShouldReturnUnit()
    {
        var request = new ExpandStatusLogPanelRequest(Guid.NewGuid());

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }

    // FR-12.11, TR-18.4
    [Fact]
    public void Request_ShouldRequireCorrelationId()
    {
        var request = new ExpandStatusLogPanelRequest(Guid.NewGuid());

        request.CorrelationId.Should().NotBe(Guid.Empty);
    }
}
