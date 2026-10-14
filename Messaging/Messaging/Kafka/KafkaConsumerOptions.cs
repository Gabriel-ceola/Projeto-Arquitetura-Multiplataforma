namespace Messaging.Kafka;

public sealed class KafkaConsumerOptions
{
    public string GroupId { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = [];
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
