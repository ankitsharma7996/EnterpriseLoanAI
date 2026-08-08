namespace LoanService.Api.Idempotency;

public static class IdempotencyHeaders
{
    public const string HeaderName = "Idempotency-Key";
    public const int MaximumLength = 128;
}