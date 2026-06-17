using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NewsController : ControllerBase
{
    private readonly FootballTransferDbContext _context;

    public NewsController(FootballTransferDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var news = await _context.TransferNews.ToListAsync();

        return Ok(news);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransferNews news)
    {
        _context.TransferNews.Add(news);

        await _context.SaveChangesAsync();

        return Ok(news);
    }
}