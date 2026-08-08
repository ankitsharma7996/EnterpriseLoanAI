using LoanService.Api.Contracts.Loans;
using LoanService.Api.Idempotency;
using LoanService.Application.Abstractions.Context;
using LoanService.Application.Loans.CreateLoan;
using MediatR;

namespace LoanService.Api.Endpoints.Loans;

public static class CreateLoanEndpoint
{
    public static IEndpointRouteBuilder MapCreateLoanEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/loans",
            async (
                CreateLoanRequest request,
                HttpRequest httpRequest,
                ISender sender,
                ICorrelationContext correlationContext,
                CancellationToken cancellationToken) =>
            {
                _ = IdempotencyKeyResolver.Resolve(httpRequest);

                var correlationId =
                    correlationContext.CorrelationId;

                var command = new CreateLoanCommand(
                    request.LoanNumber,
                    request.CustomerId,
                    request.RequestedAmount,
                    request.Currency,
                    correlationId);

                var result = await sender.Send(
                    command,
                    cancellationToken);

                return Results.Created(
                    $"/api/loans/{result.LoanId}",
                    result);
            })
            .WithName("CreateLoan")
            .WithTags("Loans")
            .Produces<CreateLoanResult>(
                StatusCodes.Status201Created)
            .Produces(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status409Conflict);

        return endpoints;
    }
}
