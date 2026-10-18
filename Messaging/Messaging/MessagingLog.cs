using Microsoft.Extensions.Logging;

namespace Messaging;

/// <summary>
/// Logs estruturados da mensageria. O <c>EventName</c> vira <c>event.action</c> no Elasticsearch
/// (ver elk/docs/logging-standard.md); os nomes dos parâmetros viram <c>labels.*</c>.
/// </summary>
internal static partial class MessagingLog
{
    [LoggerMessage(EventId = 2001, EventName = "outbox.enqueued", Level = LogLevel.Information,
        Message = "Evento {EventType} ({MessageId}) enfileirado no outbox. Tópico {Topic}, chave {MessageKey}, correlação {CorrelationId}.")]
    public static partial void OutboxEnqueued(this ILogger logger, string eventType, Guid messageId, string topic, string messageKey, string correlationId);

    [LoggerMessage(EventId = 2002, EventName = "outbox.published", Level = LogLevel.Information,
        Message = "Evento {EventType} ({MessageId}) publicado no tópico {Topic}, chave {MessageKey}. Atraso {LagMs} ms.")]
    public static partial void OutboxPublished(this ILogger logger, string eventType, Guid messageId, string topic, string messageKey, long lagMs);

    [LoggerMessage(EventId = 2003, EventName = "outbox.publish_failed", Level = LogLevel.Warning,
        Message = "Falha ao publicar o evento {EventType} ({MessageId}) no tópico {Topic}. Tentativa {Attempts}.")]
    public static partial void OutboxPublishFailed(this ILogger logger, Exception exception, string eventType, Guid messageId, string topic, int attempts);

    [LoggerMessage(EventId = 2004, EventName = "outbox.read_failed", Level = LogLevel.Error,
        Message = "Falha ao ler o outbox de {DbContext}.")]
    public static partial void OutboxReadFailed(this ILogger logger, Exception exception, string dbContext);

    [LoggerMessage(EventId = 2101, EventName = "consumer.received", Level = LogLevel.Information,
        Message = "Evento {EventType} ({MessageId}) recebido de {KafkaTopic} [{KafkaPartition}@{KafkaOffset}].")]
    public static partial void ConsumerReceived(this ILogger logger, string eventType, Guid messageId, string kafkaTopic, int kafkaPartition, long kafkaOffset);

    [LoggerMessage(EventId = 2102, EventName = "consumer.handled", Level = LogLevel.Information,
        Message = "Evento {EventType} ({MessageId}) processado na tentativa {Attempt}.")]
    public static partial void ConsumerHandled(this ILogger logger, string eventType, Guid messageId, int attempt);

    [LoggerMessage(EventId = 2103, EventName = "consumer.skipped", Level = LogLevel.Information,
        Message = "Evento {EventType} ({MessageId}) recebido em {KafkaTopic} sem handler registrado.")]
    public static partial void ConsumerSkipped(this ILogger logger, string eventType, Guid messageId, string kafkaTopic);

    [LoggerMessage(EventId = 2104, EventName = "consumer.retry", Level = LogLevel.Warning,
        Message = "Tentativa {Attempt} do evento {EventType} ({MessageId}) falhou.")]
    public static partial void ConsumerRetry(this ILogger logger, Exception exception, int attempt, string eventType, Guid messageId);

    [LoggerMessage(EventId = 2105, EventName = "consumer.failed", Level = LogLevel.Error,
        Message = "Evento {EventType} ({MessageId}) falhou após {Attempt} tentativas.")]
    public static partial void ConsumerFailed(this ILogger logger, Exception exception, string eventType, Guid messageId, int attempt);

    [LoggerMessage(EventId = 2106, EventName = "consumer.dead_lettered", Level = LogLevel.Error,
        Message = "Mensagem {KafkaTopic}/{KafkaPartition}@{KafkaOffset} enviada para a DLQ: {Reason}")]
    public static partial void ConsumerDeadLettered(this ILogger logger, string kafkaTopic, int kafkaPartition, long kafkaOffset, string reason);
}
