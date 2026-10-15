namespace Messaging.Contracts;

public sealed record EventMetadata(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTime OccurredAtUtc,
    string Source,
    string CorrelationId,
    string Topic,
    int Partition,
    long Offset,
    string? Key);
