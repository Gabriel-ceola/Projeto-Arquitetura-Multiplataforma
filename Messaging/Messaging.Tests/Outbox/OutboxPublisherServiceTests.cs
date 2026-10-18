using Confluent.Kafka;
using FluentAssertions;
using Messaging.Kafka;
using Messaging.Outbox;
using Messaging.Telemetry;
using Messaging.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Messaging.Tests.Outbox;

public class OutboxPublisherServiceTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly Mock<IMessageProducer> _producerMock = new();
    private readonly List<string> _publishedKeys = [];
    private readonly OutboxPublisherService<TestDbContext> _service;

    public OutboxPublisherServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(_databaseName, TestDbContext.Root));
        var provider = services.BuildServiceProvider();

        _producerMock
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, string, IReadOnlyDictionary<string, string>?, CancellationToken>(
                (_, key, _, _, _) => _publishedKeys.Add(key!))
            .Returns(Task.CompletedTask);

        _service = new OutboxPublisherService<TestDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            _producerMock.Object,
            Options.Create(new KafkaOptions { Outbox = new KafkaOutboxOptions { BatchSize = 10 } }),
            new MessagingMetrics(),
            NullLogger<OutboxPublisherService<TestDbContext>>.Instance);
    }

    [Fact]
    public async Task PublishPendingAsync_WhenMessagesArePending_ShouldPublishInOrderAndMarkAsProcessed()
    {
        var baseTime = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
        await SeedAsync(
            NewMessage("second", baseTime.AddSeconds(2)),
            NewMessage("first", baseTime.AddSeconds(1)),
            NewMessage("third", baseTime.AddSeconds(3)));

        var published = await _service.PublishPendingAsync();

        published.Should().Be(3);
        _publishedKeys.Should().Equal("first", "second", "third");
        await using var context = TestDbContext.Create(_databaseName);
        context.OutboxMessages.Should().OnlyContain(x => x.ProcessedAtUtc != null);
    }

    [Fact]
    public async Task PublishPendingAsync_WhenMessageWasAlreadyProcessed_ShouldNotPublishAgain()
    {
        var processed = NewMessage("done", DateTime.UtcNow);
        processed.MarkAsProcessed(DateTime.UtcNow);
        await SeedAsync(processed);

        var published = await _service.PublishPendingAsync();

        published.Should().Be(0);
        _publishedKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishPendingAsync_WhenProducerFails_ShouldRegisterFailureAndStopBatch()
    {
        var baseTime = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
        await SeedAsync(NewMessage("first", baseTime.AddSeconds(1)), NewMessage("second", baseTime.AddSeconds(2)));
        _producerMock
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), "first", It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KafkaException(ErrorCode.Local_MsgTimedOut));

        var published = await _service.PublishPendingAsync();

        published.Should().Be(0);
        _producerMock.Verify(p => p.ProduceAsync(It.IsAny<string>(), "second", It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        await using var context = TestDbContext.Create(_databaseName);
        var failed = context.OutboxMessages.Single(x => x.Key == "first");
        failed.ProcessedAtUtc.Should().BeNull();
        failed.Attempts.Should().Be(1);
        failed.LastError.Should().NotBeNullOrWhiteSpace();
        context.OutboxMessages.Single(x => x.Key == "second").ProcessedAtUtc.Should().BeNull();
    }

    private static OutboxMessage NewMessage(string key, DateTime occurredAtUtc) =>
        new(Guid.CreateVersion7(), "appointments", key, "AppointmentRequested", "{}", occurredAtUtc);

    private async Task SeedAsync(params OutboxMessage[] messages)
    {
        await using var context = TestDbContext.Create(_databaseName);
        context.OutboxMessages.AddRange(messages);
        await context.SaveChangesAsync();
    }
}
