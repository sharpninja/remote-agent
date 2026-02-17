using System.Runtime.CompilerServices;

namespace RemoteAgent.Service.IntegrationTests;

internal static class TestProcessInitialization
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        const string key = "DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS";
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, "20");
    }
}
