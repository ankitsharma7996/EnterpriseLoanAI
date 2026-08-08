using Microsoft.Extensions.DependencyInjection;

using FluentValidation;
using LoanService.Application.Abstractions.Validation;

namespace LoanService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(
                typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
