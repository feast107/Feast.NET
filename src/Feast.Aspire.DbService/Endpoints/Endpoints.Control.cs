namespace Feast.Aspire.DbService.Endpoints;

partial class Endpoints
{
    public static RouteGroupBuilder MapControlEndpoints(this RouteGroupBuilder endpoints)
    {
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
        
        return endpoints;
    }
}