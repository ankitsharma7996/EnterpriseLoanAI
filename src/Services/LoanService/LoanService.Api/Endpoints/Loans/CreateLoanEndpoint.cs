using LoanService.Api.Contracts.Loans;
using LoanService.Application.Loans.CreateLoan;
using LoanService.Domain.Loans;
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
                    HttpContext httpContext,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.LoanNumber))
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Loan number is required."
                        });
                    }

                    if (request.LoanNumber.Trim().Length >
                        Loan.MaximumLoanNumberLength)
                    {
                        return Results.BadRequest(new
                        {
                            Error = $"Loan number cannot exceed {Loan.MaximumLoanNumberLength} characters."
                        });
                    }

                    if (request.CustomerId == Guid.Empty)
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Customer ID is required."
                        });
                    }

                    if (request.RequestedAmount <= 0)
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Requested amount must be greater than zero."
                        });
                    }

                    if (request.RequestedAmount >
                        Loan.MaximumRequestedAmount)
                    {
                        return Results.BadRequest(new
                        {
                            Error = $"Requested amount cannot exceed {Loan.MaximumRequestedAmount}."
                        });
                    }

                    if (decimal.Round(request.RequestedAmount, 2) !=
                        request.RequestedAmount)
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Requested amount cannot have more than two decimal places."
                        });
                    }

                    if (string.IsNullOrWhiteSpace(request.Currency))
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Currency is required."
                        });
                    }


                    var normalizedCurrency =
                        request.Currency.Trim().ToUpperInvariant();

                    if (normalizedCurrency.Length != Loan.CurrencyLength ||
                        !normalizedCurrency.All(character =>
                            character is >= 'A' and <= 'Z'))
                    {
                        return Results.BadRequest(new
                        {
                            Error = "Currency must contain exactly three letters."
                        });
                    }

                    var correlationId = TryGetCorrelationId(httpContext.Request.Headers) ?? Guid.NewGuid();

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

    private static Guid? TryGetCorrelationId(IHeaderDictionary headers)
    {
        const string headerName = "X-Correlation-ID";

        if (!headers.TryGetValue(headerName, out var value))
        {
            return null;
        }

        return Guid.TryParse(value.ToString(), out var correlationId)
            ? correlationId
            : null;
    }
}
