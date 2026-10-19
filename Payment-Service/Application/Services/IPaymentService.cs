using PaymentService.Application.DTOs;

namespace PaymentService.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> UpdateStatusAsync(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default);
}
