using Azure.Messaging.ServiceBus;
using LoanService.Application.Abstractions.Messaging;
using LoanService.Application.Abstractions.Persistence;
using LoanService.Infrastructure.Messaging;
using LoanService.Infrastructure.Messaging.Outbox;
using LoanService.Infrastructure.Persistence;
using LoanService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("LoanDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'LoanDatabase' was not found.");

        services.AddDbContext<LoanDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<LoanDbContext>());

        services
            .AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection(
                ServiceBusOptions.SectionName));

        services
            .AddOptions<OutboxPublisherOptions>()
            .Bind(configuration.GetSection(
                OutboxPublisherOptions.SectionName));

        var publisherOptions = configuration
            .GetSection(OutboxPublisherOptions.SectionName)
            .Get<OutboxPublisherOptions>()
            ?? new OutboxPublisherOptions();

        if (!publisherOptions.Enabled)
        {
            return services;
        }

        var serviceBusConnectionString =
            configuration.GetConnectionString("AzureServiceBus");

        if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'AzureServiceBus' is required " +
                "when OutboxPublisher is enabled.");
        }

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>()
            ?? throw new InvalidOperationException(
                "Service Bus configuration was not found.");

        if (string.IsNullOrWhiteSpace(
            serviceBusOptions.LoanEventsTopicName))
        {
            throw new InvalidOperationException(
                "Service Bus topic name is required.");
        }

        services.AddSingleton(
            new ServiceBusClient(serviceBusConnectionString));

        services.AddSingleton(serviceProvider =>
        {
            var client =
                serviceProvider.GetRequiredService<ServiceBusClient>();

            return client.CreateSender(
                serviceBusOptions.LoanEventsTopicName);
        });

        services.AddScoped<OutboxProcessor>();

        services.AddHostedService<
            OutboxPublisherBackgroundService>();

        return services;
    }
}