using System.Diagnostics.Metrics;

namespace PaymentService.Infrastructure.Telemetry;

/// <summary>
/// Métricas de negócio do fluxo de pagamento: criação, transições de status
/// (Pending/Processing/Paid/Failed/Cancelled), leituras e valor monetário.
/// </summary>
public sealed class PaymentMetrics : IDisposable
{
    public const string MeterName = "PaymentService.Payments";

    public const string ResultCreated = "created";
    public const string ResultOk = "ok";
    public const string ResultRejected = "rejected";
    public const string ResultNotFound = "not_found";

    public const string ReasonNone = "none";
    public const string ReasonInvalidAppointment = "invalid_appointment";
    public const string ReasonInvalidAmount = "invalid_amount";
    public const string ReasonMissingStatus = "missing_status";
    public const string ReasonNotFound = "not_found";
    public const string ReasonModelValidation = "model_validation";

    private readonly Meter _meter;

    private readonly Counter<long> _creation;
    private readonly Counter<long> _statusUpdate;
    private readonly Counter<long> _reads;
    private readonly Histogram<double> _amount;
    private readonly Gauge<int> _active;

    public PaymentMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _creation = _meter.CreateCounter<long>(
            "payment.creation",
            unit: "{payment}",
            description: "Tentativas de criação de pagamento, por resultado e motivo.");

        _statusUpdate = _meter.CreateCounter<long>(
            "payment.status.update",
            unit: "{payment}",
            description: "Tentativas de atualização de status, por resultado e transição.");

        _reads = _meter.CreateCounter<long>(
            "payment.read",
            unit: "{request}",
            description: "Leituras de pagamento (listagem/por id), por resultado.");

        _amount = _meter.CreateHistogram<double>(
            "payment.amount",
            description: "Valor dos pagamentos criados (BRL).");

        _active = _meter.CreateGauge<int>(
            "payment.active",
            unit: "{payment}",
            description: "Pagamentos no banco por status (pending/processing/paid/failed/cancelled/total).");
    }

    public void CreationCreated(decimal amount) =>
        _creation.Add(1, Tag("result", ResultCreated), Tag("reason", ReasonNone));

    public void CreationRejected(string reason) =>
        _creation.Add(1, Tag("result", ResultRejected), Tag("reason", reason));

    public void StatusUpdate(string result, string reason, string from, string to) =>
        _statusUpdate.Add(
            1,
            Tag("result", result),
            Tag("reason", reason),
            Tag("from", from),
            Tag("to", to));

    public void Read(string operation, string result) =>
        _reads.Add(1, Tag("operation", operation), Tag("result", result));

    public void RecordAmount(decimal amount) => _amount.Record((double)amount);

    public void SetActive(string status, int count) =>
        _active.Record(count, Tag("status", status));

    private static KeyValuePair<string, object?> Tag(string name, object? value) => new(name, value);

    public void Dispose() => _meter.Dispose();
}
