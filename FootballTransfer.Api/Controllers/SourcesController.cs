using FootballTransfer.Api.Data;
using FootballTransfer.Api.DTOs;
using FootballTransfer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SourcesController : ControllerBase
{
    private readonly FootballTransferDbContext _context;
    private readonly int _intervalHours;

    public SourcesController(FootballTransferDbContext context, IConfiguration configuration)
    {
        _context = context;
        _intervalHours = Math.Max(1, configuration.GetValue("BackgroundJobs:IntervalHours", 24));
    }

    [HttpGet("status")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<SourceStatusResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var states = await _context.SourceSyncStates.AsNoTracking().ToDictionaryAsync(state => state.Name);
        var reportCounts = await _context.TransferNews.AsNoTracking()
            .GroupBy(news => news.Source)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count);

        return Ok(new SourceStatusResponse(
            _intervalHours,
            NewsSourceCatalog.All.Select(source =>
            {
                states.TryGetValue(source.Name, out var state);
                return new SourceHealthResponse(
                    source.Name,
                    source.Url,
                    state?.LastError == null && state?.LastSuccessAt != null ? "healthy" : state?.LastError != null ? "degraded" : "pending",
                    state?.LastAttemptAt,
                    state?.LastSuccessAt,
                    state?.ItemsSeen ?? 0,
                    reportCounts.GetValueOrDefault(source.Name),
                    state?.LastError);
            }).ToList()));
    }
}
