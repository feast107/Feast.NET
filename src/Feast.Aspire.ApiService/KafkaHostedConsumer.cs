using Confluent.Kafka;

namespace Feast.Aspire.ApiService;

public class KafkaHostedConsumer(IConsumer<Guid,string> consumer, ILogger<KafkaHostedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var partition = new TopicPartition("db-event", 0);
        consumer.IncrementalAssign([partition]);
        consumer.Subscribe(partition.Topic);
        try
        {
            Consume(stoppingToken);
        }
        catch (ConsumeException)
        {
            // 首次Consume如果不存在topic则会抛出一次异常，将其视为创建成功即可
            if(logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Topic:[{Topic}] created", partition.Topic);
        }
        try
        {
            Consume(stoppingToken);
        }
        catch (ConsumeException)
        {
            // 第二次Consume会申请offset，如果有异常则不管
            if(logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Topic:[{Topic}] offset fetched", partition.Topic);
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            Consume(stoppingToken);
        }
    }

    private void Consume(CancellationToken stoppingToken)
    {
        var message = consumer.Consume(stoppingToken);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Consume kafka message : Key:[{Id}], Message:[{Message}], Timestamp:[{Timestamp}] but not be committed",
                message.Message.Key, message.Message.Value, message.Message.Timestamp.UtcDateTime);
        Task.Delay(2000, stoppingToken).ContinueWith(_ =>
        {
            consumer.Commit([message.TopicPartitionOffset]);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Commit kafka message : Key:[{Id}], Message:[{Message}], Timestamp:[{Timestamp}]",
                    message.Message.Key, message.Message.Value, message.Message.Timestamp.UtcDateTime);
        }, stoppingToken);
    }
}