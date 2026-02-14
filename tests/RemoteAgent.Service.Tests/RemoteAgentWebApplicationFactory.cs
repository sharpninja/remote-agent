using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RemoteAgent.Service.Tests;

/// <summary>Exposes test server handler and base address for gRPC. Configure Agent:Command via constructor.</summary>
public class RemoteAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _command;
    private readonly string? _arguments;

    public RemoteAgentWebApplicationFactory(string? command = null, string? arguments = null)
    {
        _command = command;
        _arguments = arguments;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agent:Command"] = _command ?? "",
                ["Agent:Arguments"] = _arguments ?? "",
                ["Agent:LogDirectory"] = Path.GetTempPath()
            });
        });
    }

    public HttpMessageHandler CreateHandler() => Server.CreateHandler();

    public Uri BaseAddress => Server.BaseAddress;
}

public sealed class NoCommandWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public NoCommandWebApplicationFactory() : base("") { }
}

public sealed class CatWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public CatWebApplicationFactory() : base("/bin/cat", "") { }
}

public sealed class SleepWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public SleepWebApplicationFactory() : base(
        OperatingSystem.IsWindows() ? "cmd" : "sleep",
        OperatingSystem.IsWindows() ? "/c ping -n 6 127.0.0.1" : "5") { }
}
