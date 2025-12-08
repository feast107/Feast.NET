using Feast.Aspire.Extensions.ServiceDefaults;
using Feast.Extensions.DependencyInjection;
using Steeltoe.Discovery.Consul;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddConsulDiscoveryClient();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("Proxy"))
    .AddConsulDestinationResolver();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapReverseProxy();
app.Run();