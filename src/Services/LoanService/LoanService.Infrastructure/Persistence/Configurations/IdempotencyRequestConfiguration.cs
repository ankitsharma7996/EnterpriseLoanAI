using LoanService.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanService.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRequestConfiguration
    : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(
        EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("IdempotencyRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Operation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ResponseBody)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresOnUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.Operation,
            x.IdempotencyKey
        })
        .IsUnique();

        builder.HasIndex(x => x.ExpiresOnUtc);
    }
}
