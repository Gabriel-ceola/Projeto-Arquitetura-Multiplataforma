using Messaging.Consumers;
using Messaging.Contracts;
using Messaging.Kafka;
using Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging;

public sealed class MessagingBuilder
{
    private readonly EventHandlerRegistry _registry = new();

    internal MessagingBuilder(IServiceCollection services, KafkaOptions options)
    {
        Services = services;
        Options = options;
        Services.TryAddSingleton(_registry);
    }

    public IServiceCollection Services { get; }
    public KafkaOptions Options { get; }

    public MessagingBuilder AddOutbox<TDbContext>() where TDbContext : DbContext
    {
        Services.TryAddScoped<IOutbox, EfOutbox<TDbContext>>();

        if (Options.Enabled)
            Services.AddHostedService<OutboxPublisherService<TDbContext>>();

        return this;
    }

    public MessagingBuilder AddConsumer()
    {
        Services.TryAddSingleton<EventDispatcher>();

        if (Options.Enabled)
            Services.AddHostedService<KafkaConsumerService>();

        return this;
    }

    public MessagingBuilder AddEventHandler<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        Services.AddScoped<IEventHandler<TEvent>, THandler>();
        _registry.Register<TEvent>();
        return this;
    }
}
