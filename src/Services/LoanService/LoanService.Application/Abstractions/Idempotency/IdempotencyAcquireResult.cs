namespace LoanService.Application.Abstractions.Idempotency;

public sealed record IdempotencyAcquireResult(
    IdempotencyAcquireStatus Status,
    int? ResponseStatusCode = null,
    string? ResponseBody = null);