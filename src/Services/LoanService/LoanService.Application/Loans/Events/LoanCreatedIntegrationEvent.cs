using LoanService.Application.Abstractions.Messaging;

namespace LoanService.Application.Loans.Events;

public sealed record LoanCreatedIntegrationEvent
    : IIntegrationEvent
{
    public required Guid EventId { get; init; }

    public required Guid LoanId { get; init; }

    public required Guid CustomerId { get; init; }

    public required string LoanNumber { get; init; }

    public required decimal RequestedAmount { get; init; }

    public required string Currency { get; init; }

    public required string Status { get; init; }

    public required Guid CorrelationId { get; init; }

    public required long AggregateVersion { get; init; }

    public required DateTimeOffset OccurredOnUtc { get; init; }

    public string EventVersion => "1.0";

    public Guid AggregateId => LoanId;
}