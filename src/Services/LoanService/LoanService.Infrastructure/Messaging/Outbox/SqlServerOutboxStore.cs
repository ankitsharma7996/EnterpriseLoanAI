using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class SqlServerOutboxStore : IOutboxStore
{
    private const string ClaimSql = """
        ;WITH candidates AS
        (
            SELECT TOP (@batchSize) *
            FROM dbo.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE Status = 'Pending'
              AND RetryCount < @maximumRetryCount
              AND (NextAttemptOnUtc IS NULL OR NextAttemptOnUtc <= @claimedOnUtc)
            ORDER BY OccurredOnUtc
        )
        UPDATE candidates
        SET Status = 'Processing',
            LockedBy = @publisherId,
            ProcessingStartedOnUtc = @claimedOnUtc,
            LastError = NULL
        OUTPUT INSERTED.Id AS Value;
        """;

    private readonly LoanDbContext _dbContext;

    public SqlServerOutboxStore(LoanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        string publisherId,
        DateTimeOffset claimedOnUtc,
        int batchSize,
        int maximumRetryCount,
        CancellationToken cancellationToken)
    {
        var claimedIds = await _dbContext.Database
            .SqlQueryRaw<Guid>(
                ClaimSql,
                new SqlParameter("@batchSize", batchSize),
                new SqlParameter("@maximumRetryCount", maximumRetryCount),
                new SqlParameter("@claimedOnUtc", claimedOnUtc),
                new SqlParameter("@publisherId", publisherId))
            .ToListAsync(cancellationToken);

        if (claimedIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.OutboxMessages
            .Where(message => claimedIds.Contains(message.Id))
            .OrderBy(message => message.OccurredOnUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<int> RecoverStaleClaimsAsync(
        DateTimeOffset processingStartedBeforeUtc,
        CancellationToken cancellationToken)
    {
        return _dbContext.OutboxMessages
            .Where(message =>
                message.Status == OutboxMessageStatus.Processing &&
                message.ProcessingStartedOnUtc < processingStartedBeforeUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        message => message.Status,
                        OutboxMessageStatus.Pending)
                    .SetProperty(
                        message => message.ProcessingStartedOnUtc,
                        (DateTimeOffset?)null)
                    .SetProperty(
                        message => message.LockedBy,
                        (string?)null),
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
