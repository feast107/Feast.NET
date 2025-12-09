using System.Threading.Channels;
using Confluent.Kafka;

namespace Feast.Aspire.DbService;

public class KafkaHostedProducer(IProducer<Guid,string> producer, ILogger<KafkaHostedProducer> logger) : BackgroundService
{
    private readonly Channel<Task> channel =  Channel.CreateUnbounded<Task>();

    public ValueTask Produce(HttpContext context) =>
        channel.Writer.WriteAsync(producer.ProduceAsync("db-event", new()
        {
            Key       = Guid.CreateVersion7(),
            Timestamp = new Timestamp(DateTimeOffset.UtcNow),
            Value     = context.Request.Path.ToString()
        }));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await task;
                if (logger.IsEnabled(LogLevel.Information)) 
                    logger.LogInformation("Kafka message produced");
            }
            catch
            {
                //
            }
        }
    }
}