namespace LoanService.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string LoanEventsTopicName { get; init; } = string.Empty;
}