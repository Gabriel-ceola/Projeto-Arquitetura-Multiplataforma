using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Kafka;
using Messaging.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Consumers;

public sealed class EventDispatcher
{
    public const string OriginalTopicHeader = "dlq-original-topic";
    public const string OriginalPartitionHeader = "dlq-original-partition";
    public const string OriginalOffsetHeader = "dlq-original-offset";
    public const string ConsumerGroupHeader = "dlq-consumer-group";
    public const string ErrorHeader = "dlq-error";

    private const string DeadLetterInvalidEnvelope = "invalid_envelope";
    private const string DeadLetterInvalidJson = "invalid_json";
    private const string DeadLetterHandlerFailed = "handler_failed";

    private readonly EventHandlerRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageProducer _producer;
    private readonly KafkaConsumerOptions _options;
    private readonly MessagingMetrics _metrics;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(
        EventHandlerRegistry registry,
        IServiceScopeFactory scopeFactory,
        IMessageProducer producer,
        IOptions<KafkaOptions> options,
        MessagingMetrics metrics,
        ILogger<EventDispatcher> logger)
    {
        _registry = registry;
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value.Consumer;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<DispatchResult> DispatchAsync(ConsumedMessage message, CancellationToken cancellationToken = default)
    {
        using var kafkaScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["KafkaTopic"] = message.Topic,
            ["KafkaPartition"] = message.Partition,
            ["KafkaOffset"] = message.Offset
        });
        var startedAt = Stopwatch.GetTimestamp();

        var envelope = TryReadEnvelope(message, out var readError);
        if (envelope is null)
        {
            await DeadLetterAsync(message, readError, DeadLetterInvalidEnvelope, cancellationToken);
            Record(message.Topic, "deadlettered", startedAt);
            return DispatchResult.DeadLettered;
        }

        // O correlationId do envelope acompanha todos os logs do handler (ex.: logs do caso de uso).
        using var eventScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["EventType"] = envelope.EventType,
            ["MessageId"] = envelope.EventId
        });

        _logger.ConsumerReceived(envelope.EventType, envelope.EventId, message.Topic, message.Partition, message.Offset);

        if (!_registry.TryGet(envelope.EventType, out var registration))
        {
            _logger.ConsumerSkipped(envelope.EventType, envelope.EventId, message.Topic);
            Record(message.Topic, "skipped", startedAt);
            return DispatchResult.Skipped;
        }

        var metadata = new EventMetadata(
            envelope.EventId,
            envelope.EventType,
            envelope.EventVersion,
            envelope.OccurredAtUtc,
            envelope.Source,
            envelope.CorrelationId,
            message.Topic,
            message.Partition,
            message.Offset,
            message.Key);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await registration.InvokeAsync(scope.ServiceProvider, envelope.Data, metadata, cancellationToken);
                _logger.ConsumerHandled(envelope.EventType, envelope.EventId, attempt);
                Record(message.Topic, "handled", startedAt);
                return DispatchResult.Handled;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (JsonException exception)
            {
                await DeadLetterAsync(message, exception.Message, DeadLetterInvalidJson, cancellationToken);
                Record(message.Topic, "deadlettered", startedAt);
                return DispatchResult.DeadLettered;
            }
            catch (Exception exception) when (attempt > _options.MaxRetries)
            {
                _logger.ConsumerFailed(exception, envelope.EventType, envelope.EventId, attempt);
                await DeadLetterAsync(message, exception.Message, DeadLetterHandlerFailed, cancellationToken);
                Record(message.Topic, "deadlettered", startedAt);
                return DispatchResult.DeadLettered;
            }
            catch (Exception exception)
            {
                _logger.ConsumerRetry(exception, attempt, envelope.EventType, envelope.EventId);
                _metrics.ConsumerRetry(message.Topic, envelope.EventType);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs), cancellationToken);
            }
        }
    }

    private void Record(string topic, string result, long startedAt)
    {
        _metrics.ConsumerMessage(topic, result);
        _metrics.ConsumerDuration(topic, Stopwatch.GetElapsedTime(startedAt).TotalSeconds);
    }

    private static EventEnvelope? TryReadEnvelope(ConsumedMessage message, out string error)
    {
        error = "Envelope inválido.";
        if (string.IsNullOrWhiteSpace(message.Value))
        {
            error = "Mensagem sem conteúdo.";
            return null;
        }

        try
        {
            var envelope = EventEnvelope.FromJson(message.Value);
            return envelope is { EventType.Length: > 0, Data.ValueKind: JsonValueKind.Object } ? envelope : null;
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }
    }

    private async Task DeadLetterAsync(ConsumedMessage message, string error, string reason, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>
        {
            [OriginalTopicHeader] = message.Topic,
            [OriginalPartitionHeader] = message.Partition.ToString(CultureInfo.InvariantCulture),
            [OriginalOffsetHeader] = message.Offset.ToString(CultureInfo.InvariantCulture),
            [ConsumerGroupHeader] = _options.GroupId,
            [ErrorHeader] = error
        };

        await _producer.ProduceAsync(Topics.DeadLetterOf(message.Topic), message.Key, message.Value ?? string.Empty, headers, cancellationToken);
        _metrics.ConsumerDeadLetter(message.Topic, reason);
        _logger.ConsumerDeadLettered(message.Topic, message.Partition, message.Offset, error);
    }
}