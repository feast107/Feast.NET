using System.Collections.Concurrent;
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.ServiceDiscovery;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;

namespace Feast.Extensions.ServiceDiscovery.Internal;

internal class ConsulDestinationResolver(
    IEnumerable<IConsulClient> clients,
    ConsulDestinationResolverOptions options,
    ILoggerFactory loggerFactory) : IDestinationResolver
{
    private readonly IConsulClient[] clients = clients.ToArray();

    private readonly ConcurrentDictionary<ConsulClusterContext, ConsulNotifyWorkersPlane> workers = new();

    public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(
        IReadOnlyDictionary<string, DestinationConfig> destinations,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DestinationConfig>();
        var tokens = new List<IChangeToken>();
        foreach (var (datacenter, config) in destinations)
        {
            var context = options.TransformContext(datacenter, config);
            if (context is null)
            {
                result.Add(datacenter, config);
                continue;
            }
            var worker = workers.GetOrAdd(context, id =>
                new ConsulNotifyWorkersPlane(loggerFactory.CreateLogger<ConsulNotifyWorkersPlane>(), clients, options.OptionsFactory, id));
            if (!worker.Initialized)
            {
                await worker.StartAsync(cancellationToken);
            }
            foreach (var destinationConfig in worker.Entries)
            {
                result.Add(destinationConfig.Id, destinationConfig.DestinationConfig);
            }
            tokens.Add(new CancellationChangeToken(worker.ChangeToken));
        }
        return new ResolvedDestinationCollection(result, new CompositeChangeToken(tokens));
    }
    
}
