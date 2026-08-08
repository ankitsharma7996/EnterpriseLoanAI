namespace LoanService.Domain.Loans;

public readonly record struct Currency
{
    private static readonly HashSet<string> SupportedCodes =
        new(StringComparer.Ordinal)
        {
            "AED", "AUD", "CAD", "CHF", "EUR",
            "GBP", "INR", "JPY", "SGD", "USD"
        };

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static bool IsSupported(string? code)
    {
        return code is not null &&
            SupportedCodes.Contains(code.Trim().ToUpperInvariant());
    }

    public static Currency Create(string code)
    {
        if (!IsSupported(code))
        {
            throw new ArgumentException(
                "Currency must be a supported ISO-4217 code.",
                nameof(code));
        }

        return new Currency(code.Trim().ToUpperInvariant());
    }

    public override string ToString() => Code;
}
