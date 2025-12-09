using Feast.Aspire.AppHost.Mounts;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddKubernetesEnvironment("k8s")
    .WithProperties(resource =>
    {
        resource.HelmChartName = "feast-aspire-app";
    });

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("database");

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

var consul = builder.AddContainer("consul", "hashicorp/consul")
    .WithEndpoint(8500, 8500)
    .WithArgs(c =>
    {
        c.Args.Add("agent");
        c.Args.Add("-dev");
        c.Args.Add("-client");
        c.Args.Add("0.0.0.0");
        c.Args.Add("-config-dir=/etc/consul.d/");
    })
    //if using acl, this should be persistent
    //& call 'consul acl bootstrap' to get secret-id add into token
    .WithLifetime(ContainerLifetime.Persistent)
    .WithUrl("http://localhost:8500")
    .WithDefaultMount();


var dbService = builder.AddProject<Feast_Aspire_DbService>("db-service")
    .WithReference(database)
    .WithConsulToken()
    .WaitFor(database)
    .WaitFor(consul);

var apiService = builder.AddProject<Feast_Aspire_ApiService>("api-service")
    .WithReference(dbService)
    .WithConsulToken()
    //.WithReplicas(2)
    .WaitFor(dbService)
    .WaitFor(consul);

var gateway = builder.AddProject<Feast_Aspire_Gateway>("gateway")
    .WithConsulToken()
    .WithReference(apiService)
    .WaitFor(apiService);

var app = builder.Build();

await app.RunAsync();

file static class Extensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithConsulToken() => 
            builder.WithEnvironment("Consul:Token", "ffdea316-fbdd-3ddd-52ea-bba6a9d23c39");
    }
}