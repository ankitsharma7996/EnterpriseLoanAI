namespace LoanService.Application.Abstractions.Idempotency;

public enum IdempotencyAcquireStatus
{
    Acquired = 1,
    AlreadyProcessing = 2,
    AlreadyCompleted = 3,
    RequestMismatch = 4
}