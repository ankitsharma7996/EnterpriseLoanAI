namespace LoanService.Application.Abstractions.Idempotency;

public interface IIdempotencyService
{
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string idempotencyKey,
        string operation,
        string requestHash,
        CancellationToken cancellationToken = default);
}