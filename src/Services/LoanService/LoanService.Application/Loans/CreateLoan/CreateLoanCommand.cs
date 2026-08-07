using MediatR;

namespace LoanService.Application.Loans.CreateLoan;

public sealed record CreateLoanCommand(
    string LoanNumber,
    Guid CustomerId,
    decimal RequestedAmount,
    string Currency,
    Guid CorrelationId) : IRequest<CreateLoanResult>;