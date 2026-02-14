using RemoteAgent.Service;
using RemoteAgent.Service.Services;

namespace RemoteAgent.Service;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));
        builder.Services.AddGrpc();

        var app = builder.Build();
        app.MapGrpcService<AgentGatewayService>();
        app.MapGet("/", () => "RemoteAgent gRPC service. Use the Android app to connect.");
        app.Run();
    }
}
