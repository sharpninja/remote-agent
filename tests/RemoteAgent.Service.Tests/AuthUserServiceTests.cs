using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Services;
using Xunit;

namespace RemoteAgent.Service.Tests;

/// <summary>Tests for <see cref="AuthUserService"/>. FR-13.5; TR-18.1, TR-18.2.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-13.5")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
public sealed class AuthUserServiceTests
{
    [Fact]
    public void UpsertListDelete_ShouldPersistUser()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);

        var service = new AuthUserService(Options.Create(new AgentOptions { DataDirectory = dataDir }));
        var saved = service.Upsert(new AuthUserRecord
        {
            UserId = "operator1",
            DisplayName = "Operator One",
            Role = "operator",
            Enabled = true
        });

        saved.UserId.Should().Be("operator1");
        saved.Role.Should().Be("operator");

        var listed = service.List();
        listed.Should().Contain(x => x.UserId == "operator1" && x.Role == "operator");

        var removed = service.Delete("operator1");
        removed.Should().BeTrue();
        service.List().Should().NotContain(x => x.UserId == "operator1");
    }

    [Fact]
    public void ListRoles_ShouldContainExpectedDefaults()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), "remote-agent-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDir);
        var service = new AuthUserService(Options.Create(new AgentOptions { DataDirectory = dataDir }));

        var roles = service.ListRoles();
        roles.Should().Contain("viewer");
        roles.Should().Contain("operator");
        roles.Should().Contain("admin");
    }
}
