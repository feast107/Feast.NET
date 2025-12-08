using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;

namespace Feast.Extensions.ServiceDiscovery.Yarp.Consul;

public partial class ConsulDestinationResolverOptions
{
    private static readonly Regex Regex = ConsulDcRegex();
    
    /// <summary>
    /// Configured options will be passed to each query
    /// </summary>
    public Func<ConsulClusterContext, ConsulQueryOptions> OptionsFactory { get; set; } = DefaultConfigure;
    
    /// <summary>
    /// Define how original transform should be treated as consul destination, if null, will be passed directly to Yarp
    /// </summary>
    public Func<string, DestinationConfig, ConsulClusterContext?> TransformContext { get; set; } = DefaultTransform;
    
    private static readonly Func<ConsulClusterContext, ConsulQueryOptions> DefaultConfigure = _ => ConsulQueryOptions.Default;

    private static readonly Func<string, DestinationConfig, ConsulClusterContext?> DefaultTransform = (name, config) =>
    {
        if (!Regex.IsMatch(name)) return null;
        var serviceId  = new Uri(config.Address).Host;
        var datacenter = name.Split('/')[1];
        return new ConsulClusterContext(datacenter, serviceId);
    };
#if !NET6_0
    [GeneratedRegex("consul/dc\\d*")]
#endif
    private static partial Regex ConsulDcRegex();
    
    
#if NET6_0
    private static partial Regex ConsulDcRegex() => new("consul/dc\\d*");
#endif
}