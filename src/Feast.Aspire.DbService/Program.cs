using Confluent.Kafka;
using Feast.Aspire.DbService;
using Feast.Aspire.DbService.Endpoints;
using Feast.Aspire.Extensions.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Steeltoe.Discovery.Consul;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MainDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("database")));
builder.Services.AddHostedService<MainDbContext>(s =>
{
    var scope   = s.CreateScope();
    return scope.ServiceProvider.GetRequiredService<MainDbContext>();
});
builder.AddKafkaProducer<int, int>("", configureSettings: c =>
{
    IProducer<int, int> p;
    c.Config.Acks = Acks.All;
});
// register self to consul
// unfortunately this method only accepts IConfiguration
builder.Services.AddConsulDiscoveryClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/swagger/v1/swagger.json");
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapGroup("db").WithTags("Database").MapDbEndpoints();
app.MapGroup("ctl").WithTags("Control").MapControlEndpoints();
app.Run();
