namespace LoanService.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        Guid aggregateId,
        long aggregateVersion,
        Guid correlationId,
        string eventType,
        string eventVersion,
        string payload,
        DateTimeOffset occurredOnUtc)
    {
        Id = id;
        AggregateId = aggregateId;
        AggregateVersion = aggregateVersion;
        CorrelationId = correlationId;
        EventType = eventType;
        EventVersion = eventVersion;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid AggregateId { get; private set; }

    public long AggregateVersion { get; private set; }

    public Guid CorrelationId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string EventVersion { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTimeOffset? NextAttemptOnUtc { get; private set; }

    public DateTimeOffset? ProcessingStartedOnUtc { get; private set; }

    public DateTimeOffset? PublishedOnUtc { get; private set; }

    public string? LockedBy { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(
        Guid id,
        Guid aggregateId,
        long aggregateVersion,
        Guid correlationId,
        string eventType,
        string eventVersion,
        string payload,
        DateTimeOffset occurredOnUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Outbox message ID cannot be empty.",
                nameof(id));
        }

        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException(
                "Aggregate ID cannot be empty.",
                nameof(aggregateId));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation ID cannot be empty.",
                nameof(correlationId));
        }

        if (aggregateVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aggregateVersion),
                "Aggregate version must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException(
                "Event type is required.",
                nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(eventVersion))
        {
            throw new ArgumentException(
                "Event version is required.",
                nameof(eventVersion));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Payload is required.",
                nameof(payload));
        }

        return new OutboxMessage(
            id,
            aggregateId,
            aggregateVersion,
            correlationId,
            eventType.Trim(),
            eventVersion.Trim(),
            payload,
            occurredOnUtc);
    }

    public void MarkAsProcessing(
        string publisherId,
        DateTimeOffset startedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(publisherId))
        {
            throw new ArgumentException(
                "Publisher ID is required.",
                nameof(publisherId));
        }

        Status = OutboxMessageStatus.Processing;
        LockedBy = publisherId;
        ProcessingStartedOnUtc = startedOnUtc;
        LastError = null;
    }

    public void MarkAsPublished(
        DateTimeOffset publishedOnUtc)
    {
        Status = OutboxMessageStatus.Published;
        PublishedOnUtc = publishedOnUtc;
        ProcessingStartedOnUtc = null;
        NextAttemptOnUtc = null;
        LockedBy = null;
        LastError = null;
    }

    public void ResetForRetry()
    {
        Status = OutboxMessageStatus.Pending;
        ProcessingStartedOnUtc = null;
        LockedBy = null;
    }

    public void ScheduleRetry(string error,
        DateTimeOffset nextAttemptOnUtc)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException(
                "Error is required.",
                nameof(error));
        }

        Status = OutboxMessageStatus.Pending;
        RetryCount++;
        LastError = TruncateError(error);
        NextAttemptOnUtc = nextAttemptOnUtc;
        ProcessingStartedOnUtc = null;
        LockedBy = null;
    }

    public void MarkAsPermanentlyFailed(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException(
                "Error is required.",
                nameof(error));
        }

        Status = OutboxMessageStatus.Failed;
        RetryCount++;
        LastError = TruncateError(error);
        NextAttemptOnUtc = null;
        ProcessingStartedOnUtc = null;
        LockedBy = null;
    }

    private static string TruncateError(string error)
    {
        const int maximumLength = 4000;

        return error.Length <= maximumLength
            ? error
            : error[..maximumLength];
    }
}