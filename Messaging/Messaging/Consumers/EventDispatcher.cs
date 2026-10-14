using System.Globalization;
using System.Text.Json;
using Messaging.Contracts;
using Messaging.Kafka;
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

    private readonly EventHandlerRegistry _registry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageProducer _producer;
    private readonly KafkaConsumerOptions _options;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(
        EventHandlerRegistry registry,
        IServiceScopeFactory scopeFactory,
        IMessageProducer producer,
        IOptions<KafkaOptions> options,
        ILogger<EventDispatcher> logger)
    {
        _registry = registry;
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value.Consumer;
        _logger = logger;
    }

    public async Task<DispatchResult> DispatchAsync(ConsumedMessage message, CancellationToken cancellationToken = default)
    {
        var envelope = TryReadEnvelope(message, out var readError);
        if (envelope is null)
        {
            await DeadLetterAsync(message, readError, cancellationToken);
            return DispatchResult.DeadLettered;
        }

        if (!_registry.TryGet(envelope.EventType, out var registration))
        {
            _logger.LogInformation("Evento {EventType} ({EventId}) recebido em {Topic} sem handler registrado.",
                envelope.EventType, envelope.EventId, message.Topic);
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
                return DispatchResult.Handled;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (JsonException exception)
            {
                await DeadLetterAsync(message, exception.Message, cancellationToken);
                return DispatchResult.DeadLettered;
            }
            catch (Exception exception) when (attempt > _options.MaxRetries)
            {
                _logger.LogError(exception, "Evento {EventType} ({EventId}) falhou após {Attempts} tentativas.",
                    envelope.EventType, envelope.EventId, attempt);
                await DeadLetterAsync(message, exception.Message, cancellationToken);
                return DispatchResult.DeadLettered;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Tentativa {Attempt} do evento {EventType} ({EventId}) falhou.",
                    attempt, envelope.EventType, envelope.EventId);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs), cancellationToken);
            }
        }
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

    private async Task DeadLetterAsync(ConsumedMessage message, string error, CancellationToken cancellationToken)
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
        _logger.LogWarning("Mensagem {Topic}/{Partition}@{Offset} enviada para a DLQ: {Error}",
            message.Topic, message.Partition, message.Offset, error);
    }
}
