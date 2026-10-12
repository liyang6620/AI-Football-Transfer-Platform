using FootballTransfer.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FootballTransfer.Api.Tests;

public sealed class DatabaseExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ReturnsServiceUnavailable_ForDatabaseFailures()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transfers";
        context.RequestServices = services;
        context.Response.Body = new MemoryStream();
        var handler = new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new NpgsqlException("Database unavailable"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("10", context.Response.Headers.RetryAfter);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandle_UnrelatedFailures()
    {
        var context = new DefaultHttpContext();
        var handler = new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Application failure"),
            CancellationToken.None);

        Assert.False(handled);
    }
}
