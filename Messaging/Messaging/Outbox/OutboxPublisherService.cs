using System.Text.Json;
using Messaging.Contracts;
using Messaging.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Outbox;

public sealed class OutboxPublisherService<TDbContext> : BackgroundService where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageProducer _producer;
    private readonly KafkaOutboxOptions _options;
    private readonly ILogger<OutboxPublisherService<TDbContext>> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IMessageProducer producer,
        IOptions<KafkaOptions> options,
        ILogger<OutboxPublisherService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value.Outbox;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(_options.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.OutboxReadFailed(exception, typeof(TDbContext).Name);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    internal async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var pending = await context.Set<OutboxMessage>()
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in pending)
        {
            // Escopo com o correlationId original do evento, para ligar a publicação à requisição de origem.
            using var correlationScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = ReadCorrelationId(message.Payload) ?? message.Id.ToString()
            });

            try
            {
                await _producer.ProduceAsync(message.Topic, message.Key, message.Payload, cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.RegisterFailure(exception.Message);
                await context.SaveChangesAsync(cancellationToken);
                _logger.OutboxPublishFailed(exception, message.EventType, message.Id, message.Topic, message.Attempts);
                break;
            }

            var processedAtUtc = DateTime.UtcNow;
            message.MarkAsProcessed(processedAtUtc);
            await context.SaveChangesAsync(cancellationToken);
            published++;
            _logger.OutboxPublished(message.EventType, message.Id, message.Topic, message.Key,
                (long)(processedAtUtc - message.OccurredAtUtc).TotalMilliseconds);
        }

        return published;
    }

    private static string? ReadCorrelationId(string payload)
    {
        try
        {
            return EventEnvelope.FromJson(payload)?.CorrelationId;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
