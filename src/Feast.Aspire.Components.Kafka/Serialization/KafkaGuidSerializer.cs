using Confluent.Kafka;

namespace Feast.Aspire.Components.Kafka.Serialization;

public class KafkaGuidSerializer : KafkaSerializer<Guid, KafkaGuidSerializer>
{
    public override byte[] Serialize(Guid data, SerializationContext context) => data.ToByteArray();
    public override Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? Guid.Empty : new Guid(data);
}