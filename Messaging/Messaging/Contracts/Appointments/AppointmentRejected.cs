using System.Text.Json.Serialization;

namespace Messaging.Contracts.Appointments;

public sealed record AppointmentRejected(AppointmentSnapshot Appointment) : IIntegrationEvent
{
    public static string Topic => Topics.Appointments;
    public static string EventType => nameof(AppointmentRejected);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => Appointment.AppointmentId.ToString();
}
