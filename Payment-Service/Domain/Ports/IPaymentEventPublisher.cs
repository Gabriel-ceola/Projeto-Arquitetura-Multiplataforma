using PaymentService.Domain;

namespace PaymentService.Domain.Ports;

public interface IPaymentEventPublisher
{
    Task PublishAsync(Payment payment, CancellationToken cancellationToken = default);
}
