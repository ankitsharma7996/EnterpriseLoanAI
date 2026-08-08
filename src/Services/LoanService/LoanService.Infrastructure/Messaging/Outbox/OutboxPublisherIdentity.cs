using Microsoft.Extensions.Options;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class OutboxPublisherIdentity
{
    public OutboxPublisherIdentity(IOptions<OutboxPublisherOptions> options)
        : this(CreateValue(options.Value.PublisherId))
    {
    }

    internal OutboxPublisherIdentity(string value)
    {
        Value = value;
    }

    public string Value { get; }

    private static string CreateValue(string? configuredValue)
    {
        return !string.IsNullOrWhiteSpace(configuredValue)
            ? configuredValue.Trim()
            : $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }
}
