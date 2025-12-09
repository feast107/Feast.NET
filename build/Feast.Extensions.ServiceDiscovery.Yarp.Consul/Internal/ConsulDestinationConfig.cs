using Consul;
using Yarp.ReverseProxy.Configuration;

namespace Feast.Extensions.ServiceDiscovery.Internal;

internal record ConsulDestinationConfig(string Id, DestinationConfig DestinationConfig)
{
    public static ConsulDestinationConfig Transform(ServiceEntry entry) =>
        new($"consul/{entry.Node.Name}/{entry.Service.ID}",
            new()
            {
                Address  = $"{GetSchema(entry.Service.Meta)}://{entry.Service.Address}:{entry.Service.Port}",
                Metadata = new Dictionary<string, string>(entry.Service.Meta),
            });

    private static string GetSchema(IDictionary<string, string> meta) => 
        meta.TryGetValue("secure", out var secure) && secure == "false" ? "http" : "https";
}