namespace LoanService.Application.Abstractions.Validation;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
