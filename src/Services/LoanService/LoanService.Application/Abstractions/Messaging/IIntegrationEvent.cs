namespace LoanService.Application.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    Guid AggregateId { get; }

    long AggregateVersion { get; }

    Guid CorrelationId { get; }

    DateTimeOffset OccurredOnUtc { get; }

    string EventVersion { get; }
}