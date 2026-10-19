using PaymentService.Application.Logging;
using PaymentService.Domain;
using PaymentService.Domain.Ports;

namespace PaymentService.Infrastructure.Messaging;

public sealed class LoggingPaymentEventPublisher(
    ILogger<LoggingPaymentEventPublisher> logger) : IPaymentEventPublisher
{
    public Task PublishAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        logger.PaymentEventPrepared(payment.Id, payment.AppointmentId, payment.Status);

        return Task.CompletedTask;
    }
}
