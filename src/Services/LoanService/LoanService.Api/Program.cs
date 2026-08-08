using LoanService.Api.Endpoints.Loans;
using LoanService.Api.Middleware;
using LoanService.Application;
using LoanService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    Service = "EnterpriseLoanAI Loan Service",
    Status = "Running"
}));

app.MapCreateLoanEndpoint();

app.Run();