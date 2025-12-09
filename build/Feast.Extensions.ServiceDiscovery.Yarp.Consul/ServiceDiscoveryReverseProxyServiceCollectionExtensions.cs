using Feast.Extensions.ServiceDiscovery;
using Feast.Extensions.ServiceDiscovery.Internal;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.ServiceDiscovery;

// ReSharper disable once CheckNamespace
namespace Feast.Extensions.DependencyInjection;

public static class ConsulReverseProxyServiceCollectionExtensions
{
    public static IReverseProxyBuilder AddConsulDestinationResolver(this IReverseProxyBuilder builder,
        Action<ConsulDestinationResolverOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IDestinationResolver, ConsulDestinationResolver>();
        var options = new ConsulDestinationResolverOptions();
        configureOptions?.Invoke(options);
        builder.Services.AddSingleton(options);
        return builder;
    }
}