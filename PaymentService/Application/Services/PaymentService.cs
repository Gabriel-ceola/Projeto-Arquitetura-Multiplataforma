using PaymentService.Application.DTOs;
using PaymentService.Domain;
using PaymentService.Domain.Ports;
using PaymentService.Infrastructure.Telemetry;

namespace PaymentService.Application.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentEventPublisher eventPublisher,
    PaymentMetrics metrics) : IPaymentService
{
    public async Task<PaymentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetByIdAsync(id, cancellationToken);
        metrics.Read("get_by_id", payment is null ? PaymentMetrics.ResultNotFound : PaymentMetrics.ResultOk);
        return payment is null ? null : ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var payments = await repository.GetAllAsync(cancellationToken);
        metrics.Read("list", PaymentMetrics.ResultOk);
        return payments.Select(ToResponse).ToList();
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            metrics.CreationRejected(PaymentMetrics.ReasonInvalidAppointment);
            throw new ArgumentException("O ID do agendamento é obrigatório.", nameof(request));
        }

        if (request.Amount <= 0)
        {
            metrics.CreationRejected(PaymentMetrics.ReasonInvalidAmount);
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var payment = new Payment(Guid.NewGuid(), request.AppointmentId, request.Amount, now);
        await repository.AddAsync(payment, cancellationToken);
        await eventPublisher.PublishAsync(payment, cancellationToken);
        metrics.CreationCreated(payment.Amount);
        metrics.RecordAmount(payment.Amount);
        return ToResponse(payment);
    }

    public async Task<PaymentResponse?> UpdateStatusAsync(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is null)
        {
            metrics.StatusUpdate(
                PaymentMetrics.ResultRejected,
                PaymentMetrics.ReasonMissingStatus,
                "unknown",
                "unknown");
            throw new ArgumentException("O status do pagamento é obrigatório.", nameof(request));
        }

        var requestedStatus = request.Status.Value.ToString().ToLowerInvariant();
        var payment = await repository.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            metrics.StatusUpdate(
                PaymentMetrics.ResultNotFound,
                PaymentMetrics.ReasonNotFound,
                "unknown",
                requestedStatus);
            return null;
        }

        var previousStatus = payment.Status.ToString().ToLowerInvariant();
        payment.UpdateStatus(request.Status.Value, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(payment, cancellationToken);
        await eventPublisher.PublishAsync(payment, cancellationToken);
        metrics.StatusUpdate(PaymentMetrics.ResultOk, PaymentMetrics.ReasonNone, previousStatus, requestedStatus);
        return ToResponse(payment);
    }

    private static PaymentResponse ToResponse(Payment payment) =>
        new(
            payment.Id,
            payment.AppointmentId,
            payment.Amount,
            payment.Status,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.ProcessedAt);
}
