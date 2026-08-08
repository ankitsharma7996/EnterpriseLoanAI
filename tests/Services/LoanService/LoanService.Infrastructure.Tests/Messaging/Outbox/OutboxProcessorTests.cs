using System.Collections.Concurrent;
using LoanService.Infrastructure.Messaging;
using LoanService.Infrastructure.Messaging.Outbox;
using LoanService.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LoanService.Infrastructure.Tests.Messaging.Outbox;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task Concurrent_publishers_publish_each_message_once()
    {
        var time = new MutableTimeProvider();
        var messages = Enumerable.Range(1, 20)
            .Select(_ => CreateMessage(time.GetUtcNow()))
            .ToArray();
        var store = new FakeOutboxStore(messages);
        var publisher = new FakePublisher();
        var options = CreateOptions(batchSize: 10);
        var first = CreateProcessor(
            store, publisher, time, options, "publisher-one");
        var second = CreateProcessor(
            store, publisher, time, options, "publisher-two");

        await Task.WhenAll(
            first.ProcessBatchAsync(TestContext.Current.CancellationToken),
            second.ProcessBatchAsync(TestContext.Current.CancellationToken));

        Assert.Equal(20, publisher.PublishedIds.Count);
        Assert.Equal(20, publisher.PublishedIds.Distinct().Count());
        Assert.All(messages, message =>
            Assert.Equal(OutboxMessageStatus.Published, message.Status));
    }

    [Fact]
    public async Task Stale_recovery_does_not_consume_publish_retry()
    {
        var time = new MutableTimeProvider();
        var message = CreateMessage(time.GetUtcNow());
        var store = new FakeOutboxStore([message]);
        await store.ClaimBatchAsync(
            "crashed", time.GetUtcNow(), 1, 3,
            TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(6));
        var processor = CreateProcessor(
            store, new FakePublisher(), time, CreateOptions(), "replacement");

        var recovered = await processor.RecoverStaleClaimsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, recovered);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextAttemptOnUtc);
    }

    [Fact]
    public async Task Retry_exhaustion_marks_message_failed()
    {
        var time = new MutableTimeProvider();
        var message = CreateMessage(time.GetUtcNow());
        var store = new FakeOutboxStore([message]);
        var publisher = new FakePublisher(alwaysFail: true);
        var processor = CreateProcessor(
            store, publisher, time,
            CreateOptions(maximumRetryCount: 2), "publisher");

        await processor.ProcessBatchAsync(
            TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(1));
        await processor.ProcessBatchAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(2, message.RetryCount);
        Assert.NotNull(message.LastError);
    }

    [Fact]
    public async Task Successful_publish_marks_message_published()
    {
        var time = new MutableTimeProvider();
        var message = CreateMessage(time.GetUtcNow());
        var publisher = new FakePublisher();
        var processor = CreateProcessor(
            new FakeOutboxStore([message]),
            publisher,
            time,
            CreateOptions(),
            "publisher");

        var count = await processor.ProcessBatchAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
        Assert.Equal([message.Id], publisher.PublishedIds);
        Assert.Equal(OutboxMessageStatus.Published, message.Status);
        Assert.NotNull(message.PublishedOnUtc);
    }

    [Fact]
    public async Task Service_Bus_failure_schedules_publish_retry()
    {
        var time = new MutableTimeProvider();
        var message = CreateMessage(time.GetUtcNow());
        var processor = CreateProcessor(
            new FakeOutboxStore([message]),
            new FakePublisher(alwaysFail: true),
            time,
            CreateOptions(maximumRetryCount: 3),
            "publisher");

        await processor.ProcessBatchAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(1, message.RetryCount);
        Assert.NotNull(message.NextAttemptOnUtc);
        Assert.Contains("Simulated Service Bus failure", message.LastError);
    }

    [Fact]
    public async Task Crash_after_claim_is_recovered_and_published()
    {
        var time = new MutableTimeProvider();
        var message = CreateMessage(time.GetUtcNow());
        var store = new FakeOutboxStore([message]);

        await store.ClaimBatchAsync(
            "crashed", time.GetUtcNow(), 1, 3,
            TestContext.Current.CancellationToken);

        var publisher = new FakePublisher();
        var replacement = CreateProcessor(
            store, publisher, time, CreateOptions(), "replacement");

        Assert.Equal(0, await replacement.ProcessBatchAsync(
            TestContext.Current.CancellationToken));

        time.Advance(TimeSpan.FromMinutes(6));
        await replacement.RecoverStaleClaimsAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(1, await replacement.ProcessBatchAsync(
            TestContext.Current.CancellationToken));

        Assert.Equal([message.Id], publisher.PublishedIds);
        Assert.Equal(OutboxMessageStatus.Published, message.Status);
        Assert.Equal(0, message.RetryCount);
    }

    private static OutboxProcessor CreateProcessor(
        IOutboxStore store,
        IOutboxMessagePublisher publisher,
        TimeProvider timeProvider,
        OutboxPublisherOptions options,
        string identity)
    {
        return new OutboxProcessor(
            store,
            publisher,
            Options.Create(options),
            new OutboxPublisherIdentity(identity),
            timeProvider,
            NullLogger<OutboxProcessor>.Instance);
    }

    private static OutboxPublisherOptions CreateOptions(
        int batchSize = 50,
        int maximumRetryCount = 3)
    {
        return new OutboxPublisherOptions
        {
            BatchSize = batchSize,
            MaximumRetryCount = maximumRetryCount,
            ProcessingTimeoutSeconds = 300
        };
    }

    private static OutboxMessage CreateMessage(DateTimeOffset occurredOnUtc)
    {
        return OutboxMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            "LoanCreated",
            "1.0",
            "{}",
            occurredOnUtc);
    }

    private sealed class FakeOutboxStore : IOutboxStore
    {
        private readonly object _gate = new();
        private readonly List<OutboxMessage> _messages;

        public FakeOutboxStore(IEnumerable<OutboxMessage> messages)
        {
            _messages = messages.ToList();
        }

        public Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
            string publisherId,
            DateTimeOffset claimedOnUtc,
            int batchSize,
            int maximumRetryCount,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var claimed = _messages
                    .Where(message =>
                        message.Status == OutboxMessageStatus.Pending &&
                        message.RetryCount < maximumRetryCount &&
                        (message.NextAttemptOnUtc is null ||
                         message.NextAttemptOnUtc <= claimedOnUtc))
                    .OrderBy(message => message.OccurredOnUtc)
                    .Take(batchSize)
                    .ToArray();

                foreach (var message in claimed)
                {
                    message.MarkAsProcessing(publisherId, claimedOnUtc);
                }

                return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
            }
        }

        public Task<int> RecoverStaleClaimsAsync(
            DateTimeOffset processingStartedBeforeUtc,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var stale = _messages.Where(message =>
                    message.Status == OutboxMessageStatus.Processing &&
                    message.ProcessingStartedOnUtc <
                    processingStartedBeforeUtc).ToArray();

                foreach (var message in stale)
                {
                    message.ResetForRetry();
                }

                return Task.FromResult(stale.Length);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublisher : IOutboxMessagePublisher
    {
        private readonly bool _alwaysFail;
        private readonly ConcurrentBag<Guid> _publishedIds = [];

        public FakePublisher(bool alwaysFail = false)
        {
            _alwaysFail = alwaysFail;
        }

        public IReadOnlyCollection<Guid> PublishedIds => _publishedIds;

        public Task PublishAsync(
            OutboxMessage outboxMessage,
            CancellationToken cancellationToken)
        {
            if (_alwaysFail)
            {
                throw new InvalidOperationException(
                    "Simulated Service Bus failure.");
            }

            _publishedIds.Add(outboxMessage.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow =
            new(2026, 8, 8, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
