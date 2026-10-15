namespace Messaging.Outbox;

public class OutboxMessage
{
    public const int LastErrorMaxLength = 2000;

    public Guid Id { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, string topic, string key, string eventType, string payload, DateTime occurredAtUtc)
    {
        Id = id;
        Topic = topic;
        Key = key;
        EventType = eventType;
        Payload = payload;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
    }

    public void MarkAsProcessed(DateTime processedAtUtc)
    {
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);
        LastError = null;
    }

    public void RegisterFailure(string error)
    {
        Attempts++;
        LastError = error.Length > LastErrorMaxLength ? error[..LastErrorMaxLength] : error;
    }
}
