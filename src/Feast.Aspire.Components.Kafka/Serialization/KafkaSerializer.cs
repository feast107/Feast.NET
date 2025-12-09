using Confluent.Kafka;

namespace Feast.Aspire.Components.Kafka.Serialization;

public abstract class KafkaSerializer<T, TSerializer> : ISerializer<T>, IDeserializer<T> where TSerializer : KafkaSerializer<T, TSerializer>, new()
{
    public abstract byte[] Serialize(T data, SerializationContext context);
    public abstract T      Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context);

    public static TSerializer Instance { get; } = new();
}