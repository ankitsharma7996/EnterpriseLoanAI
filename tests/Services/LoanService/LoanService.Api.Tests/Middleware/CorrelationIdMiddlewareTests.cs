using LoanService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LoanService.Api.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Missing_header_generates_id_and_returns_same_id()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(async httpContext =>
            await httpContext.Response.StartAsync(
                TestContext.Current.CancellationToken));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseId = GetResponseCorrelationId(context);
        Assert.NotEqual(Guid.Empty, responseId);
        Assert.Equal(
            context.Items[CorrelationIdMiddleware.HttpContextItemKey],
            responseId);
    }

    [Fact]
    public async Task Valid_header_preserves_supplied_id()
    {
        // Arrange
        var suppliedId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] =
            suppliedId.ToString();
        var middleware = CreateMiddleware(async httpContext =>
            await httpContext.Response.StartAsync(
                TestContext.Current.CancellationToken));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(suppliedId, GetResponseCorrelationId(context));
        Assert.Equal(
            suppliedId,
            context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    [Fact]
    public async Task Invalid_header_generates_new_id()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] =
            "not-a-guid";
        var middleware = CreateMiddleware(async httpContext =>
            await httpContext.Response.StartAsync(
                TestContext.Current.CancellationToken));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var resolvedId = GetResponseCorrelationId(context);
        Assert.NotEqual(Guid.Empty, resolvedId);
        Assert.Equal(
            resolvedId,
            context.Items[CorrelationIdMiddleware.HttpContextItemKey]);
    }

    [Fact]
    public async Task Resolved_id_is_stored_in_http_context_items()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var item = context.Items[
            CorrelationIdMiddleware.HttpContextItemKey];
        Assert.IsType<Guid>(item);
        Assert.NotEqual(Guid.Empty, (Guid)item);
    }

    [Fact]
    public async Task Downstream_middleware_can_read_resolved_id()
    {
        // Arrange
        Guid? downstreamId = null;
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(httpContext =>
        {
            downstreamId = Assert.IsType<Guid>(
                httpContext.Items[
                    CorrelationIdMiddleware.HttpContextItemKey]);
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotNull(downstreamId);
        Assert.NotEqual(Guid.Empty, downstreamId.Value);
    }

    private static CorrelationIdMiddleware CreateMiddleware(
        RequestDelegate next)
    {
        return new CorrelationIdMiddleware(
            next,
            NullLogger<CorrelationIdMiddleware>.Instance);
    }

    private static Guid GetResponseCorrelationId(HttpContext context)
    {
        var value = context.Response.Headers[
            CorrelationIdMiddleware.HeaderName].ToString();
        Assert.True(Guid.TryParse(value, out var correlationId));
        return correlationId;
    }
}
