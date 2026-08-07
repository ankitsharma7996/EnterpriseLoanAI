namespace LoanService.Api.Contracts.Loans;

public sealed record CreateLoanRequest(
    string LoanNumber,
    Guid CustomerId,
    decimal RequestedAmount,
    string Currency);