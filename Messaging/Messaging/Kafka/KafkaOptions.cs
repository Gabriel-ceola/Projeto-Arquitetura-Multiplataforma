namespace Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public KafkaOutboxOptions Outbox { get; set; } = new();
    public KafkaConsumerOptions Consumer { get; set; } = new();
}
