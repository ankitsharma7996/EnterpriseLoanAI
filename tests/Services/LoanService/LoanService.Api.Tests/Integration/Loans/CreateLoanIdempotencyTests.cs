using LoanService.Api.Contracts.Loans;
using LoanService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LoanService.Api.Tests.Integration.Loans;

public sealed class CreateLoanIdempotencyTests
{
    [Fact]
    public async Task CreateLoan_WithNewIdempotencyKey_ReturnsCreated()
    {
        // Arrange
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();

        var idempotencyKey =
            $"create-loan-{Guid.NewGuid():N}";

        var request = CreateRequest();

        // Act
        using var response = await client.SendAsync(
            CreateHttpRequest(
                request,
                idempotencyKey),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WithSameIdempotencyKeyAndSamePayload_ReturnsConflict()
    {
        // Arrange
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();

        var idempotencyKey =
            $"create-loan-{Guid.NewGuid():N}";

        var request = CreateRequest();

        // Act
        using var firstResponse = await client.SendAsync(
            CreateHttpRequest(
                request,
                idempotencyKey),
            TestContext.Current.CancellationToken);

        using var secondResponse = await client.SendAsync(
            CreateHttpRequest(
                request,
                idempotencyKey),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        await AssertSingleLoanAndIdempotencyRecordAsync(factory);
    }

    [Fact]
    public async Task CreateLoan_WithSameIdempotencyKeyAndDifferentPayload_ReturnsConflict()
    {
        // Arrange
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();

        var idempotencyKey =
            $"create-loan-{Guid.NewGuid():N}";

        var customerId = Guid.NewGuid();

        var loanNumber =
            $"LN-{Guid.NewGuid():N}";

        var firstRequest = new CreateLoanRequest(
            loanNumber,
            customerId,
            3_500_000m,
            "INR");

        var changedRequest = new CreateLoanRequest(
            loanNumber,
            customerId,
            5_000_000m,
            "INR");

        // Act
        using var firstResponse = await client.SendAsync(
            CreateHttpRequest(
                firstRequest,
                idempotencyKey),
            TestContext.Current.CancellationToken);

        using var secondResponse = await client.SendAsync(
            CreateHttpRequest(
                changedRequest,
                idempotencyKey),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        await AssertSingleLoanAndIdempotencyRecordAsync(factory);
    }

    private static CreateLoanRequest CreateRequest()
    {
        return new CreateLoanRequest(
            $"LN-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            3_500_000m,
            "INR");
    }

    private static HttpRequestMessage CreateHttpRequest(
        CreateLoanRequest request,
        string idempotencyKey)
    {
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/loans")
        {
            Content = JsonContent.Create(request)
        };

        message.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        message.Headers.Add(
            "X-Correlation-ID",
            Guid.NewGuid().ToString());

        return message;
    }

    private static async Task AssertSingleLoanAndIdempotencyRecordAsync(
        LoanServiceApiFactory factory)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<LoanDbContext>();

        var loanCount =
            await dbContext.Loans.CountAsync(
                TestContext.Current.CancellationToken);

        var idempotencyCount =
            await dbContext.IdempotencyRequests.CountAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(1, loanCount);
        Assert.Equal(1, idempotencyCount);
    }

    private sealed class LoanServiceApiFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName =
            Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "ConnectionStrings:LoanDatabase",
                "Server=(localdb)\\mssqllocaldb;Database=Test;");

            builder.ConfigureServices(services =>
            {
                services.AddLogging(
                    logging => logging.ClearProviders());

                services.RemoveAll<
                    DbContextOptions<LoanDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<LoanDbContext>>();

                services.AddDbContext<LoanDbContext>(
                    options =>
                        options.UseInMemoryDatabase(
                            _databaseName));
            });
        }
    }
}
