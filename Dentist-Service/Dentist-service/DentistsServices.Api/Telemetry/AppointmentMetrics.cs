using System.Diagnostics.Metrics;

namespace Dentist_service.DentistsServices.Api.Telemetry;

/// <summary>
/// Métricas de negócio do fluxo de agendamento. Cobre todos os caminhos possíveis do
/// <c>AppointmentsController</c>: criação, confirmação, rejeição, cancelamento,
/// disponibilidade de agenda e atualização de expediente/exceções.
/// </summary>
public sealed class AppointmentMetrics : IDisposable
{
    public const string MeterName = "DentistService.Appointments";

    // Resultados
    public const string ResultCreated = "created";
    public const string ResultRejected = "rejected";
    public const string ResultError = "error";
    public const string ResultOk = "ok";

    // Motivos (alta granularidade de casos)
    public const string ReasonNone = "none";
    public const string ReasonMissingIdentity = "missing_identity";
    public const string ReasonPatientMismatch = "patient_mismatch";
    public const string ReasonInvalidType = "invalid_type";
    public const string ReasonInsuranceRequired = "insurance_required";
    public const string ReasonInsuranceNotAllowed = "insurance_not_allowed";
    public const string ReasonServiceUnavailable = "service_unavailable";
    public const string ReasonPatientRequired = "patient_required";
    public const string ReasonForbiddenService = "forbidden_service";
    public const string ReasonInvalidStartAt = "invalid_start_at";
    public const string ReasonOutOfHorizon = "out_of_horizon";
    public const string ReasonOutsideHours = "outside_hours";
    public const string ReasonSlotTaken = "slot_taken";
    public const string ReasonSerializationConflict = "serialization_conflict";
    public const string ReasonNotFound = "not_found";
    public const string ReasonInvalidState = "invalid_state";
    public const string ReasonCancellationWindow = "cancellation_window";
    public const string ReasonForbidden = "forbidden";
    public const string ReasonInvalid = "invalid";
    public const string ReasonModelValidation = "model_validation";
    public const string ReasonUnexpected = "unexpected";

    private readonly Meter _meter;

    private readonly Counter<long> _creation;
    private readonly Counter<long> _confirmation;
    private readonly Counter<long> _rejection;
    private readonly Counter<long> _cancellation;
    private readonly Counter<long> _availability;
    private readonly Counter<long> _scheduleUpdate;
    private readonly Counter<long> _listing;
    private readonly Gauge<int> _active;

    public AppointmentMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _creation = _meter.CreateCounter<long>(
            "appointment.creation",
            unit: "{appointment}",
            description: "Tentativas de criação de agendamento, por resultado, motivo e tipo.");

        _confirmation = _meter.CreateCounter<long>(
            "appointment.confirmation",
            unit: "{appointment}",
            description: "Tentativas de confirmação de agendamento, por resultado e motivo.");

        _rejection = _meter.CreateCounter<long>(
            "appointment.rejection",
            unit: "{appointment}",
            description: "Tentativas de rejeição de agendamento, por resultado e motivo.");

        _cancellation = _meter.CreateCounter<long>(
            "appointment.cancellation",
            unit: "{appointment}",
            description: "Tentativas de cancelamento de agendamento, por resultado, motivo e autor.");

        _availability = _meter.CreateCounter<long>(
            "appointment.availability",
            unit: "{request}",
            description: "Consultas de disponibilidade, por resultado.");

        _scheduleUpdate = _meter.CreateCounter<long>(
            "appointment.schedule.update",
            unit: "{request}",
            description: "Atualizações de agenda (expediente semanal / exceções), por tipo e resultado.");

        _listing = _meter.CreateCounter<long>(
            "appointment.list",
            unit: "{request}",
            description: "Listagens de agendamentos, por escopo e resultado.");

        _active = _meter.CreateGauge<int>(
            "appointment.active",
            unit: "{appointment}",
            description: "Agendamentos no banco por status (pending/confirmed/rejected/cancelled/total).");
    }

    public void CreationCreated(string type) =>
        _creation.Add(1, Tag("result", ResultCreated), Tag("reason", ReasonNone), Tag("type", type));

    public void CreationRejected(string reason, string type = "unknown") =>
        _creation.Add(1, Tag("result", ResultRejected), Tag("reason", reason), Tag("type", type));

    public void CreationError(string reason = ReasonUnexpected) =>
        _creation.Add(1, Tag("result", ResultError), Tag("reason", reason), Tag("type", "unknown"));

    public void Confirmation(string result, string reason) =>
        _confirmation.Add(1, Tag("result", result), Tag("reason", reason));

    public void Rejection(string result, string reason) =>
        _rejection.Add(1, Tag("result", result), Tag("reason", reason));

    public void Cancellation(string result, string reason, string actor) =>
        _cancellation.Add(1, Tag("result", result), Tag("reason", reason), Tag("actor", actor));

    public void Availability(string result) =>
        _availability.Add(1, Tag("result", result));

    public void ScheduleUpdate(string kind, string result) =>
        _scheduleUpdate.Add(1, Tag("kind", kind), Tag("result", result));

    public void Listing(string scope, string result) =>
        _listing.Add(1, Tag("scope", scope), Tag("result", result));

    public void SetActive(string status, int count) =>
        _active.Record(count, Tag("status", status));

    private static KeyValuePair<string, object?> Tag(string name, object? value) => new(name, value);

    public void Dispose() => _meter.Dispose();
}
