using LoanService.Api.Contracts.Loans;
using LoanService.Api.Idempotency;
using LoanService.Application.Abstractions.Context;
using LoanService.Application.Abstractions.Idempotency;
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
                IIdempotencyService idempotencyService,
                IRequestHasher requestHasher,
                CancellationToken cancellationToken) =>
            {
                // 1. Get and validate Idempotency-Key from HTTP header
                var idempotencyKey =
                    IdempotencyKeyResolver.Resolve(httpRequest);

                // 2. Create normalized representation for hashing
                var hashInput = new
                {
                    LoanNumber =
                        request.LoanNumber
                            .Trim()
                            .ToUpperInvariant(),

                    request.CustomerId,

                    request.RequestedAmount,

                    Currency =
                        request.Currency
                            .Trim()
                            .ToUpperInvariant()
                };

                // 3. Calculate deterministic request hash
                var requestHash =
                    requestHasher.ComputeHash(hashInput);

                // 4. Try to acquire this Idempotency-Key
                var acquireResult =
                    await idempotencyService.TryAcquireAsync(
                        idempotencyKey,
                        CreateLoanOperation.Name,
                        requestHash,
                        cancellationToken);

                // 5. Decide whether command is allowed to execute
                switch (acquireResult.Status)
                {
                    case IdempotencyAcquireStatus.RequestMismatch:
                        return Results.Conflict(new
                        {
                            Error =
                                "The Idempotency-Key has already been used " +
                                "with a different request."
                        });

                    case IdempotencyAcquireStatus.AlreadyProcessing:
                        return Results.Conflict(new
                        {
                            Error =
                                "A request with this Idempotency-Key " +
                                "is already being processed."
                        });

                    case IdempotencyAcquireStatus.AlreadyCompleted:
                        return Results.Conflict(new
                        {
                            Error =
                                "A request with this Idempotency-Key " +
                                "has already been completed."
                        });

                    case IdempotencyAcquireStatus.Acquired:
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported idempotency status: " +
                            $"{acquireResult.Status}.");
                }

                // 6. Correlation ID already established by middleware
                var correlationId =
                    correlationContext.CorrelationId;

                // 7. Only the request that ACQUIRED the key
                // is allowed to create the Loan
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