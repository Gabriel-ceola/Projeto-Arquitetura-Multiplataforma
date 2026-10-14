using FluentAssertions;
using Messaging.Consumers;
using Messaging.Contracts;
using Messaging.Kafka;
using Messaging.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Messaging.Tests.Consumers;

public class EventDispatcherTests
{
    private readonly Mock<IEventHandler<SampleEvent>> _handlerMock = new();
    private readonly Mock<IMessageProducer> _producerMock = new();
    private readonly EventHandlerRegistry _registry = new();
    private readonly EventDispatcher _dispatcher;

    public EventDispatcherTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _handlerMock.Object);
        var provider = services.BuildServiceProvider();

        var options = new KafkaOptions
        {
            Consumer = new KafkaConsumerOptions { GroupId = "dentist-service", MaxRetries = 2, RetryDelayMs = 0 }
        };

        _dispatcher = new EventDispatcher(
            _registry,
            provider.GetRequiredService<IServiceScopeFactory>(),
            _producerMock.Object,
            Options.Create(options),
            NullLogger<EventDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerIsRegistered_ShouldInvokeHandlerWithEventAndMetadata()
    {
        _registry.Register<SampleEvent>();
        var @event = new SampleEvent(Guid.NewGuid(), "pagamento");

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(EventsFactory.EnvelopeJson(@event, "corr-9")));

        result.Should().Be(DispatchResult.Handled);
        _handlerMock.Verify(h => h.HandleAsync(
            @event,
            It.Is<EventMetadata>(m => m.CorrelationId == "corr-9" && m.Topic == Topics.Payments && m.Offset == 7),
            It.IsAny<CancellationToken>()), Times.Once);
        VerifyNothingWasDeadLettered();
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlerIsRegistered_ShouldSkipWithoutDeadLetter()
    {
        var json = EventsFactory.EnvelopeJson(new SampleEvent(Guid.NewGuid(), "sem handler"));

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(json));

        result.Should().Be(DispatchResult.Skipped);
        _handlerMock.VerifyNoOtherCalls();
        VerifyNothingWasDeadLettered();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerRecoversOnRetry_ShouldNotDeadLetter()
    {
        _registry.Register<SampleEvent>();
        _handlerMock
            .SetupSequence(h => h.HandleAsync(It.IsAny<SampleEvent>(), It.IsAny<EventMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("instável"))
            .Returns(Task.CompletedTask);

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(EventsFactory.EnvelopeJson(new SampleEvent(Guid.NewGuid(), "x"))));

        result.Should().Be(DispatchResult.Handled);
        _handlerMock.Verify(h => h.HandleAsync(It.IsAny<SampleEvent>(), It.IsAny<EventMetadata>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyNothingWasDeadLettered();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerKeepsFailing_ShouldRetryAndSendToDeadLetterWithOriginHeaders()
    {
        _registry.Register<SampleEvent>();
        _handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<SampleEvent>(), It.IsAny<EventMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falhou"));
        var json = EventsFactory.EnvelopeJson(new SampleEvent(Guid.NewGuid(), "x"));

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(json));

        result.Should().Be(DispatchResult.DeadLettered);
        _handlerMock.Verify(h => h.HandleAsync(It.IsAny<SampleEvent>(), It.IsAny<EventMetadata>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _producerMock.Verify(p => p.ProduceAsync(
            "payments.dlq",
            "key-1",
            json,
            It.Is<IReadOnlyDictionary<string, string>?>(h =>
                h![EventDispatcher.OriginalTopicHeader] == "payments" &&
                h[EventDispatcher.OriginalOffsetHeader] == "7" &&
                h[EventDispatcher.ConsumerGroupHeader] == "dentist-service" &&
                h[EventDispatcher.ErrorHeader] == "falhou"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("isto não é json")]
    [InlineData("{\"eventType\":\"\"}")]
    [InlineData("")]
    public async Task DispatchAsync_WhenMessageIsMalformed_ShouldSendToDeadLetterWithoutCallingHandlers(string value)
    {
        _registry.Register<SampleEvent>();

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(value));

        result.Should().Be(DispatchResult.DeadLettered);
        _handlerMock.VerifyNoOtherCalls();
        _producerMock.Verify(p => p.ProduceAsync("payments.dlq", It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenDataDoesNotMatchContract_ShouldSendToDeadLetterWithoutRetry()
    {
        _registry.Register<SampleEvent>();
        var json = "{\"eventId\":\"" + Guid.NewGuid() + "\",\"eventType\":\"SampleEvent\",\"eventVersion\":1," +
                   "\"occurredAtUtc\":\"2026-10-14T12:00:00Z\",\"source\":\"tests\",\"correlationId\":\"c\"," +
                   "\"data\":{\"id\":\"nao-e-guid\"}}";

        var result = await _dispatcher.DispatchAsync(EventsFactory.Message(json));

        result.Should().Be(DispatchResult.DeadLettered);
        _handlerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchAsync_WhenCancelled_ShouldThrowWithoutDeadLetter()
    {
        _registry.Register<SampleEvent>();
        using var cancellation = new CancellationTokenSource();
        _handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<SampleEvent>(), It.IsAny<EventMetadata>(), It.IsAny<CancellationToken>()))
            .Returns<SampleEvent, EventMetadata, CancellationToken>((_, _, token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(token);
            });

        var act = async () => await _dispatcher.DispatchAsync(
            EventsFactory.Message(EventsFactory.EnvelopeJson(new SampleEvent(Guid.NewGuid(), "x"))), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        VerifyNothingWasDeadLettered();
    }

    private void VerifyNothingWasDeadLettered() =>
        _producerMock.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);
}
