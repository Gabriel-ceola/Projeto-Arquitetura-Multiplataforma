using Messaging.Contracts;

namespace Messaging.Outbox;

public interface IOutbox
{
    void Enqueue<TEvent>(TEvent @event, string? correlationId = null) where TEvent : IIntegrationEvent;
}
