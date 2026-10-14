using Dentist_service.DentistsServices.Domain;
using Dentist_service.DentistsServices.Domain.Ports;
using Messaging.Contracts.Appointments;
using Messaging.Outbox;

namespace Dentist_service.DentistsServices.Infrastructure.Events;

public class AppointmentEventPublisher : IAppointmentEventPublisher
{
    private readonly IOutbox _outbox;

    public AppointmentEventPublisher(IOutbox outbox)
    {
        _outbox = outbox;
    }

    public void Requested(Appointment appointment) =>
        _outbox.Enqueue(new AppointmentRequested(ToSnapshot(appointment)));

    public void Confirmed(Appointment appointment) =>
        _outbox.Enqueue(new AppointmentConfirmed(ToSnapshot(appointment)));

    public void Rejected(Appointment appointment) =>
        _outbox.Enqueue(new AppointmentRejected(ToSnapshot(appointment)));

    public void Cancelled(Appointment appointment, AppointmentActor cancelledBy) =>
        _outbox.Enqueue(new AppointmentCancelled(
            ToSnapshot(appointment),
            cancelledBy == AppointmentActor.Dentist ? CancelledBy.Dentist : CancelledBy.Patient));

    private static AppointmentSnapshot ToSnapshot(Appointment appointment) => new(
        appointment.Id,
        appointment.DentistId,
        appointment.PatientId,
        appointment.ServiceId,
        appointment.StartAtUtc,
        appointment.EndAtUtc,
        appointment.Type.ToString(),
        appointment.Status.ToString(),
        appointment.PriceCents,
        appointment.InsuranceName);
}
