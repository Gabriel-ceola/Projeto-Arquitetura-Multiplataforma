using FluentAssertions;
using Messaging.Contracts;
using Messaging.Contracts.Appointments;
using Messaging.Kafka;
using Messaging.Outbox;
using Messaging.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Messaging.Tests.Outbox;

public class EfOutboxTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly HttpContextAccessor _httpContextAccessor = new();
    private readonly IOptions<KafkaOptions> _options = Options.Create(new KafkaOptions { ClientId = "dentist-service" });

    private EfOutbox<TestDbContext> CreateOutbox(TestDbContext context) => new(context, _httpContextAccessor, _options);

    [Fact]
    public void Enqueue_WhenCallerDoesNotSave_ShouldNotPersistMessage()
    {
        using var context = TestDbContext.Create(_databaseName);

        CreateOutbox(context).Enqueue(new AppointmentRequested(EventsFactory.Snapshot()));

        using var otherContext = TestDbContext.Create(_databaseName);
        otherContext.OutboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Enqueue_WhenCallerSaves_ShouldPersistMessageWithTopicKeyTypeAndEnvelope()
    {
        await using (var context = TestDbContext.Create(_databaseName))
        {
            CreateOutbox(context).Enqueue(new AppointmentConfirmed(EventsFactory.Snapshot(status: "Confirmed")));
            await context.SaveChangesAsync();
        }

        await using var readContext = TestDbContext.Create(_databaseName);
        var message = readContext.OutboxMessages.Single();
        message.Topic.Should().Be(Topics.Appointments);
        message.Key.Should().Be(EventsFactory.DefaultAppointmentId.ToString());
        message.EventType.Should().Be("AppointmentConfirmed");
        message.ProcessedAtUtc.Should().BeNull();
        message.Attempts.Should().Be(0);

        var envelope = EventEnvelope.FromJson(message.Payload)!;
        envelope.EventId.Should().Be(message.Id);
        envelope.Source.Should().Be("dentist-service");
        envelope.DataAs<AppointmentConfirmed>()!.Appointment.Status.Should().Be("Confirmed");
    }

    [Fact]
    public void Enqueue_WhenCorrelationIdIsGiven_ShouldUseIt()
    {
        using var context = TestDbContext.Create(_databaseName);
        _httpContextAccessor.HttpContext = ContextWithCorrelationHeader("from-header");

        CreateOutbox(context).Enqueue(new AppointmentRequested(EventsFactory.Snapshot()), "explicit");

        CorrelationIdOf(context.OutboxMessages.Local.Single()).Should().Be("explicit");
    }

    [Fact]
    public void Enqueue_WhenRequestHasCorrelationHeader_ShouldUseHeader()
    {
        using var context = TestDbContext.Create(_databaseName);
        _httpContextAccessor.HttpContext = ContextWithCorrelationHeader("from-header");

        CreateOutbox(context).Enqueue(new AppointmentRequested(EventsFactory.Snapshot()));

        CorrelationIdOf(context.OutboxMessages.Local.Single()).Should().Be("from-header");
    }

    [Fact]
    public void Enqueue_WhenNoCorrelationIsAvailable_ShouldFallBackToEventId()
    {
        using var context = TestDbContext.Create(_databaseName);

        CreateOutbox(context).Enqueue(new AppointmentRequested(EventsFactory.Snapshot()));

        var message = context.OutboxMessages.Local.Single();
        CorrelationIdOf(message).Should().Be(message.Id.ToString());
    }

    private static DefaultHttpContext ContextWithCorrelationHeader(string value)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = value;
        return httpContext;
    }

    private static string CorrelationIdOf(OutboxMessage message) => EventEnvelope.FromJson(message.Payload)!.CorrelationId;
}
