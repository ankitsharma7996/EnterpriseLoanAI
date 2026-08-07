using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanService.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.AggregateId)
            .IsRequired();

        builder.Property(message => message.AggregateVersion)
            .IsRequired();

        builder.Property(message => message.CorrelationId)
            .IsRequired();

        builder.Property(message => message.EventType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(message => message.EventVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(message => message.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(message => message.LockedBy)
            .HasMaxLength(200);

        builder.Property(message => message.LastError)
            .HasMaxLength(4000);

        builder.HasIndex(message => new
        {
            message.Status,
            message.NextAttemptOnUtc,
            message.OccurredOnUtc
        })
        .HasDatabaseName("IX_OutboxMessages_Publishing");

        builder.HasIndex(message => new
        {
            message.AggregateId,
            message.AggregateVersion
        })
        .HasDatabaseName("IX_OutboxMessages_Aggregate");

        builder.HasIndex(message => message.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");

        builder.HasIndex(message => message.PublishedOnUtc)
            .HasDatabaseName("IX_OutboxMessages_PublishedOnUtc");
    }
}