using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RemoteAgent.Service.Tests;

/// <summary>Exposes test server handler and base address for gRPC. Agent config (Command, Arguments, RunnerId) is passed to the server; leave empty to use the strategy default for the current environment.</summary>
public class RemoteAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _command;
    private readonly string? _arguments;
    private readonly string? _runnerId;

    /// <summary>Creates a factory with the given agent config. Pass null or empty to use strategy default (process on Linux, copilot-windows on Windows).</summary>
    public RemoteAgentWebApplicationFactory(string? command = null, string? arguments = null, string? runnerId = null)
    {
        _command = command;
        _arguments = arguments;
        _runnerId = runnerId;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agent:Command"] = _command ?? "",
                ["Agent:Arguments"] = _arguments ?? "",
                ["Agent:RunnerId"] = _runnerId ?? "",
                ["Agent:LogDirectory"] = Path.GetTempPath()
            });
        });
    }

    public HttpMessageHandler CreateHandler() => Server.CreateHandler();

    public Uri BaseAddress => Server.BaseAddress;
}

public sealed class NoCommandWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    /// <summary>Use sentinel "none" so the service returns SessionError; null/empty would use runner default.</summary>
    public NoCommandWebApplicationFactory() : base("none", "", null) { }
}

public sealed class CatWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    /// <summary>Uses strategy default: process (agent) on Linux, copilot-windows on Windows. No OS-specific config.</summary>
    public CatWebApplicationFactory() : base("", "", null) { }
}

public sealed class SleepWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    /// <summary>Uses strategy default: process (agent) on Linux, copilot-windows on Windows. No OS-specific config.</summary>
    public SleepWebApplicationFactory() : base("", "", null) { }
}
