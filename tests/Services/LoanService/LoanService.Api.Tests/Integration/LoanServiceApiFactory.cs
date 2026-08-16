using LoanService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LoanService.Api.Tests.Integration;

internal sealed class LoanServiceApiFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:LoanDatabase",
            "Server=(localdb)\\mssqllocaldb;Database=Test;");
        builder.ConfigureServices(services =>
        {
            services.AddLogging(logging => logging.ClearProviders());
            services.RemoveAll<DbContextOptions<LoanDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<LoanDbContext>>();
            services.AddDbContext<LoanDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
