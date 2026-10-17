using Messaging.Kafka;
using Messaging.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static MessagingBuilder AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(KafkaOptions.SectionName);
        var options = section.Get<KafkaOptions>() ?? new KafkaOptions();

        services.Configure<KafkaOptions>(section);
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IMessageProducer, KafkaMessageProducer>();
        services.TryAddSingleton<MessagingMetrics>();

        return new MessagingBuilder(services, options);
    }
}
