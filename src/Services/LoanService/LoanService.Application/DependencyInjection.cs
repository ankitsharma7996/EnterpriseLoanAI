using Microsoft.Extensions.DependencyInjection;

namespace LoanService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly));

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}