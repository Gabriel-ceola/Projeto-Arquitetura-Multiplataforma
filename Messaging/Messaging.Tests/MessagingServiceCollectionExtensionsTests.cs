using FluentAssertions;
using Messaging.Consumers;
using Messaging.Contracts;
using Messaging.Kafka;
using Messaging.Outbox;
using Messaging.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Messaging.Tests;

public class MessagingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddKafkaMessaging_WhenDisabled_ShouldRegisterOutboxWithoutHostedServices()
    {
        var services = CreateServices();

        services.AddKafkaMessaging(BuildConfiguration(enabled: false))
            .AddOutbox<TestDbContext>()
            .AddConsumer();

        services.Should().NotContain(d => d.ServiceType == typeof(IHostedService));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOutbox>().Should().BeOfType<EfOutbox<TestDbContext>>();
    }

    [Fact]
    public void AddKafkaMessaging_WhenEnabled_ShouldRegisterOutboxPublisherAndConsumer()
    {
        var services = CreateServices();

        services.AddKafkaMessaging(BuildConfiguration(enabled: true))
            .AddOutbox<TestDbContext>()
            .AddConsumer();

        var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).Select(d => d.ImplementationType);
        hostedServices.Should().BeEquivalentTo([typeof(OutboxPublisherService<TestDbContext>), typeof(KafkaConsumerService)]);
    }

    [Fact]
    public void AddKafkaMessaging_ShouldBindOptionsFromKafkaSection()
    {
        var services = CreateServices();

        services.AddKafkaMessaging(BuildConfiguration(enabled: true));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
        options.ClientId.Should().Be("dentist-service");
        options.Consumer.GroupId.Should().Be("dentist-service");
        options.Consumer.Topics.Should().Equal(Topics.Payments, Topics.HealthInsurance);
        options.Outbox.BatchSize.Should().Be(25);
    }

    [Fact]
    public void AddEventHandler_ShouldRegisterHandlerAndMakeEventTypeDispatchable()
    {
        var services = CreateServices();

        services.AddKafkaMessaging(BuildConfiguration(enabled: false))
            .AddConsumer()
            .AddEventHandler<SampleEvent, SampleEventHandler>();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<EventHandlerRegistry>().TryGet(SampleEvent.EventType, out var registration).Should().BeTrue();
        registration!.Topic.Should().Be(Topics.Payments);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEventHandler<SampleEvent>>().Should().BeOfType<SampleEventHandler>();
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString(), TestDbContext.Root));
        return services;
    }

    private static IConfiguration BuildConfiguration(bool enabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:ClientId"] = "dentist-service",
                ["Kafka:Enabled"] = enabled.ToString(),
                ["Kafka:Outbox:BatchSize"] = "25",
                ["Kafka:Consumer:GroupId"] = "dentist-service",
                ["Kafka:Consumer:Topics:0"] = Topics.Payments,
                ["Kafka:Consumer:Topics:1"] = Topics.HealthInsurance
            })
            .Build();

    public sealed class SampleEventHandler : IEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
