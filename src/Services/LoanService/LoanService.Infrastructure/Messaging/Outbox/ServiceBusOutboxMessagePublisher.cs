using Azure.Messaging.ServiceBus;
using LoanService.Infrastructure.Persistence.Outbox;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class ServiceBusOutboxMessagePublisher
    : IOutboxMessagePublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusOutboxMessagePublisher(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public Task PublishAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(
            BinaryData.FromString(outboxMessage.Payload))
        {
            MessageId = outboxMessage.Id.ToString(),
            CorrelationId = outboxMessage.CorrelationId.ToString(),
            Subject = outboxMessage.EventType,
            ContentType = "application/json",
            SessionId = $"loan:{outboxMessage.AggregateId}"
        };

        message.ApplicationProperties["eventType"] =
            outboxMessage.EventType;
        message.ApplicationProperties["eventVersion"] =
            outboxMessage.EventVersion;
        message.ApplicationProperties["aggregateId"] =
            outboxMessage.AggregateId.ToString();
        message.ApplicationProperties["aggregateVersion"] =
            outboxMessage.AggregateVersion;
        message.ApplicationProperties["occurredOnUtc"] =
            outboxMessage.OccurredOnUtc.ToString("O");

        return _sender.SendMessageAsync(message, cancellationToken);
    }
}
