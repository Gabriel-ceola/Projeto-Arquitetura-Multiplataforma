using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Consumers;

public sealed class EventHandlerRegistry
{
    private readonly Dictionary<string, EventHandlerRegistration> _registrations = new(StringComparer.Ordinal);

    public IReadOnlyCollection<EventHandlerRegistration> Registrations => _registrations.Values;

    public void Register<TEvent>() where TEvent : IIntegrationEvent
    {
        _registrations.TryAdd(TEvent.EventType, new EventHandlerRegistration(TEvent.EventType, TEvent.Topic, InvokeAsync<TEvent>));
    }

    public bool TryGet(string eventType, [NotNullWhen(true)] out EventHandlerRegistration? registration) =>
        _registrations.TryGetValue(eventType, out registration);

    private static async Task InvokeAsync<TEvent>(
        IServiceProvider services,
        JsonElement data,
        EventMetadata metadata,
        CancellationToken cancellationToken) where TEvent : IIntegrationEvent
    {
        var @event = data.Deserialize<TEvent>(MessagingJson.Options)
            ?? throw new JsonException($"O evento {TEvent.EventType} chegou sem dados.");

        foreach (var handler in services.GetServices<IEventHandler<TEvent>>())
            await handler.HandleAsync(@event, metadata, cancellationToken);
    }
}
