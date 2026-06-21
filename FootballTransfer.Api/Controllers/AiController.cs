using FootballTransfer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly AiAnalysisService _aiAnalysisService;

    public AiController(AiAnalysisService aiAnalysisService)
    {
        _aiAnalysisService = aiAnalysisService;
    }

    [HttpPost("process-unprocessed")]
    public async Task<IActionResult> ProcessUnprocessed()
    {
        var processedCount = await _aiAnalysisService.ProcessUnprocessedLimitAsync(5);

        return Ok(new
        {
            processed = processedCount
        });
    }

    [HttpPost("process-unprocessed/{limit:int}")]
    public async Task<IActionResult> ProcessUnprocessedWithLimit(int limit)
    {
        if (limit <= 0)
        {
            return BadRequest(new
            {
                message = "Limit must be greater than 0."
            });
        }

        if (limit > 20)
        {
            limit = 20;
        }

        var processedCount = await _aiAnalysisService.ProcessUnprocessedLimitAsync(limit);

        return Ok(new
        {
            processed = processedCount,
            limit
        });
    }

    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAll()
    {
        var processedCount = await _aiAnalysisService.ProcessAllAsync();

        return Ok(new
        {
            processed = processedCount
        });
    }
}