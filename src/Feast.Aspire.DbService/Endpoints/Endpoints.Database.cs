using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feast.Aspire.DbService.Endpoints;

partial class Endpoints
{
    public static RouteGroupBuilder MapDbEndpoints(this RouteGroupBuilder endpoints)
    {
        endpoints.AddEndpointFilter(async (context, next) =>
        {
            if (States[State.Reject])
            {
                context.HttpContext.Abort();
                return null;
            }
            if (States[State.Timeout])
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, timeoutChanges.Token);
                    return null;
                }
                catch (OperationCanceledException)
                {

                }
            }
            return await next(context);
        });

        endpoints.MapGet("/", async ([FromQuery] int? skip,
            [FromQuery] int? take,
            [FromServices] MainDbContext db) =>
        {
            var query = db.SampleEntities.Skip(skip ?? 0);
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }
            return await query.ToListAsync().AsJsonResult();
        });

        endpoints.MapGet("/{id:int}", async (int id, [FromServices] MainDbContext db) =>
        {
            return await db.SampleEntities.Where(x => x.Id == id).ToListAsync().AsJsonResult();
        });

        endpoints.MapPut("/", async ([FromBody] SampleEntity entity, [FromServices] MainDbContext db) =>
        {
            var track = await db.SampleEntities.AddAsync(entity);
            await db.SaveChangesAsync();
            return track.Entity.Id.AsJsonResult();
        });

        endpoints.MapDelete("/{id:int}", async (int id, [FromServices] MainDbContext db) =>
        {
            return await db.SampleEntities.Where(x => x.Id == id)
                .ExecuteDeleteAsync()
                .AsJsonResult();
        });

        return endpoints;
    }
}