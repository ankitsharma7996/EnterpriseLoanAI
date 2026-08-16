using LoanService.Application.Abstractions.Idempotency;
using LoanService.Infrastructure.Idempotency;
using LoanService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LoanService.Infrastructure.Tests.Idempotency;

public sealed class IdempotencyServiceSqlServerConcurrencyTests
{
    [Fact]
    public async Task ConcurrentAcquisitions_ForSameOperationAndKey_HaveOneOwner()
    {
        var databaseName =
            $"EnterpriseLoanAI_Idempotency_{Guid.NewGuid():N}";

        var connectionString =
            "Server=(localdb)\\mssqllocaldb;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;";

        var setupOptions =
            new DbContextOptionsBuilder<LoanDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        await using var setupContext =
            new LoanDbContext(setupOptions);

        try
        {
            await setupContext.Database.MigrateAsync(
                TestContext.Current.CancellationToken);

            var saveBarrier = new ConcurrentSaveBarrier();
            var options =
                new DbContextOptionsBuilder<LoanDbContext>()
                    .UseSqlServer(connectionString)
                    .AddInterceptors(saveBarrier)
                    .Options;

            await using var firstContext = new LoanDbContext(options);
            await using var secondContext = new LoanDbContext(options);

            var firstService = new IdempotencyService(
                firstContext,
                TimeProvider.System);

            var secondService = new IdempotencyService(
                secondContext,
                TimeProvider.System);

            var idempotencyKey = $"key-{Guid.NewGuid():N}";
            var requestHash = new string('A', 64);

            var results = await Task.WhenAll(
                firstService.TryAcquireAsync(
                    idempotencyKey,
                    "CreateLoan",
                    requestHash,
                    TestContext.Current.CancellationToken),
                secondService.TryAcquireAsync(
                    idempotencyKey,
                    "CreateLoan",
                    requestHash,
                    TestContext.Current.CancellationToken));

            Assert.Single(
                results,
                result =>
                    result.Status == IdempotencyAcquireStatus.Acquired);

            Assert.Single(
                results,
                result =>
                    result.Status ==
                    IdempotencyAcquireStatus.AlreadyProcessing);

            await using var verificationContext =
                new LoanDbContext(setupOptions);

            Assert.Equal(
                1,
                await verificationContext.IdempotencyRequests.CountAsync(
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            await setupContext.Database.EnsureDeletedAsync(
                CancellationToken.None);
        }
    }

    private sealed class ConcurrentSaveBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _bothSavesReady =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _saveCount;

        public override async ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _saveCount) == 2)
            {
                _bothSavesReady.TrySetResult();
            }

            await _bothSavesReady.Task.WaitAsync(cancellationToken);

            return result;
        }
    }
}
