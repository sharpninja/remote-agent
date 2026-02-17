using Xunit;

namespace RemoteAgent.Service.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ServiceIntegrationSequentialCollection
{
    public const string Name = "ServiceIntegrationSequential";
}
