namespace Messaging.Kafka;

public sealed class KafkaOutboxOptions
{
    public int PollingIntervalMs { get; set; } = 1000;
    public int BatchSize { get; set; } = 50;
}
