using LoanService.Infrastructure.Persistence.Outbox;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        string publisherId,
        DateTimeOffset claimedOnUtc,
        int batchSize,
        int maximumRetryCount,
        CancellationToken cancellationToken);

    Task<int> RecoverStaleClaimsAsync(
        DateTimeOffset processingStartedBeforeUtc,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
