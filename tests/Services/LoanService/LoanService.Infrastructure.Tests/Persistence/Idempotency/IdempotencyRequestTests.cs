using LoanService.Infrastructure.Persistence.Idempotency;
using Xunit;

namespace LoanService.Infrastructure.Tests.Persistence.Idempotency;

public sealed class IdempotencyRequestTests
{
    [Fact]
    public void Create_WithValidValues_CreatesProcessingRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdOnUtc = DateTimeOffset.UtcNow;
        var expiresOnUtc = createdOnUtc.AddHours(24);

        // Act
        var request = IdempotencyRequest.Create(
            id,
            "loan-create-abc123",
            "CreateLoan",
            new string('A', 64),
            createdOnUtc,
            expiresOnUtc);

        // Assert
        Assert.Equal(id, request.Id);
        Assert.Equal(
            "loan-create-abc123",
            request.IdempotencyKey);

        Assert.Equal(
            "CreateLoan",
            request.Operation);

        Assert.Equal(
            new string('A', 64),
            request.RequestHash);

        Assert.Equal(
            IdempotencyRequestStatus.Processing,
            request.Status);

        Assert.Equal(
            createdOnUtc,
            request.CreatedOnUtc);

        Assert.Equal(
            expiresOnUtc,
            request.ExpiresOnUtc);

        Assert.Null(request.ResponseStatusCode);
        Assert.Null(request.ResponseBody);
        Assert.Null(request.CompletedOnUtc);
    }

    [Fact]
    public void Create_TrimsKeyOperationAndHash()
    {
        // Arrange
        var createdOnUtc = DateTimeOffset.UtcNow;

        // Act
        var request = IdempotencyRequest.Create(
            Guid.NewGuid(),
            "  ABC-123  ",
            "  CreateLoan  ",
            $"  {new string('A', 64)}  ",
            createdOnUtc,
            createdOnUtc.AddHours(24));

        // Assert
        Assert.Equal(
            "ABC-123",
            request.IdempotencyKey);

        Assert.Equal(
            "CreateLoan",
            request.Operation);

        Assert.Equal(
            new string('A', 64),
            request.RequestHash);
    }

    [Fact]
    public void Create_WithEmptyId_ThrowsArgumentException()
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.Empty,
                "ABC-123",
                "CreateLoan",
                new string('A', 64),
                createdOnUtc,
                createdOnUtc.AddHours(24)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithInvalidIdempotencyKey_ThrowsArgumentException(
        string idempotencyKey)
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                idempotencyKey,
                "CreateLoan",
                new string('A', 64),
                createdOnUtc,
                createdOnUtc.AddHours(24)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidOperation_ThrowsArgumentException(
        string operation)
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                "ABC-123",
                operation,
                new string('A', 64),
                createdOnUtc,
                createdOnUtc.AddHours(24)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidRequestHash_ThrowsArgumentException(
        string requestHash)
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                "ABC-123",
                "CreateLoan",
                requestHash,
                createdOnUtc,
                createdOnUtc.AddHours(24)));
    }

    [Fact]
    public void Create_WithInvalidRequestHashLength_ThrowsArgumentException()
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                "ABC-123",
                "CreateLoan",
                "ABC",
                createdOnUtc,
                createdOnUtc.AddHours(24)));
    }

    [Fact]
    public void Create_WhenExpirationEqualsCreation_ThrowsArgumentException()
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                "ABC-123",
                "CreateLoan",
                new string('A', 64),
                createdOnUtc,
                createdOnUtc));
    }

    [Fact]
    public void Create_WhenExpirationBeforeCreation_ThrowsArgumentException()
    {
        var createdOnUtc = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            IdempotencyRequest.Create(
                Guid.NewGuid(),
                "ABC-123",
                "CreateLoan",
                new string('A', 64),
                createdOnUtc,
                createdOnUtc.AddMinutes(-1)));
    }
}
