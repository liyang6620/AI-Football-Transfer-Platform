using FootballTransfer.Api.Models;
using FootballTransfer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly NewsService _newsService;

    public NewsController(NewsService newsService)
    {
        _newsService = newsService;
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
}