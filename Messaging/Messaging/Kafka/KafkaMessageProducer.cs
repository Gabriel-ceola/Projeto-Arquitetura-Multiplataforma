using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Kafka;

public sealed class KafkaMessageProducer : IMessageProducer, IDisposable
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(10);

    private readonly Lazy<IProducer<string?, string>> _producer;

    public KafkaMessageProducer(IOptions<KafkaOptions> options, ILogger<KafkaMessageProducer> logger)
    {
        var settings = options.Value;
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = settings.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 15000,
            Partitioner = Partitioner.Murmur2Random
        };

        _producer = new Lazy<IProducer<string?, string>>(() =>
            new ProducerBuilder<string?, string>(config)
                .SetErrorHandler((_, error) => logger.LogWarning("Kafka producer: {Reason}", error.Reason))
                .SetLogHandler((_, message) => logger.LogDebug("Kafka producer: {Message}", message.Message))
                .Build());
    }

    public async Task ProduceAsync(
        string topic,
        string? key,
        string value,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var message = new Message<string?, string> { Key = key, Value = value };

        if (headers is { Count: > 0 })
        {
            message.Headers = new Headers();
            foreach (var (name, headerValue) in headers)
                message.Headers.Add(name, Encoding.UTF8.GetBytes(headerValue));
        }

        await _producer.Value.ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        if (!_producer.IsValueCreated)
            return;

        _producer.Value.Flush(FlushTimeout);
        _producer.Value.Dispose();
    }
}
