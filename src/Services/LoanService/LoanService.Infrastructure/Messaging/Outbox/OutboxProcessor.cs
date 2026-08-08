using Azure.Messaging.ServiceBus;
using System.Data;
using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class OutboxProcessor
{
    private readonly LoanDbContext _dbContext;
    private readonly ServiceBusSender _sender;
    private readonly OutboxPublisherOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        LoanDbContext dbContext,
        ServiceBusSender sender,
        IOptions<OutboxPublisherOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        var publisherId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        var messages = await ClaimBatchAsync(
            publisherId,
            cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        foreach (var outboxMessage in messages)
        {
            await ProcessMessageAsync(
                outboxMessage,
                cancellationToken);
        }

        return messages.Count;
    }

    private async Task<List<OutboxMessage>> ClaimBatchAsync(
        string publisherId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var lockExpiredBefore = now.Subtract(
            TimeSpan.FromSeconds(_options.ProcessingTimeoutSeconds));

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var messages = await _dbContext.OutboxMessages
            .Where(message =>
                (message.Status == OutboxMessageStatus.Pending &&
                 (message.NextAttemptOnUtc == null ||
                  message.NextAttemptOnUtc <= now)) ||
                (message.Status == OutboxMessageStatus.Processing &&
                 message.ProcessingStartedOnUtc <= lockExpiredBefore))
            .Where(message =>
                message.RetryCount < _options.MaximumRetryCount)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (message.Status == OutboxMessageStatus.Processing)
            {
                message.ResetForRetry();
            }

            message.MarkAsProcessing(publisherId, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    private async Task ProcessMessageAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceBusMessage =
                CreateServiceBusMessage(outboxMessage);

            await _sender.SendMessageAsync(
                serviceBusMessage,
                cancellationToken);

            outboxMessage.MarkAsPublished(
                _timeProvider.GetUtcNow());

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Published outbox message {EventId} " +
                "for aggregate {AggregateId}.",
                outboxMessage.Id,
                outboxMessage.AggregateId);
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            var nextRetryCount = outboxMessage.RetryCount + 1;

            if (nextRetryCount >= _options.MaximumRetryCount)
            {
                outboxMessage.MarkAsPermanentlyFailed(
                    exception.ToString());

                _logger.LogError(
                    exception,
                    "Outbox message {EventId} permanently failed " +
                    "after {RetryCount} attempts.",
                    outboxMessage.Id,
                    nextRetryCount);
            }
            else
            {
                var delay = CalculateRetryDelay(nextRetryCount);

                outboxMessage.ScheduleRetry(
                    exception.ToString(),
                    _timeProvider.GetUtcNow().Add(delay));

                _logger.LogWarning(
                    exception,
                    "Outbox message {EventId} failed. " +
                    "Retry {RetryCount} scheduled after {Delay}.",
                    outboxMessage.Id,
                    nextRetryCount,
                    delay);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }

    private static ServiceBusMessage CreateServiceBusMessage(
        OutboxMessage outboxMessage)
    {
        var message = new ServiceBusMessage(
            BinaryData.FromString(outboxMessage.Payload))
        {
            MessageId = outboxMessage.Id.ToString(),
            CorrelationId = outboxMessage.CorrelationId.ToString(),
            Subject = outboxMessage.EventType,
            ContentType = "application/json",

            // All events belonging to one Loan share the same session.
            SessionId = $"loan:{outboxMessage.AggregateId}"
        };

        message.ApplicationProperties["eventType"] =
            outboxMessage.EventType;

        message.ApplicationProperties["eventVersion"] =
            outboxMessage.EventVersion;

        message.ApplicationProperties["aggregateId"] =
            outboxMessage.AggregateId.ToString();

        message.ApplicationProperties["aggregateVersion"] =
            outboxMessage.AggregateVersion;

        message.ApplicationProperties["occurredOnUtc"] =
            outboxMessage.OccurredOnUtc.ToString("O");

        return message;
    }

    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        var exponentialSeconds = Math.Pow(2, retryCount);

        var cappedSeconds = Math.Min(
            exponentialSeconds,
            300);

        var jitterMilliseconds =
            Random.Shared.Next(100, 1000);

        return TimeSpan.FromSeconds(cappedSeconds)
            + TimeSpan.FromMilliseconds(jitterMilliseconds);
    }
}
