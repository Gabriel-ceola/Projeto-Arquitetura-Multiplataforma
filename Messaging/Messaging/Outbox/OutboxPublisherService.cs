using System.Diagnostics;
using Messaging.Kafka;
using Messaging.Telemetry;
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
    private readonly MessagingMetrics _metrics;
    private readonly ILogger<OutboxPublisherService<TDbContext>> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IMessageProducer producer,
        IOptions<KafkaOptions> options,
        MessagingMetrics metrics,
        ILogger<OutboxPublisherService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value.Outbox;
        _metrics = metrics;
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
                _logger.LogError(exception, "Falha ao ler o outbox de {Context}.", typeof(TDbContext).Name);
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
        var startedAt = Stopwatch.GetTimestamp();
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
            try
            {
                await _producer.ProduceAsync(message.Topic, message.Key, message.Payload, cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.RegisterFailure(exception.Message);
                await context.SaveChangesAsync(cancellationToken);
                _metrics.OutboxPublishFailed(message.Topic);
                _logger.LogWarning(exception, "Falha ao publicar o evento {EventId} ({EventType}) no tópico {Topic}.",
                    message.Id, message.EventType, message.Topic);
                break;
            }

            message.MarkAsProcessed(DateTime.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            _metrics.OutboxPublished(message.EventType, message.Topic);
            published++;
        }

        _metrics.OutboxPending(await CountPendingAsync(context, cancellationToken));
        _metrics.OutboxPublishDuration(Stopwatch.GetElapsedTime(startedAt).TotalSeconds);

        return published;
    }

    private static Task<int> CountPendingAsync(TDbContext context, CancellationToken cancellationToken) =>
        context.Set<OutboxMessage>().CountAsync(x => x.ProcessedAtUtc == null, cancellationToken);
}
