namespace LoanService.Application.Abstractions.Idempotency;

public interface IRequestHasher
{
    string ComputeHash<TRequest>(TRequest request);
}