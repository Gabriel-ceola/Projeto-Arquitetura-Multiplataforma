using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Infrastructure.Data;

namespace PaymentService.Infrastructure.Telemetry;

/// <summary>
/// Amostra periodicamente a quantidade de pagamentos por status no banco e publica no gauge
/// <c>payment.active</c>. Independe do tráfego HTTP, mantendo o gauge correto mesmo ocioso.
/// </summary>
public sealed class PaymentMetricsSampler(
    IServiceScopeFactory scopeFactory,
    PaymentMetrics metrics,
    ILogger<PaymentMetricsSampler> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SampleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Falha ao amostrar pagamentos por status.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SampleAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        var counts = await db.Payments
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(x => x.Status, x => x.Count);
        var total = 0;

        foreach (var status in Enum.GetValues<PaymentStatus>())
        {
            var count = byStatus.GetValueOrDefault(status);
            metrics.SetActive(status.ToString().ToLowerInvariant(), count);
            total += count;
        }

        metrics.SetActive("total", total);
    }
}
