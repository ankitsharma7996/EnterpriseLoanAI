namespace LoanService.Infrastructure.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending = 1,
    Processing = 2,
    Published = 3,
    Failed = 4
}