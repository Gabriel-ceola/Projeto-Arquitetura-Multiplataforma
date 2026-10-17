using Microsoft.AspNetCore.Mvc.Filters;
using PaymentService.Controllers;

namespace PaymentService.Infrastructure.Telemetry;

/// <summary>
/// Registra métricas de negócio para requisições barradas pela validação de modelo
/// (DataAnnotations/[ApiController]), que retornam 400 antes de a action ser executada.
/// </summary>
public sealed class ModelValidationMetricsFilter(PaymentMetrics metrics) : IAlwaysRunResultFilter
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

        switch (action)
        {
            case nameof(PaymentsController.Create):
                metrics.CreationRejected(PaymentMetrics.ReasonModelValidation);
                break;
            case nameof(PaymentsController.UpdateStatus):
                metrics.StatusUpdate(
                    PaymentMetrics.ResultRejected,
                    PaymentMetrics.ReasonModelValidation,
                    "unknown",
                    "unknown");
                break;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Nada a fazer.
    }
}
