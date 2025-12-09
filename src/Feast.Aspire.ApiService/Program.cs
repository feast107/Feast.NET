using Confluent.Kafka;
using Feast.Aspire.ApiService;
using Feast.Aspire.Components.Consul;
using Feast.Aspire.Components.Kafka.Serialization;
using Feast.Aspire.Extensions.ServiceDefaults;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddConsulDiscoveryClient();

// kafka在多消费端的情况下会集中将消息发往一个消费端，如果这个消费端持续无法消费消息，则会转向其他候选发送
builder.AddKafkaConsumer<Guid, string>("kafka", c =>
{
    c.Config.GroupId               = "my-aspire-group";
    c.Config.ClientId              = $"api-service-{Guid.CreateVersion7()}";
    c.Config.AllowAutoCreateTopics = true;
    // 禁用自动commit以启动消息手动确认
    c.Config.EnableAutoCommit      = false;
    // 2025/12/09实测出现了已确认消息未传达到kafka的情况，导致消息被视作未处理而转发到另一消费端
    // 判断为服务调用了Commit但是sdk并未及时回传给kafka。即便有手动确认，业务侧也要在处理时二次确认
}, b =>
{
    b.SetKeyDeserializer(KafkaGuidSerializer.Instance);
});


// add consul service discovery
builder.Services.AddHttpClient("db-service-client", client =>
{
    client.BaseAddress = new Uri("http://db-service");
}).AddConsulServiceDiscovery<RandomLoadBalancer>();
builder.Services.AddHostedService<KafkaHostedConsumer>();
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
