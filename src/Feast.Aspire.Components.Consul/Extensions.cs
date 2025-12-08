using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.HttpClients;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Feast.Aspire.Components.Consul;

public static class Extensions
{
    extension(IHttpClientBuilder builder)
    {
        public IHttpClientBuilder AddConsulServiceDiscovery() => builder.AddServiceDiscovery();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TLoadBalancer"></typeparam>
        /// <returns></returns>
        public IHttpClientBuilder AddConsulServiceDiscovery<TLoadBalancer>()
            where TLoadBalancer : class, ILoadBalancer =>
            builder.AddServiceDiscovery<TLoadBalancer>();
    }

}