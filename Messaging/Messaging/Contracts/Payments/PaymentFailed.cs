using System.Text.Json.Serialization;

namespace Messaging.Contracts.Payments;

public sealed record PaymentFailed(Guid PaymentId, Guid AppointmentId, string Reason) : IIntegrationEvent
{
    public static string Topic => Topics.Payments;
    public static string EventType => nameof(PaymentFailed);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => AppointmentId.ToString();
}
