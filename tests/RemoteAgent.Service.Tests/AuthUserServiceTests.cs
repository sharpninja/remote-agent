using FluentAssertions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

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
