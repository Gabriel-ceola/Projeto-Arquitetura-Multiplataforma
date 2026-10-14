using System.Text.Json.Serialization;

namespace Messaging.Contracts.HealthInsurance;

public sealed record InsuranceDenied(Guid AuthorizationId, Guid AppointmentId, string Reason) : IIntegrationEvent
{
    public static string Topic => Topics.HealthInsurance;
    public static string EventType => nameof(InsuranceDenied);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => AppointmentId.ToString();
}
