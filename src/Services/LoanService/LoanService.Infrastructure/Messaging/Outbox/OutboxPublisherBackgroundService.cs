using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanService.Infrastructure.Messaging.Outbox;

internal sealed class OutboxPublisherBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxPublisherOptions> options,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Outbox Publisher is disabled.");

            return;
        }

        _logger.LogInformation(
            "Outbox Publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var processor =
                    scope.ServiceProvider
                        .GetRequiredService<OutboxProcessor>();

                await processor.RecoverStaleClaimsAsync(
                    stoppingToken);

                var processedCount =
                    await processor.ProcessBatchAsync(
                        stoppingToken);

                if (processedCount == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            _options.PollingIntervalSeconds),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected Outbox Publisher failure.");

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.PollingIntervalSeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Outbox Publisher stopped.");
    }
}
