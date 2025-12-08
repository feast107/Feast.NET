using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddKubernetesEnvironment("k8s")
    .WithProperties(resource =>
    {
        resource.HelmChartName = "feast-aspire-app";
    }); 

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("database");

/*var serviceBus = builder.AddAzureServiceBus("service-bus")
    .RunAsEmulator();

serviceBus.AddServiceBusQueue("entities");*/

var consul = builder.AddContainer("consul", "hashicorp/consul")
    .WithEndpoint(8500, 8500)
    .WithUrl("http://localhost:8500");


var dbService = builder.AddProject<Feast_Aspire_DbService>("db-service")
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(consul);

var apiService = builder.AddProject<Feast_Aspire_ApiService>("api-service")
    .WithReference(dbService)
    .WaitFor(dbService)
    .WaitFor(consul);

var gateway = builder.AddProject<Feast_Aspire_Gateway>("gateway")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();