namespace LoanService.Infrastructure.Persistence.Idempotency;

public sealed class IdempotencyRequest
{
    private IdempotencyRequest()
    {
    }

    private IdempotencyRequest(
        Guid id,
        string idempotencyKey,
        string operation,
        string requestHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        Operation = operation;
        RequestHash = requestHash;
        Status = IdempotencyRequestStatus.Processing;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    public Guid Id { get; private set; }

    public string IdempotencyKey { get; private set; } = null!;

    public string Operation { get; private set; } = null!;

    public string RequestHash { get; private set; } = null!;

    public IdempotencyRequestStatus Status { get; private set; }

    public int? ResponseStatusCode { get; private set; }

    public string? ResponseBody { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? CompletedOnUtc { get; private set; }

    public DateTimeOffset ExpiresOnUtc { get; private set; }

    public static IdempotencyRequest Create(
        Guid id,
        string idempotencyKey,
        string operation,
        string requestHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Idempotency request ID is required.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException(
                "Operation is required.",
                nameof(operation));
        }

        if (string.IsNullOrWhiteSpace(requestHash))
        {
            throw new ArgumentException(
                "Request hash is required.",
                nameof(requestHash));
        }

        if (requestHash.Trim().Length != 64)
        {
            throw new ArgumentException(
                "Request hash must be 64 characters.",
                nameof(requestHash));
        }

        if (expiresOnUtc <= createdOnUtc)
        {
            throw new ArgumentException(
                "Expiration must be after creation time.",
                nameof(expiresOnUtc));
        }

        return new IdempotencyRequest(
            id,
            idempotencyKey.Trim(),
            operation.Trim(),
            requestHash.Trim(),
            createdOnUtc,
            expiresOnUtc);
    }
}
