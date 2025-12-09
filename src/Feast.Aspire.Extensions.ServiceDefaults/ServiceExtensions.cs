using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Feast.Aspire.Extensions.ServiceDefaults;

public static class ServiceExtensions
{
    public static IServiceCollection AddRegisteredHostedService<TService>(this IServiceCollection collection)
        where TService : class, IHostedService => collection.AddHostedService<TService>(sp => sp.GetRequiredService<TService>());
}