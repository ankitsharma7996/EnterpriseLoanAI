using LoanService.Application.Abstractions.Idempotency;
using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Idempotency;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Idempotency;

internal sealed class IdempotencyService
    : IIdempotencyService
{
    private readonly LoanDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public IdempotencyService(
        LoanDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IdempotencyAcquireResult> TryAcquireAsync(
        string idempotencyKey,
        string operation,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var existing =
            await _dbContext.IdempotencyRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Operation == operation &&
                        x.IdempotencyKey == idempotencyKey,
                    cancellationToken);

        if (existing is not null)
        {
            return ResolveExisting(
                existing,
                requestHash);
        }

        var now =
            _timeProvider.GetUtcNow();

        var request =
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                idempotencyKey,
                operation,
                requestHash,
                now,
                now.AddHours(24));

        _dbContext.IdempotencyRequests.Add(request);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new IdempotencyAcquireResult(
                IdempotencyAcquireStatus.Acquired);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
                {
                    _dbContext.Entry(request).State =
                        EntityState.Detached;

                    existing =
                        await _dbContext.IdempotencyRequests
                            .AsNoTracking()
                            .SingleOrDefaultAsync(
                                x =>
                                    x.Operation == operation &&
                                    x.IdempotencyKey == idempotencyKey,
                                cancellationToken);

                    if (existing is null)
                    {
                        throw;
                    }

                    return ResolveExisting(
                        existing,
                        requestHash);
                }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               sqlException.Number is 2601 or 2627;
    }
    

    private static IdempotencyAcquireResult ResolveExisting(
        IdempotencyRequest existing,
        string requestHash)
    {
        if (!string.Equals(
                existing.RequestHash,
                requestHash,
                StringComparison.Ordinal))
        {
            return new IdempotencyAcquireResult(
                IdempotencyAcquireStatus.RequestMismatch);
        }

        return existing.Status switch
        {
            IdempotencyRequestStatus.Processing =>
                new IdempotencyAcquireResult(
                    IdempotencyAcquireStatus.AlreadyProcessing),

            IdempotencyRequestStatus.Completed =>
                new IdempotencyAcquireResult(
                    IdempotencyAcquireStatus.AlreadyCompleted,
                    existing.ResponseStatusCode,
                    existing.ResponseBody),

            _ =>
                new IdempotencyAcquireResult(
                    IdempotencyAcquireStatus.AlreadyProcessing)
        };
    }
}
