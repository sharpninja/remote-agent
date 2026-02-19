using FluentAssertions;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.UiTests.Handlers;

/// <summary>Tests for <see cref="SetManagementSectionHandler"/>. FR-12.1; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.1")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class SetManagementSectionHandlerTests
{
    private readonly SetManagementSectionHandler _handler = new();

    // FR-12.1, TR-18.4
    [Fact]
    public async Task Handle_WithValidSectionKey_ShouldReturnUnit()
    {
        var request = new SetManagementSectionRequest(Guid.NewGuid(), "Sessions");

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }

    // FR-12.1, TR-18.4
    [Fact]
    public async Task Handle_WithEmptySectionKey_ShouldReturnUnit()
    {
        var request = new SetManagementSectionRequest(Guid.NewGuid(), "");

        var result = await _handler.HandleAsync(request);

        result.Should().Be(Unit.Value);
    }
}
