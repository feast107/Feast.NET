using Confluent.Kafka;

namespace Feast.Aspire.DbService.Endpoints;

partial class Endpoints
{
    public static TEndpointBuilder MapControlEndpoints<TEndpointBuilder>(this TEndpointBuilder endpoints)
    where TEndpointBuilder : IEndpointRouteBuilder, IEndpointConventionBuilder
    {
        var producer = endpoints.ServiceProvider.GetRequiredService<KafkaHostedProducer>();
        
        endpoints.MapGet("/", () => States.AsJsonResult());

        endpoints.MapPut("reject", () =>
        {
            var changed = States[State.Reject] = !States[State.Reject];
            return changed.AsJsonResult();
        });

        endpoints.MapPut("timeout", async () =>
        {
            var changed = States[State.Timeout] = !States[State.Timeout];
            await timeoutChanges.CancelAsync();
            timeoutChanges = new();
            return changed.AsJsonResult();
        });

        endpoints.MapDelete("shut/{code:int}", async (HttpContext context, int? code = 0) =>
        {
            await Results.Ok("bye").ExecuteAsync(context);
            await context.Response.CompleteAsync();
            Environment.Exit(code ?? 0);
            return Results.Ok();
        });

        endpoints.AddEndpointFilter(async (context, next) =>
        {
            var execution = next(context);
            await producer.Produce(context.HttpContext);
            return await execution;
        });
        
        return endpoints;
    }
}