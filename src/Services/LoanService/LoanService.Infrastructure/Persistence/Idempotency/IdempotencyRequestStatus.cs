namespace LoanService.Infrastructure.Persistence.Idempotency;

public enum IdempotencyRequestStatus
{
    Processing = 1,
    Completed = 2,
    Failed = 3
}
