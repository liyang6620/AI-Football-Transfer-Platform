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

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfersFromNews()
    {
        var news = await _newsService.GetAllNewsAsync();

        var transfers = news
            .Where(n => !string.IsNullOrWhiteSpace(n.ExtractedPlayer))
            .OrderByDescending(n => n.PublishedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Source,
                n.Url,
                n.PublishedAt,
                n.AiSummary,
                n.ExtractedPlayer,
                n.ExtractedClub,
                n.FromClub,
                n.ToClub,
                n.TransferType,
                n.EstimatedFee,
                n.Confidence
            })
            .ToList();

        return Ok(transfers);
    }

    [HttpGet("/api/transfers")]
    public async Task<IActionResult> GetRealTransfers()
    {
        var transfers = await _newsService.GetTransfersAsync();
        return Ok(transfers);
    }

    [HttpGet("latest-transfers")]
    public async Task<IActionResult> GetLatestTransfers()
    {
        var news = await _newsService.GetAllNewsAsync();

        var latestTransfers = news
            .Where(n => !string.IsNullOrWhiteSpace(n.ExtractedPlayer))
            .OrderByDescending(n => n.PublishedAt)
            .Take(10)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Source,
                n.Url,
                n.PublishedAt,
                n.ExtractedPlayer,
                n.FromClub,
                n.ToClub,
                n.TransferType,
                n.EstimatedFee,
                n.Confidence
            })
            .ToList();

        return Ok(latestTransfers);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTransfers([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest("Keyword is required.");
        }

        var news = await _newsService.GetAllNewsAsync();

        var results = news
            .Where(n =>
                ContainsKeyword(n.Title, keyword) ||
                ContainsKeyword(n.Content, keyword) ||
                ContainsKeyword(n.AiSummary, keyword) ||
                ContainsKeyword(n.ExtractedPlayer, keyword) ||
                ContainsKeyword(n.ExtractedClub, keyword) ||
                ContainsKeyword(n.FromClub, keyword) ||
                ContainsKeyword(n.ToClub, keyword) ||
                ContainsKeyword(n.TransferType, keyword))
            .OrderByDescending(n => n.PublishedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Source,
                n.Url,
                n.PublishedAt,
                n.AiSummary,
                n.ExtractedPlayer,
                n.ExtractedClub,
                n.FromClub,
                n.ToClub,
                n.TransferType,
                n.EstimatedFee,
                n.Confidence
            })
            .ToList();

        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var news = await _newsService.GetAllNewsAsync();

        var item = news.FirstOrDefault(n => n.Id == id);

        if (item == null)
        {
            return NotFound("News not found.");
        }

        return Ok(item);
    }

    private static bool ContainsKeyword(string? value, string keyword)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}