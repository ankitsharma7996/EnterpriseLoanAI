using LoanService.Application.Abstractions.Persistence;
using LoanService.Domain.Loans;
using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Persistence;

public sealed class LoanDbContext : DbContext, IUnitOfWork
{
    public LoanDbContext(
        DbContextOptions<LoanDbContext> options)
        : base(options)
    {
    }

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LoanDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}