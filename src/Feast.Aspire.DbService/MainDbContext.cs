using Microsoft.EntityFrameworkCore;

namespace Feast.Aspire.DbService;

public class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options), IHostedService
{
    public required DbSet<SampleEntity> SampleEntities { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken) => await Database.MigrateAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}