using System.Net;
using System.Net.Http.Json;
using LoanService.Api.Contracts.Loans;
using LoanService.Api.Middleware;
using LoanService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LoanService.Api.Tests.Integration;

public sealed class CorrelationIdPersistenceTests
{
    [Fact]
    public async Task SuppliedCorrelationId_IsPersistedToOutbox()
    {
        // Arrange
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        var correlationId = Guid.NewGuid();
        using var request = CreateLoanRequest();
        request.Headers.Add(
            CorrelationIdMiddleware.HeaderName,
            correlationId.ToString());

        // Act
        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            correlationId,
            await GetPersistedCorrelationIdAsync(factory));
    }

    [Fact]
    public async Task MissingCorrelationId_GeneratesAndPersistsCorrelationId()
    {
        // Arrange
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();

        // Act
        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseCorrelationId = Guid.Parse(
            response.Headers.GetValues(
                CorrelationIdMiddleware.HeaderName).Single());
        Assert.NotEqual(Guid.Empty, responseCorrelationId);
        Assert.Equal(
            responseCorrelationId,
            await GetPersistedCorrelationIdAsync(factory));
    }

    private static HttpRequestMessage CreateLoanRequest()
    {
        var payload = new CreateLoanRequest(
            $"ELAI-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            100_000m,
            "USD");

        return new HttpRequestMessage(HttpMethod.Post, "/api/loans")
        {
            Content = JsonContent.Create(payload)
        };
    }

    private static async Task<Guid> GetPersistedCorrelationIdAsync(
        LoanServiceApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<LoanDbContext>();

        return await dbContext.OutboxMessages
            .Select(message => message.CorrelationId)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private sealed class LoanServiceApiFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:LoanDatabase",
                "Server=(localdb)\\mssqllocaldb;Database=Test;");
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());
                services.RemoveAll<DbContextOptions<LoanDbContext>>();
                services.RemoveAll<
                    IDbContextOptionsConfiguration<LoanDbContext>>();
                services.AddDbContext<LoanDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
