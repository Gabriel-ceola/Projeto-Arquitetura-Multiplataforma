namespace PaymentService.Domain;

public sealed class Payment
{
    public Payment(
        Guid id,
        Guid appointmentId,
        decimal amount,
        DateTimeOffset createdAt)
    {
        Id = id;
        AppointmentId = appointmentId;
        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid AppointmentId { get; }
    public decimal Amount { get; }
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public void UpdateStatus(PaymentStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
        ProcessedAt = status is PaymentStatus.Paid or PaymentStatus.Failed or PaymentStatus.Cancelled
            ? updatedAt
            : ProcessedAt;
    }
}
