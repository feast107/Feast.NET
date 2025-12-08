using System.Collections.Concurrent;
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.ServiceDiscovery;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;

namespace Feast.Extensions.ServiceDiscovery.Yarp.Consul;

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
            foreach (var keyValuePair in Transform(worker.Entries))
            {
                result.Add(keyValuePair.Key, keyValuePair.Value);
            }
            tokens.Add(new CancellationChangeToken(worker.ChangeToken));
        }
        return new ResolvedDestinationCollection(result, new CompositeChangeToken(tokens));
    }

    private static IEnumerable<KeyValuePair<string, DestinationConfig>> Transform(IEnumerable<ServiceEntry> entries) =>
        from entry in entries 
        select new KeyValuePair<string, DestinationConfig>($"consul/{entry.Node.Name}/{entry.Service.ID}",
            new ()
            {
                Address  = $"{GetSchema(entry.Service.Meta)}://{entry.Service.Address}:{entry.Service.Port}",
                Metadata = new Dictionary<string, string>(entry.Service.Meta),
            });

    private static string GetSchema(IDictionary<string, string> meta) => 
        meta.TryGetValue("secure", out var secure) && secure == "false" ? "http" : "https";

}
