using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using LoanService.Application.Abstractions.Validation;

namespace LoanService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationValidationException exception)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(new
            {
                Error = exception.Message,
                exception.Errors
            });
        }
        catch (InvalidOperationException exception)
        {
            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await context.Response.WriteAsJsonAsync(new
            {
                Error = exception.Message
            });
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await context.Response.WriteAsJsonAsync(new
            {
                Error = "A loan with the same loan number already exists."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unhandled error occurred while processing the request.");

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(new
            {
                Error = "An unexpected error occurred."
            });
        }
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException
        {
            Number: 2601 or 2627
        };
    }
}
