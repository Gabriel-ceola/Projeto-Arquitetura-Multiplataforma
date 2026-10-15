using Messaging.Consumers;
using Messaging.Contracts;
using Messaging.Contracts.Appointments;

namespace Messaging.Tests.Helpers;

public static class EventsFactory
{
    public static readonly Guid DefaultAppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static AppointmentSnapshot Snapshot(Guid? appointmentId = null, string status = "Pending") => new(
        appointmentId ?? DefaultAppointmentId,
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        new DateTime(2026, 10, 20, 13, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 10, 20, 13, 30, 0, DateTimeKind.Utc),
        "Private",
        status,
        15000,
        null);

    public static string EnvelopeJson<TEvent>(TEvent @event, string correlationId = "correlation-test") where TEvent : IIntegrationEvent =>
        EventEnvelope.Create(@event, Guid.NewGuid(), DateTime.UtcNow, "tests", correlationId).ToJson();

    public static ConsumedMessage Message(string? value, string topic = Topics.Payments, string? key = "key-1", long offset = 7) =>
        new(topic, 0, offset, key, value);
}
