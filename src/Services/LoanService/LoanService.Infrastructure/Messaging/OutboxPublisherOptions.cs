namespace LoanService.Infrastructure.Messaging;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public bool Enabled { get; init; }

    public int BatchSize { get; init; } = 50;

    public int PollingIntervalSeconds { get; init; } = 5;

    public int MaximumRetryCount { get; init; } = 10;

    public int ProcessingTimeoutSeconds { get; init; } = 300;
}
