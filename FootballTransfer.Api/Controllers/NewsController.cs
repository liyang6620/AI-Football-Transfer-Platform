using FootballTransfer.Api.Models;
using FootballTransfer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly NewsService _newsService;
    private readonly AiAnalysisService _aiAnalysisService;

    public NewsController(
        NewsService newsService,
        AiAnalysisService aiAnalysisService)
    {
        _newsService = newsService;
        _aiAnalysisService = aiAnalysisService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var news = await _newsService.GetAllNewsAsync();
        return Ok(news);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransferNews news)
    {
        var createdNews = await _newsService.CreateNewsAsync(news);
        return Ok(createdNews);
    }

    [HttpGet("unprocessed")]
    public async Task<IActionResult> GetUnprocessed()
    {
        var news = await _newsService.GetUnprocessedNewsAsync();
        return Ok(news);
    }

    [HttpPost("process/{id}")]
    public async Task<IActionResult> Process(int id)
    {
        var result = await _aiAnalysisService.ProcessNewsAsync(id);

        if (!result)
        {
            return NotFound("News not found.");
        }

        return Ok(new
        {
            message = "News processed successfully.",
            newsId = id
        });
    }

    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAll()
    {
        var count = await _aiAnalysisService.ProcessAllAsync();

        return Ok(new
        {
            message = "All unprocessed news processed successfully.",
            processedCount = count
        });
    }
}