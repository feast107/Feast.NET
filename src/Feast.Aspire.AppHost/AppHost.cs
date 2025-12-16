using System.Diagnostics;
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
string? consulToken = null;
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
    .WithUrl("http://localhost:8500")
    .WithDefaultMount()
    //if using acl call 'consul acl bootstrap' to get secret-id add into token
    .OnResourceReady(async (r, e, c) =>
    {
        foreach (var name in r.GetContainerNames())
        {
            var info = new ProcessStartInfo()
            {
                FileName               = "cmd",
                Arguments              = $"docker exec {name} consul acl bootstrap",
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true 
            };
            using var cmd = Process.Start(info)!;
            await cmd.StandardInput.WriteAsync($"docker exec {name} consul acl bootstrap\n");
            while (await cmd.StandardOutput.ReadLineAsync() is { } output)
            {
                if (!output.StartsWith("SecretID:")) continue;
                consulToken = output.Replace("SecretID:","").Trim();
                break;
            }
        }
    });;


var dbService = builder.AddProject<Feast_Aspire_DbService>("db-service")
    .WithConsulToken(() => consulToken)
    .WithReference(database)
    .WithReference(kafka)
    .WaitFor(database)
    .WaitFor(consul)
    .WaitFor(kafka);

var apiService = builder.AddProject<Feast_Aspire_ApiService>("api-service")
    .WithReference(dbService)
    .WithConsulToken(() => consulToken)
    .WithReference(kafka)
    //.WithReplicas(2)
    .WaitFor(dbService)
    .WaitFor(consul)
    .WaitFor(kafka);

var gateway = builder.AddProject<Feast_Aspire_Gateway>("gateway")
    .WithReference(apiService)
    .WithConsulToken(() => consulToken)
    .WaitFor(apiService);

var app = builder.Build();

await app.RunAsync();

file static class Extensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithConsulToken(Func<string?> token) =>
            builder.WithEnvironment(c =>
            {
                c.EnvironmentVariables.Add("Consul:Token", token() ?? throw new ArgumentNullException(nameof(token)));
            });
    }
    
    public static IEnumerable<string> GetContainerNames(this ContainerResource container)
    {
        var dcp       = container.Annotations.First(x => x.GetType().Name == "DcpInstancesAnnotation");
        if (dcp.GetType().GetProperty("Instances")?.GetMethod?.Invoke(dcp,null) is not IEnumerable<object> instances) 
            yield break;
        foreach (var instance in instances)
        {
            if (instance.GetType().GetProperty("Name")?.GetMethod?.Invoke(instance, null) is string name)
                yield return name;
        }
    }
}