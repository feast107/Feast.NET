using Feast.Aspire.Components.Consul;
using Feast.Aspire.Extensions.ServiceDefaults;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddConsulDiscoveryClient();

// add consul service discovery
builder.Services.AddHttpClient("db-service-client", client =>
{
    client.BaseAddress = new Uri("http://db-service");
}).AddConsulServiceDiscovery<RandomLoadBalancer>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/{**_}", async (HttpContext context, IHttpClientFactory factory) =>
{
    using var client = factory.CreateClient("db-service-client");
    context.Request.EnableBuffering();
    var message = new HttpRequestMessage(HttpMethod.Parse(context.Request.Method), context.Request.Path)
    {
        Content = new StreamContent(context.Request.Body)
    };
    foreach (var (key,value) in context.Request.Headers) message.Headers.TryAddWithoutValidation(key, value.ToString());
    var response = await client.SendAsync(message);
    response.Headers.Clear();
    context.Response.StatusCode    = (int)response.StatusCode;
    if (response.IsSuccessStatusCode)
    {
        context.Response.ContentType   = response.Content.Headers.ContentType?.ToString();
        context.Response.ContentLength = response.Content.Headers.ContentLength;
        await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
    }
    await context.Response.CompleteAsync();
});
app.Run();