namespace LoanService.Domain.Loans;

public sealed class Loan
{
    private Loan()
    {
    }

    private Loan(
        Guid id,
        string loanNumber,
        Guid customerId,
        decimal requestedAmount,
        string currency,
        DateTimeOffset createdOnUtc)
    {
        Id = id;
        LoanNumber = loanNumber;
        CustomerId = customerId;
        RequestedAmount = requestedAmount;
        Currency = currency;
        Status = LoanStatus.Draft;
        CreatedOnUtc = createdOnUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string LoanNumber { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public decimal RequestedAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public LoanStatus Status { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public long Version { get; private set; }

    public static Loan Create(
        Guid id,
        string loanNumber,
        Guid customerId,
        decimal requestedAmount,
        string currency,
        DateTimeOffset createdOnUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Loan ID cannot be empty.",
                nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer ID cannot be empty.",
                nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(loanNumber))
        {
            throw new ArgumentException(
                "Loan number is required.",
                nameof(loanNumber));
        }

        if (requestedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedAmount),
                "Requested amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        return new Loan(
            id,
            loanNumber.Trim(),
            customerId,
            requestedAmount,
            currency.Trim().ToUpperInvariant(),
            createdOnUtc);
    }
}