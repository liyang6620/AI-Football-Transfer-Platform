using FootballTransfer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlerController : ControllerBase
{
    private readonly NewsCrawlerService _crawler;

    public CrawlerController(NewsCrawlerService crawler)
    {
        _crawler = crawler;
    }

    [HttpGet]
    public async Task<IActionResult> Crawl()
    {
        var added = await _crawler.CrawlAndSaveAsync();

        return Ok(new
        {
            added
        });
    }
}