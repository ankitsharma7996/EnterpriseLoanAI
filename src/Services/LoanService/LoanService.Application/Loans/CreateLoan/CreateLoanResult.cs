namespace LoanService.Application.Loans.CreateLoan;

public sealed record CreateLoanResult(
    Guid LoanId,
    string LoanNumber,
    string Status);