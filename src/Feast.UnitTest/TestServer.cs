using System.Text;
using System.Text.Json;
using Feast.Extensions.Http;
using Yarp.ReverseProxy.Configuration;

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
            new()
            {
                RouteId   = "test-route",
                ClusterId = "test-cluster",
                Match = new()
                {
                    Path = "/{**all}"
                },
                Transforms = [
                    new Dictionary<string, string>()
                    {
                        { "PathPrefix", "receives/" }
                    }
                ]
            }
        ], [
            new()
            {
                ClusterId = "test-cluster",
                Destinations = new Dictionary<string, DestinationConfig>()
                {
                    {
                        "found", new()
                        {
                            Address = "http://127.0.0.1:5000",
                        }
                    }
                }
            }
        ]);
    }
    
    
    [Test]
    public async Task Run()
    {
        var app =  Builder.Build();
        app.MapGet("receives/{**all}", async (context) =>
        {
            var content = await new StreamReader(context.Request.Body).ReadToEndAsync();
            await Results.Ok().ExecuteAsync(context);
        });
        app.MapReverseProxy(b =>
        {
            b.Use(async (HttpContext context, RequestDelegate pass) =>
            {
                //context.Request.EnableBuffering();
                using (context.Request.EnableSwitchableBuffering())
                {
                    var buffer = new byte[1000];
                    await context.Request.Body.ReadExactlyAsync(buffer);
                    var content = Encoding.UTF8.GetString(buffer);
                    var jsonReader = new Utf8JsonReader(buffer);
                    while (jsonReader.Read())
                    {
                        if (jsonReader.CurrentDepth > 1) break;
                        if (jsonReader.TokenType != JsonTokenType.PropertyName) continue;
                        var name = jsonReader.GetString();
                        if (jsonReader.Read())
                        {
                            var value = jsonReader.GetString();
                        }
                    }
                }
                context.Request.Body.Position = 0;
                await pass(context);
            });
        });
        await app.RunAsync();
    }
    
}

