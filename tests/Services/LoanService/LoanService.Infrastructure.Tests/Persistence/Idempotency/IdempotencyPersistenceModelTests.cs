using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LoanService.Infrastructure.Tests.Persistence.Idempotency;

public sealed class IdempotencyPersistenceModelTests
{
    [Fact]
    public void Model_HasUniqueIndex_OnOperationAndIdempotencyKey()
    {
        // Arrange
        var options =
            new DbContextOptionsBuilder<LoanDbContext>()
                .UseInMemoryDatabase(
                    $"IdempotencyModel-{Guid.NewGuid()}")
                .Options;

        using var dbContext =
            new LoanDbContext(options);

        // Act
        var entityType =
            dbContext.Model.FindEntityType(
                typeof(IdempotencyRequest));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .SingleOrDefault(index =>
                index.Properties
                    .Select(property => property.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(IdempotencyRequest.Operation),
                            nameof(IdempotencyRequest.IdempotencyKey)
                        }));

        // Assert
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void Model_HasIndex_OnExpiresOnUtc()
    {
        var options =
            new DbContextOptionsBuilder<LoanDbContext>()
                .UseInMemoryDatabase(
                    $"IdempotencyModel-{Guid.NewGuid()}")
                .Options;

        using var dbContext =
            new LoanDbContext(options);

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(IdempotencyRequest));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .SingleOrDefault(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name ==
                nameof(IdempotencyRequest.ExpiresOnUtc));

        Assert.NotNull(index);
    }
}