using System.Text.Json.Serialization;

namespace Messaging.Contracts.Appointments;

public sealed record AppointmentCancelled(AppointmentSnapshot Appointment, CancelledBy CancelledBy) : IIntegrationEvent
{
    public static string Topic => Topics.Appointments;
    public static string EventType => nameof(AppointmentCancelled);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => Appointment.AppointmentId.ToString();
}
