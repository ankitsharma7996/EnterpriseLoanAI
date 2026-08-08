using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class OutboxProcessor
{
    private readonly IOutboxStore _store;
    private readonly IOutboxMessagePublisher _publisher;
    private readonly OutboxPublisherOptions _options;
    private readonly OutboxPublisherIdentity _identity;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxStore store,
        IOutboxMessagePublisher publisher,
        IOptions<OutboxPublisherOptions> options,
        OutboxPublisherIdentity identity,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        _store = store;
        _publisher = publisher;
        _options = options.Value;
        _identity = identity;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        var messages = await _store.ClaimBatchAsync(
            _identity.Value,
            _timeProvider.GetUtcNow(),
            _options.BatchSize,
            _options.MaximumRetryCount,
            cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        return messages.Count;
    }

    public async Task<int> RecoverStaleClaimsAsync(
        CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().Subtract(
            TimeSpan.FromSeconds(_options.ProcessingTimeoutSeconds));

        var recovered = await _store.RecoverStaleClaimsAsync(
            cutoff,
            cancellationToken);

        if (recovered > 0)
        {
            _logger.LogWarning(
                "Recovered {Count} stale outbox claims.",
                recovered);
        }

        return recovered;
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(message, cancellationToken);
            message.MarkAsPublished(_timeProvider.GetUtcNow());
            await _store.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Published outbox message {EventId} for aggregate {AggregateId}.",
                message.Id,
                message.AggregateId);
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            var nextRetryCount = message.RetryCount + 1;

            if (nextRetryCount >= _options.MaximumRetryCount)
            {
                message.MarkAsPermanentlyFailed(exception.ToString());
                _logger.LogError(
                    exception,
                    "Outbox message {EventId} permanently failed after {RetryCount} attempts.",
                    message.Id,
                    nextRetryCount);
            }
            else
            {
                var delay = CalculateRetryDelay(nextRetryCount);
                message.ScheduleRetry(
                    exception.ToString(),
                    _timeProvider.GetUtcNow().Add(delay));
                _logger.LogWarning(
                    exception,
                    "Outbox message {EventId} failed. Retry {RetryCount} scheduled after {Delay}.",
                    message.Id,
                    nextRetryCount,
                    delay);
            }

            await _store.SaveChangesAsync(cancellationToken);
        }
    }

    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        var seconds = Math.Min(Math.Pow(2, retryCount), 300);
        return TimeSpan.FromSeconds(seconds)
            + TimeSpan.FromMilliseconds(Random.Shared.Next(100, 1000));
    }
}
