using FootballTransfer.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("")]
public sealed class HealthController : ControllerBase
{
    private readonly FootballTransferDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public HealthController(FootballTransferDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new
    {
        status = "ok",
        service = "football-transfer-api",
        environment = _environment.EnvironmentName
    });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        try
        {
            if (!await _dbContext.Database.CanConnectAsync(timeout.Token))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "not_ready",
                    dependency = "postgresql"
                });
            }

            var pendingMigrations = (await _dbContext.Database
                .GetPendingMigrationsAsync(timeout.Token))
                .ToArray();

            if (pendingMigrations.Length > 0)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "not_ready",
                    dependency = "database_schema",
                    pendingMigrations = pendingMigrations.Length
                });
            }

            return Ok(new { status = "ready", database = "connected" });
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "not_ready",
                dependency = "postgresql"
            });
        }
    }
}
