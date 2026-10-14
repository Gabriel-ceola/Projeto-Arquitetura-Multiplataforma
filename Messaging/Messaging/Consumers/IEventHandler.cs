using Messaging.Contracts;

namespace Messaging.Consumers;

public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default);
}
