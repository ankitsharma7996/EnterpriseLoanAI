namespace LoanService.Application.Abstractions.Context;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
}