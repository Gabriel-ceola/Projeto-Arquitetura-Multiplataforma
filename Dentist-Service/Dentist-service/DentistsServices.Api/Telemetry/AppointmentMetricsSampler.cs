using Dentist_service.DentistsServices.Domain;
using Dentist_service.DentistsServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dentist_service.DentistsServices.Api.Telemetry;

/// <summary>
/// Amostra periodicamente a quantidade de agendamentos por status no banco e publica
/// no gauge <c>appointment.active</c>. É independente do tráfego HTTP, então o gauge
/// continua correto mesmo sem requisições.
/// </summary>
public sealed class AppointmentMetricsSampler(
    IServiceScopeFactory scopeFactory,
    AppointmentMetrics metrics,
    ILogger<AppointmentMetricsSampler> logger) : BackgroundService
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
                logger.LogWarning(exception, "Falha ao amostrar agendamentos por status.");
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
        var db = scope.ServiceProvider.GetRequiredService<DentistServicesDbContext>();

        var counts = await db.Appointments
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(x => x.Status, x => x.Count);
        var total = 0;

        foreach (var status in Enum.GetValues<AppointmentStatus>())
        {
            var count = byStatus.GetValueOrDefault(status);
            metrics.SetActive(status.ToString().ToLowerInvariant(), count);
            total += count;
        }

        metrics.SetActive("total", total);
    }
}
