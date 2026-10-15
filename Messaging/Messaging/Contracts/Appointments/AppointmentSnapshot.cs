namespace Messaging.Contracts.Appointments;

public sealed record AppointmentSnapshot(
    Guid AppointmentId,
    Guid DentistId,
    Guid PatientId,
    Guid ServiceId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Type,
    string Status,
    int PriceCents,
    string? InsuranceName);
