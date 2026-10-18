using System.Diagnostics;
using Messaging.Contracts;
using Messaging.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Messaging.Outbox;

public sealed class EfOutbox<TDbContext> : IOutbox where TDbContext : DbContext
{
    public const string CorrelationIdHeader = "x-correlation-id";

    private readonly TDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _source;
    private readonly ILogger _logger;

    public EfOutbox(
        TDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IOptions<KafkaOptions> options,
        ILogger<EfOutbox<TDbContext>>? logger = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _source = options.Value.ClientId;
        _logger = logger ?? NullLogger<EfOutbox<TDbContext>>.Instance;
    }

    public void Enqueue<TEvent>(TEvent @event, string? correlationId = null) where TEvent : IIntegrationEvent
    {
        var eventId = Guid.CreateVersion7();
        var occurredAtUtc = DateTime.UtcNow;
        var envelope = EventEnvelope.Create(@event, eventId, occurredAtUtc, _source, ResolveCorrelationId(correlationId, eventId));

        _context.Set<OutboxMessage>().Add(
            new OutboxMessage(eventId, TEvent.Topic, @event.Key, TEvent.EventType, envelope.ToJson(), occurredAtUtc));

        _logger.OutboxEnqueued(TEvent.EventType, eventId, TEvent.Topic, @event.Key, envelope.CorrelationId);
    }

    private string ResolveCorrelationId(string? correlationId, Guid eventId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
            return correlationId;

        var header = _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString();
        if (!string.IsNullOrWhiteSpace(header))
            return header;

        return Activity.Current?.TraceId.ToString() ?? eventId.ToString();
    }
}
