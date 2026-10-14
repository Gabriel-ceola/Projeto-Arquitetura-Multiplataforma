namespace Messaging.Kafka;

public interface IMessageProducer
{
    Task ProduceAsync(
        string topic,
        string? key,
        string value,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}
