using Microsoft.Extensions.Logging;

namespace Dentist_service.DentistsServices.Api.Logging;

/// <summary>
/// Logs de negócio do agendamento (ver elk/docs/logging-standard.md).
/// Nunca incluir dados pessoais: apenas identificadores (UUID), datas, tipo e motivo.
/// </summary>
internal static partial class AppointmentsLog
{
    [LoggerMessage(EventId = 3001, EventName = "appointment.requested", Level = LogLevel.Information,
        Message = "Agendamento {AppointmentId} solicitado por {Actor} para {StartAtUtc:O} (dentista {DentistId}, paciente {PatientId}, serviço {ServiceId}, tipo {AppointmentType}).")]
    public static partial void AppointmentRequested(this ILogger logger, Guid appointmentId, Guid dentistId, Guid patientId, Guid serviceId, DateTime startAtUtc, string appointmentType, string actor);

    [LoggerMessage(EventId = 3002, EventName = "appointment.confirmed", Level = LogLevel.Information,
        Message = "Agendamento {AppointmentId} confirmado pelo dentista {DentistId} (paciente {PatientId}, início {StartAtUtc:O}).")]
    public static partial void AppointmentConfirmed(this ILogger logger, Guid appointmentId, Guid dentistId, Guid patientId, DateTime startAtUtc);

    [LoggerMessage(EventId = 3003, EventName = "appointment.rejected", Level = LogLevel.Information,
        Message = "Agendamento {AppointmentId} rejeitado pelo dentista {DentistId} (paciente {PatientId}, início {StartAtUtc:O}).")]
    public static partial void AppointmentRejected(this ILogger logger, Guid appointmentId, Guid dentistId, Guid patientId, DateTime startAtUtc);

    [LoggerMessage(EventId = 3004, EventName = "appointment.cancelled", Level = LogLevel.Information,
        Message = "Agendamento {AppointmentId} cancelado por {Actor} (dentista {DentistId}, paciente {PatientId}, início {StartAtUtc:O}).")]
    public static partial void AppointmentCancelled(this ILogger logger, Guid appointmentId, Guid dentistId, Guid patientId, DateTime startAtUtc, string actor);

    [LoggerMessage(EventId = 3005, EventName = "appointment.slot_conflict", Level = LogLevel.Warning,
        Message = "Conflito de horário para o dentista {DentistId} em {StartAtUtc:O}: {Reason} (agendamento {AppointmentId}, serviço {ServiceId}).")]
    public static partial void AppointmentSlotConflict(this ILogger logger, Guid dentistId, DateTime startAtUtc, string reason, Guid? appointmentId, Guid? serviceId);

    [LoggerMessage(EventId = 3006, EventName = "appointment.denied", Level = LogLevel.Warning,
        Message = "Operação de agendamento negada: {Reason} (agendamento {AppointmentId}, dentista {DentistId}).")]
    public static partial void AppointmentDenied(this ILogger logger, string reason, Guid? appointmentId, Guid? dentistId);

    [LoggerMessage(EventId = 3101, EventName = "schedule.weekly_updated", Level = LogLevel.Information,
        Message = "Expediente semanal do dentista {DentistId} atualizado ({DaysCount} dias).")]
    public static partial void WeeklyScheduleUpdated(this ILogger logger, Guid dentistId, int daysCount);

    [LoggerMessage(EventId = 3102, EventName = "schedule.exception_updated", Level = LogLevel.Information,
        Message = "Exceção de agenda do dentista {DentistId} em {ExceptionDate:yyyy-MM-dd} atualizada (fechado: {IsClosed}).")]
    public static partial void ScheduleExceptionUpdated(this ILogger logger, Guid dentistId, DateOnly exceptionDate, bool isClosed);

    [LoggerMessage(EventId = 3103, EventName = "availability.queried", Level = LogLevel.Debug,
        Message = "Disponibilidade do dentista {DentistId} consultada para o serviço {ServiceId}: {SlotCount} horários.")]
    public static partial void AvailabilityQueried(this ILogger logger, Guid dentistId, Guid serviceId, int slotCount);
}
