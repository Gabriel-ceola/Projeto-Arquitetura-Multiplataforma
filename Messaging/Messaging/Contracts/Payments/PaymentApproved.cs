using System.Text.Json.Serialization;

namespace Messaging.Contracts.Payments;

public sealed record PaymentApproved(Guid PaymentId, Guid AppointmentId, int AmountCents, DateTime ApprovedAtUtc) : IIntegrationEvent
{
    public static string Topic => Topics.Payments;
    public static string EventType => nameof(PaymentApproved);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => AppointmentId.ToString();
}
