using System.Diagnostics.Metrics;

namespace Messaging.Telemetry;

/// <summary>
/// Métricas de telemetria da biblioteca de mensageria: outbox (produção) e consumer Kafka
/// (despacho, retry e dead-letter). Publica no <see cref="System.Diagnostics.Metrics.Meter"/>
/// chamado <see cref="MeterName"/>; cada host adiciona esse meter ao exporter escolhido
/// (neste projeto, OpenTelemetry + Prometheus).
/// </summary>
public sealed class MessagingMetrics : IDisposable
{
    public const string MeterName = "Messaging";

    private readonly Meter _meter;

    private readonly Counter<long> _outboxPublished;
    private readonly Counter<long> _outboxPublishFailures;
    private readonly Gauge<long> _outboxPending;
    private readonly Histogram<double> _outboxPublishDuration;

    private readonly Counter<long> _consumerMessages;
    private readonly Counter<long> _consumerRetries;
    private readonly Counter<long> _consumerDeadLetters;
    private readonly Histogram<double> _consumerDuration;
    private readonly Counter<long> _consumerFatalErrors;

    public MessagingMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _outboxPublished = _meter.CreateCounter<long>(
            "messaging.outbox.published",
            unit: "{message}",
            description: "Eventos publicados no Kafka a partir do outbox.");

        _outboxPublishFailures = _meter.CreateCounter<long>(
            "messaging.outbox.publish.failures",
            unit: "{failure}",
            description: "Falhas ao publicar eventos do outbox no Kafka.");

        _outboxPending = _meter.CreateGauge<long>(
            "messaging.outbox.pending",
            unit: "{message}",
            description: "Eventos pendentes no outbox aguardando publicação (backlog).");

        _outboxPublishDuration = _meter.CreateHistogram<double>(
            "messaging.outbox.publish.duration",
            unit: "s",
            description: "Duração da publicação de um lote do outbox.");

        _consumerMessages = _meter.CreateCounter<long>(
            "messaging.consumer.messages",
            unit: "{message}",
            description: "Mensagens consumidas, por resultado de despacho (handled/skipped/deadlettered).");

        _consumerRetries = _meter.CreateCounter<long>(
            "messaging.consumer.retries",
            unit: "{retry}",
            description: "Tentativas de reprocessamento de uma mensagem consumida.");

        _consumerDeadLetters = _meter.CreateCounter<long>(
            "messaging.consumer.dlq",
            unit: "{message}",
            description: "Mensagens enviadas para a dead-letter queue (DLQ).");

        _consumerDuration = _meter.CreateHistogram<double>(
            "messaging.consumer.duration",
            unit: "s",
            description: "Duração do despacho de uma mensagem consumida.");

        _consumerFatalErrors = _meter.CreateCounter<long>(
            "messaging.consumer.fatal.errors",
            unit: "{error}",
            description: "Erros fatais no consumidor Kafka.");
    }

    public void OutboxPublished(string eventType, string topic) =>
        _outboxPublished.Add(
            1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("topic", topic));

    public void OutboxPublishFailed(string topic) =>
        _outboxPublishFailures.Add(1, new KeyValuePair<string, object?>("topic", topic));

    public void OutboxPending(long count) => _outboxPending.Record(count);

    public void OutboxPublishDuration(double seconds) => _outboxPublishDuration.Record(seconds);

    public void ConsumerMessage(string topic, string result) =>
        _consumerMessages.Add(
            1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("result", result));

    public void ConsumerRetry(string topic, string eventType) =>
        _consumerRetries.Add(
            1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("event_type", eventType));

    public void ConsumerDeadLetter(string topic, string reason) =>
        _consumerDeadLetters.Add(
            1,
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("reason", reason));

    public void ConsumerDuration(string topic, double seconds) =>
        _consumerDuration.Record(seconds, new KeyValuePair<string, object?>("topic", topic));

    public void ConsumerFatalError(string reason) =>
        _consumerFatalErrors.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void Dispose() => _meter.Dispose();
}
