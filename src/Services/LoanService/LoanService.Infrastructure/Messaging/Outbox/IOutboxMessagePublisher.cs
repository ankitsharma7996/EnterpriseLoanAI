using LoanService.Infrastructure.Persistence.Outbox;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal interface IOutboxMessagePublisher
{
    Task PublishAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken);
}
