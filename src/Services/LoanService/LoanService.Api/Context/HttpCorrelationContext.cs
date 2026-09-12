using LoanService.Application.Abstractions.Context;
using LoanService.Api.Middleware;

namespace LoanService.Api.Context;

public sealed class HttpCorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CorrelationId
    {
        get
        {
            var httpContext =
                _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "No active HTTP context is available.");

            if (httpContext.Items.TryGetValue(
                    CorrelationIdMiddleware.HttpContextItemKey,
                    out var value) &&
                value is Guid correlationId)
            {
                return correlationId;
            }

            throw new InvalidOperationException(
                "Correlation ID was not initialized.");
        }
    }
}
