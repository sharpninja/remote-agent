using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));
        builder.Services.Configure<PluginsOptions>(builder.Configuration.GetSection(PluginsOptions.SectionName));
        builder.Services.AddSingleton<ProcessAgentRunner>();
        builder.Services.AddSingleton<IReadOnlyDictionary<string, IAgentRunner>>(sp => PluginLoader.BuildRunnerRegistry(sp));
        builder.Services.AddSingleton<IAgentRunnerFactory, DefaultAgentRunnerFactory>();
        builder.Services.AddGrpc();

        var app = builder.Build();
        app.MapGrpcService<AgentGatewayService>();
        app.MapGet("/", () => "RemoteAgent gRPC service. Use the Android app to connect.");
        app.Run();
    }
}
