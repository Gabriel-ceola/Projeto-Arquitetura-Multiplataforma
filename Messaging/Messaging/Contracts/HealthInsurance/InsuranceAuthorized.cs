using System.Text.Json.Serialization;

namespace Messaging.Contracts.HealthInsurance;

public sealed record InsuranceAuthorized(Guid AuthorizationId, Guid AppointmentId, string InsuranceName) : IIntegrationEvent
{
    public static string Topic => Topics.HealthInsurance;
    public static string EventType => nameof(InsuranceAuthorized);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => AppointmentId.ToString();
}
