using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service.Tests;

public class PluginConfigurationServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ra-plugin-tests-" + Guid.NewGuid().ToString("N"));

    public PluginConfigurationServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Loads_FromOptions_WhenNoPersistedFile()
    {
        var sut = NewService(new PluginsOptions { Assemblies = ["a.dll", "b.dll"] });
        sut.GetAssemblies().Should().Equal("a.dll", "b.dll");
    }

    [Fact]
    public void Loads_PersistedFile_WhenPresent()
    {
        File.WriteAllText(Path.Combine(_tempDir, "plugins.json"), "{\"Assemblies\":[\"x.dll\",\"y.dll\"]}");

        var sut = NewService(new PluginsOptions { Assemblies = ["a.dll"] });
        sut.GetAssemblies().Should().Equal("x.dll", "y.dll");
    }

    [Fact]
    public void UpdateAssemblies_Deduplicates_AndPersists()
    {
        var sut = NewService(new PluginsOptions());
        var result = sut.UpdateAssemblies([" a.dll ", "A.dll", "", "b.dll"]);

        result.Should().Equal("a.dll", "b.dll");
        var persisted = File.ReadAllText(Path.Combine(_tempDir, "plugins.json"));
        persisted.Should().Contain("a.dll");
        persisted.Should().Contain("b.dll");
    }

    private PluginConfigurationService NewService(PluginsOptions plugins)
    {
        return new PluginConfigurationService(
            Options.Create(new AgentOptions { DataDirectory = _tempDir }),
            Options.Create(plugins),
            NullLogger<PluginConfigurationService>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
