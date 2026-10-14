using System.Text.Json.Serialization;
using Messaging.Contracts;

namespace Messaging.Tests.Helpers;

public sealed record SampleEvent(Guid Id, string Name) : IIntegrationEvent
{
    public static string Topic => Topics.Payments;
    public static string EventType => nameof(SampleEvent);
    public static int EventVersion => 1;

    [JsonIgnore]
    public string Key => Id.ToString();
}
