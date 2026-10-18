using Microsoft.AspNetCore.Mvc.Filters;

namespace Dentist_service.DentistsServices.Api.Telemetry;

/// <summary>
/// Registra métricas de negócio para requisições barradas pela validação de modelo
/// (binding de query/body), que retornam 400 antes de a action ser executada.
/// </summary>
public sealed class AppointmentModelValidationFilter(AppointmentMetrics metrics) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var value)
            ? value
            : string.Empty;
        var reason = AppointmentMetrics.ReasonModelValidation;

        switch (action)
        {
            case "Create":
                metrics.CreationRejected(reason, "unknown");
                break;
            case "Availability":
                metrics.Availability(reason);
                break;
            case "SetWeeklyHours":
                metrics.ScheduleUpdate("weekly", reason);
                break;
            case "SetException":
                metrics.ScheduleUpdate("exception", reason);
                break;
            case "Cancel":
                metrics.Cancellation(AppointmentMetrics.ResultRejected, reason, "unknown");
                break;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Nada a fazer.
    }
}
