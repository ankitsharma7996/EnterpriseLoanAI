using LoanService.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanService.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration
    : IEntityTypeConfiguration<Loan>
{
    public void Configure(
        EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(loan => loan.Id);

        builder.Property(loan => loan.LoanNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(loan => loan.LoanNumber)
            .IsUnique();

        builder.Property(loan => loan.RequestedAmount)
            .HasPrecision(
                LoanDatabaseConstraints.AmountPrecision,
                LoanDatabaseConstraints.AmountScale)
            .IsRequired();

        builder.Property(loan => loan.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(loan => loan.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(loan => loan.Version)
            .IsConcurrencyToken();

        builder.Property(loan => loan.CreatedOnUtc)
            .IsRequired();
    }
}
