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
            return NotFound(new
            {
                message = "News not found."
            });
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

    [HttpGet("extracted-transfers")]
    public async Task<IActionResult> GetExtractedTransfersFromNews()
    {
        var news = await _newsService.GetExtractedTransfersAsync();

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

    [HttpGet("latest-extracted-transfers")]
    public async Task<IActionResult> GetLatestExtractedTransfers()
    {
        var news = await _newsService.GetExtractedTransfersAsync(10);

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
    public async Task<IActionResult> SearchNews([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new
            {
                message = "Keyword is required."
            });
        }

        if (keyword.Length > 100)
        {
            return BadRequest(new { message = "Keyword must not exceed 100 characters." });
        }

        var news = await _newsService.SearchNewsAsync(keyword);

        var results = news
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _newsService.GetNewsByIdAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "News not found."
            });
        }

        return Ok(item);
    }
}
