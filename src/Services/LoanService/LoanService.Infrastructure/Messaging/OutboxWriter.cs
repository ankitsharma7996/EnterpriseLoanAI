using System.Text.Json;
using LoanService.Application.Abstractions.Messaging;
using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Outbox;

namespace LoanService.Infrastructure.Messaging;

internal sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly LoanDbContext _dbContext;

    public OutboxWriter(LoanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var runtimeType = integrationEvent.GetType();

        var eventType =
            runtimeType.FullName
            ?? throw new InvalidOperationException(
                "Unable to determine the integration event type.");

        var payload = JsonSerializer.Serialize(
            integrationEvent,
            runtimeType,
            SerializerOptions);

        var outboxMessage = OutboxMessage.Create(
            integrationEvent.EventId,
            integrationEvent.AggregateId,
            integrationEvent.AggregateVersion,
            integrationEvent.CorrelationId,
            eventType,
            integrationEvent.EventVersion,
            payload,
            integrationEvent.OccurredOnUtc);

        await _dbContext.OutboxMessages.AddAsync(
            outboxMessage,
            cancellationToken);
    }
}