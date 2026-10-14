using System.Text.Json;
using FluentAssertions;
using Messaging.Contracts;
using Messaging.Contracts.Appointments;
using Messaging.Tests.Helpers;

namespace Messaging.Tests.Contracts;

public class EventEnvelopeTests
{
    [Fact]
    public void Create_WhenEventIsGiven_ShouldFillMetadataFromContract()
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

        var envelope = EventEnvelope.Create(new AppointmentRequested(EventsFactory.Snapshot()), eventId, occurredAtUtc, "dentist-service", "corr-1");

        envelope.EventId.Should().Be(eventId);
        envelope.EventType.Should().Be("AppointmentRequested");
        envelope.EventVersion.Should().Be(1);
        envelope.OccurredAtUtc.Should().Be(occurredAtUtc);
        envelope.Source.Should().Be("dentist-service");
        envelope.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void Create_WhenEventHasKey_ShouldNotSerializeKeyIntoData()
    {
        var envelope = EventEnvelope.Create(new AppointmentRequested(EventsFactory.Snapshot()), Guid.NewGuid(), DateTime.UtcNow, "tests", "corr");

        envelope.Data.TryGetProperty("key", out _).Should().BeFalse();
        envelope.Data.GetProperty("appointment").GetProperty("appointmentId").GetGuid().Should().Be(EventsFactory.DefaultAppointmentId);
    }

    [Fact]
    public void Create_WhenEventHasEnum_ShouldSerializeEnumAsString()
    {
        var @event = new AppointmentCancelled(EventsFactory.Snapshot(status: "Cancelled"), CancelledBy.Patient);

        var envelope = EventEnvelope.Create(@event, Guid.NewGuid(), DateTime.UtcNow, "tests", "corr");

        envelope.Data.GetProperty("cancelledBy").ValueKind.Should().Be(JsonValueKind.String);
        envelope.Data.GetProperty("cancelledBy").GetString().Should().Be("Patient");
    }

    [Fact]
    public void FromJson_WhenEnvelopeWasSerialized_ShouldRoundTripEnvelopeAndData()
    {
        var @event = new AppointmentCancelled(EventsFactory.Snapshot(status: "Cancelled"), CancelledBy.Dentist);
        var original = EventEnvelope.Create(@event, Guid.NewGuid(), DateTime.UtcNow, "tests", "corr");

        var restored = EventEnvelope.FromJson(original.ToJson());

        restored.Should().NotBeNull();
        restored!.EventId.Should().Be(original.EventId);
        restored.EventType.Should().Be(original.EventType);
        restored.CorrelationId.Should().Be(original.CorrelationId);
        restored.DataAs<AppointmentCancelled>().Should().Be(@event);
    }

    [Fact]
    public void ToJson_WhenSerialized_ShouldUseCamelCaseEnvelopeFields()
    {
        var json = EventsFactory.EnvelopeJson(new AppointmentRequested(EventsFactory.Snapshot()));

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("eventId", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("eventType", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("eventVersion", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("occurredAtUtc", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("data", out _).Should().BeTrue();
    }
}
