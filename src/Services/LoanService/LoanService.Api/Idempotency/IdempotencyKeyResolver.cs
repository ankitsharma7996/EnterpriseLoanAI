namespace LoanService.Api.Idempotency;

public static class IdempotencyKeyResolver
{
    public static string Resolve(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(
                IdempotencyHeaders.HeaderName,
                out var values))
        {
            throw new BadHttpRequestException(
                "Idempotency-Key header is required.");
        }

        var idempotencyKey = values.ToString().Trim();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new BadHttpRequestException(
                "Idempotency-Key header cannot be empty.");
        }

        if (idempotencyKey.Length >
            IdempotencyHeaders.MaximumLength)
        {
            throw new BadHttpRequestException(
                $"Idempotency-Key cannot exceed " +
                $"{IdempotencyHeaders.MaximumLength} characters.");
        }

        return idempotencyKey;
    }
}