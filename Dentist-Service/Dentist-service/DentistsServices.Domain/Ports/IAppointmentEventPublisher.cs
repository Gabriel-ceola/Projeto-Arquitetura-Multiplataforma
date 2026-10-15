namespace Dentist_service.DentistsServices.Domain.Ports;

public interface IAppointmentEventPublisher
{
    void Requested(Appointment appointment);
    void Confirmed(Appointment appointment);
    void Rejected(Appointment appointment);
    void Cancelled(Appointment appointment, AppointmentActor cancelledBy);
}
