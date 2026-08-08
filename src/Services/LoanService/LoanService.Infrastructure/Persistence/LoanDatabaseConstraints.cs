namespace LoanService.Infrastructure.Persistence;

internal static class LoanDatabaseConstraints
{
    public const int AmountPrecision = 18;
    public const int AmountScale = 2;
    public const decimal MaximumAmount = 9999999999999999.99m;
}
