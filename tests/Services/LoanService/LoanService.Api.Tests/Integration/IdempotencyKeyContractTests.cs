using System.Net;
using System.Net.Http.Json;
using LoanService.Api.Contracts.Loans;
using LoanService.Api.Idempotency;
using Xunit;

namespace LoanService.Api.Tests.Integration;

public sealed class IdempotencyKeyContractTests
{
    [Fact]
    public async Task Missing_Idempotency_Key_Returns_400()
    {
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_Idempotency_Key_Returns_400()
    {
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();
        request.Headers.TryAddWithoutValidation(
            IdempotencyHeaders.HeaderName,
            string.Empty);

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Idempotency_Key_Over_128_Characters_Returns_400()
    {
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();
        request.Headers.Add(
            IdempotencyHeaders.HeaderName,
            new string('a', IdempotencyHeaders.MaximumLength + 1));

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Valid_UUID_Idempotency_Key_Continues_Request()
    {
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();
        request.Headers.Add(
            IdempotencyHeaders.HeaderName,
            Guid.NewGuid().ToString());

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Non_GUID_Opaque_Idempotency_Key_Is_Accepted()
    {
        await using var factory = new LoanServiceApiFactory();
        using var client = factory.CreateClient();
        using var request = CreateLoanRequest();
        request.Headers.Add(
            IdempotencyHeaders.HeaderName,
            "loan-request-from-mobile-client-42");

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
}
