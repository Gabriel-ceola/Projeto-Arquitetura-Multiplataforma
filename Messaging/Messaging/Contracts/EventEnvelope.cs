using System.Text.Json;

namespace Messaging.Contracts;

public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTime OccurredAtUtc,
    string Source,
    string CorrelationId,
    JsonElement Data)
{
    public static EventEnvelope Create<TEvent>(TEvent @event, Guid eventId, DateTime occurredAtUtc, string source, string correlationId)
        where TEvent : IIntegrationEvent =>
        new(eventId, TEvent.EventType, TEvent.EventVersion, occurredAtUtc, source, correlationId,
            JsonSerializer.SerializeToElement(@event, MessagingJson.Options));

    public static EventEnvelope? FromJson(string json) =>
        JsonSerializer.Deserialize<EventEnvelope>(json, MessagingJson.Options);

    public string ToJson() => JsonSerializer.Serialize(this, MessagingJson.Options);

    public TEvent? DataAs<TEvent>() where TEvent : IIntegrationEvent =>
        Data.Deserialize<TEvent>(MessagingJson.Options);
}
