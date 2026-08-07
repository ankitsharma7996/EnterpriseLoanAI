using LoanService.Domain.Loans;

namespace LoanService.Application.Abstractions.Persistence;

public interface ILoanRepository
{
    Task<bool> ExistsByLoanNumberAsync(
        string loanNumber,
        CancellationToken cancellationToken);

    Task AddAsync(
        Loan loan,
        CancellationToken cancellationToken);
}