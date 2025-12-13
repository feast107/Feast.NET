using System.Collections.Concurrent;
using System.Diagnostics;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Feast.UnitTest;

public class TestServer
{
    public required WebApplicationBuilder Builder { get; set; }
    [SetUp]
    public void Setup()
    {
        Builder = WebApplication.CreateSlimBuilder();
        Builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenAnyIP(5000);
        });
        Builder.Services.AddReverseProxy().LoadFromMemory(
        [
            new ()
            {
                RouteId = "test-route",
                ClusterId = "test-cluster",
                Match = new ()
                {
                    Path = "/{**all}"
                }
            }
        ], [
            new ()
            {
                ClusterId = "test-cluster",
                Destinations = new Dictionary<string, DestinationConfig>()
                {
                    {  "found", new()
                    {
                        Address = "http://127.0.0.1",
                    } }
                }
            }
        ]);
        Builder.Services.AddSingleton<IDestinationResolver,NoneDestinationResolver>();
        Builder.Services.AddSingleton<IDestinationResolver,NoneDestinationResolver2>();
    }
    
    [Test]
    public async Task Run()
    {
        var app =  Builder.Build();
        app.MapReverseProxy();
        await app.RunAsync();
    }
}

file class NoneDestinationResolver : IDestinationResolver
{
    public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(IReadOnlyDictionary<string, DestinationConfig> destinations,
        CancellationToken cancellationToken)
    {
        Debugger.Break();
        return new (destinations, null);
    }
}

file class NoneDestinationResolver2 : IDestinationResolver
{
    public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(IReadOnlyDictionary<string, DestinationConfig> destinations,
        CancellationToken cancellationToken)
    {
        Debugger.Break();
        throw new  NotImplementedException();
        return new (new Dictionary<string, DestinationConfig>(), null);
    }
}
