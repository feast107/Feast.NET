using Consul;

namespace Feast.Extensions.ServiceDiscovery;

public class ConsulQueryOptions : QueryOptions
{
    public new static readonly ConsulQueryOptions Default = new()
    {
        Consistency = ConsistencyMode.Default,
        Datacenter  = string.Empty,
        Token       = string.Empty,
        WaitIndex   = 0
    };
    
    public string Tag { get; set; } = string.Empty;
    
    /// <summary>
    /// Default is true
    /// </summary>
    public bool PassingOnly { get; set; } = true;
}