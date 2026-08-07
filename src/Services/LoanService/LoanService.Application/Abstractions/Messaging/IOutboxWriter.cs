namespace LoanService.Application.Abstractions.Messaging;

public interface IOutboxWriter
{
    Task AddAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}