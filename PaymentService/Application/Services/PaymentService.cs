using PaymentService.Application.DTOs;
using PaymentService.Application.Logging;
using PaymentService.Domain;
using PaymentService.Domain.Ports;

namespace PaymentService.Application.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentEventPublisher eventPublisher,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            logger.PaymentNotFound(id);
            return null;
        }

        return ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var payments = await repository.GetAllAsync(cancellationToken);
        return payments.Select(ToResponse).ToList();
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AppointmentId == Guid.Empty)
            throw new ArgumentException("O ID do agendamento é obrigatório.", nameof(request));

        if (request.Amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.", nameof(request));

        var now = DateTimeOffset.UtcNow;
        var payment = new Payment(Guid.NewGuid(), request.AppointmentId, request.Amount, now);
        await repository.AddAsync(payment, cancellationToken);
        await eventPublisher.PublishAsync(payment, cancellationToken);
        logger.PaymentCreated(payment.Id, payment.AppointmentId, payment.Amount);
        return ToResponse(payment);
    }

    public async Task<PaymentResponse?> UpdateStatusAsync(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Status is null)
            throw new ArgumentException("O status do pagamento é obrigatório.", nameof(request));

        var payment = await repository.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            logger.PaymentNotFound(id);
            return null;
        }

        var previousStatus = payment.Status;
        payment.UpdateStatus(request.Status.Value, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(payment, cancellationToken);
        await eventPublisher.PublishAsync(payment, cancellationToken);
        logger.PaymentStatusChanged(payment.Id, payment.AppointmentId, previousStatus, payment.Status);
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
