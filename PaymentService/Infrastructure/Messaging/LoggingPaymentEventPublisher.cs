using PaymentService.Domain;
using PaymentService.Domain.Ports;

namespace PaymentService.Infrastructure.Messaging;

public sealed class LoggingPaymentEventPublisher(
    ILogger<LoggingPaymentEventPublisher> logger) : IPaymentEventPublisher
{
    public Task PublishAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Evento de pagamento preparado para publicação. PaymentId: {PaymentId}, AppointmentId: {AppointmentId}, Status: {Status}",
            payment.Id,
            payment.AppointmentId,
            payment.Status);

        return Task.CompletedTask;
    }
}
