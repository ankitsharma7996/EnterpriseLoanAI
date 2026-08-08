using LoanService.Api.Context;
using LoanService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LoanService.Api.Tests.Context;

public sealed class HttpCorrelationContextTests
{
    [Fact]
    public void HttpCorrelationContext_ReturnsInitializedCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey] =
            correlationId;
        var context = CreateCorrelationContext(httpContext);

        // Act
        var result = context.CorrelationId;

        // Assert
        Assert.Equal(correlationId, result);
    }

    [Fact]
    public void HttpCorrelationContext_ThrowsWhenCorrelationIdWasNotInitialized()
    {
        // Arrange
        var context = CreateCorrelationContext(new DefaultHttpContext());

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => context.CorrelationId);

        // Assert
        Assert.Equal(
            "Correlation ID was not initialized.",
            exception.Message);
    }

    private static HttpCorrelationContext CreateCorrelationContext(
        HttpContext httpContext)
    {
        return new HttpCorrelationContext(
            new HttpContextAccessor { HttpContext = httpContext });
    }
}
