namespace PaymentService.Domain;

public enum PaymentStatus
{
    Pending,
    Processing,
    Paid,
    Failed,
    Cancelled
}
