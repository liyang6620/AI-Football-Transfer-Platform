using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace FootballTransfer.Api.Infrastructure;

public sealed class DatabaseExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DatabaseExceptionHandler> _logger;

    public DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!ContainsDatabaseFailure(exception))
        {
            return false;
        }

        _logger.LogWarning(exception, "PostgreSQL is unavailable.");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.Headers.RetryAfter = "10";

        await Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Transfer data is temporarily unavailable",
            detail: "The database dependency could not be reached. Retry the request shortly.")
            .ExecuteAsync(httpContext);

        return true;
    }

    private static bool ContainsDatabaseFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
