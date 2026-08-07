using LoanService.Application.Abstractions.Persistence;
using LoanService.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Persistence.Repositories;

internal sealed class LoanRepository : ILoanRepository
{
    private readonly LoanDbContext _dbContext;

    public LoanRepository(LoanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByLoanNumberAsync(
        string loanNumber,
        CancellationToken cancellationToken)
    {
        return _dbContext.Loans.AnyAsync(
            loan => loan.LoanNumber == loanNumber,
            cancellationToken);
    }

    public async Task AddAsync(
        Loan loan,
        CancellationToken cancellationToken)
    {
        await _dbContext.Loans.AddAsync(
            loan,
            cancellationToken);
    }
}