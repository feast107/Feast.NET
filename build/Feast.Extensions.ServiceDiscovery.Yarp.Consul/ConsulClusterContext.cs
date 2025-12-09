using Consul;

namespace Feast.Extensions.ServiceDiscovery;

/// <summary>
/// those have same serviceId and datacenter, consider as a same resolvation 
/// </summary>
/// <param name="DataCenter"><see cref="QueryOptions.Datacenter"/></param>
/// <param name="Service">service</param>
public sealed record ConsulClusterContext(string DataCenter, string Service);