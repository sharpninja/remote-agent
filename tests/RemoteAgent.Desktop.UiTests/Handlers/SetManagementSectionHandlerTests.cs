using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

public class SetManagementSectionHandlerTests
{
    private readonly SetManagementSectionHandler _handler = new();

    [Fact]
    public async Task Handle_WithValidSectionKey_ShouldReturnUnit()
    {
        var request = new SetManagementSectionRequest(Guid.NewGuid(), "Sessions");

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_WithEmptySectionKey_ShouldReturnUnit()
    {
        var request = new SetManagementSectionRequest(Guid.NewGuid(), "");

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }
}
