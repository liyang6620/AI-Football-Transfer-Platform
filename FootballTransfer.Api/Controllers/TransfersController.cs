using FootballTransfer.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly FootballTransferDbContext _context;

    public TransfersController(FootballTransferDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transfers = await _context.Transfers
            .Include(t => t.TransferNews)
            .OrderByDescending(t => t.PublishedAt)
            .Select(t => new
            {
                t.Id,
                t.PlayerName,
                t.FromClub,
                t.ToClub,
                t.TransferType,
                t.EstimatedFee,
                t.FeeCurrency,
                t.Confidence,
                t.PublishedAt,
                t.CreatedAt,
                NewsTitle = t.TransferNews != null
                    ? t.TransferNews.Title
                    : null,
                NewsUrl = t.TransferNews != null
                    ? t.TransferNews.Url
                    : null,
                NewsSource = t.TransferNews != null
                    ? t.TransferNews.Source
                    : null
            })
            .ToListAsync();

        return Ok(transfers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transfer = await _context.Transfers
            .Include(t => t.TransferNews)
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.PlayerName,
                t.FromClub,
                t.ToClub,
                t.TransferType,
                t.EstimatedFee,
                t.FeeCurrency,
                t.Confidence,
                t.PublishedAt,
                t.CreatedAt,

                NewsTitle = t.TransferNews != null
                    ? t.TransferNews.Title
                    : null,

                NewsContent = t.TransferNews != null
                    ? t.TransferNews.Content
                    : null,

                NewsUrl = t.TransferNews != null
                    ? t.TransferNews.Url
                    : null,

                NewsSource = t.TransferNews != null
                    ? t.TransferNews.Source
                    : null,

                AiSummary = t.TransferNews != null
                    ? t.TransferNews.AiSummary
                    : null
            })
            .FirstOrDefaultAsync();

        if (transfer == null)
        {
            return NotFound(new
            {
                message = "Transfer not found."
            });
        }

        return Ok(transfer);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new
            {
                message = "Keyword is required."
            });
        }

        var lowerKeyword = keyword.ToLower();

        var transfers = await _context.Transfers
            .Include(t => t.TransferNews)
            .Where(t =>
                (t.PlayerName != null &&
                 t.PlayerName.ToLower().Contains(lowerKeyword))

                ||

                (t.FromClub != null &&
                 t.FromClub.ToLower().Contains(lowerKeyword))

                ||

                (t.ToClub != null &&
                 t.ToClub.ToLower().Contains(lowerKeyword))

                ||

                (t.TransferType != null &&
                 t.TransferType.ToLower().Contains(lowerKeyword))
            )
            .OrderByDescending(t => t.PublishedAt)
            .Select(t => new
            {
                t.Id,
                t.PlayerName,
                t.FromClub,
                t.ToClub,
                t.TransferType,
                t.EstimatedFee,
                t.FeeCurrency,
                t.Confidence,
                t.PublishedAt,

                NewsTitle = t.TransferNews != null
                    ? t.TransferNews.Title
                    : null,

                NewsUrl = t.TransferNews != null
                    ? t.TransferNews.Url
                    : null
            })
            .ToListAsync();

        return Ok(transfers);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalTransfers =
            await _context.Transfers.CountAsync();

        if (totalTransfers == 0)
        {
            return Ok(new
            {
                totalTransfers = 0,
                completedTransfers = 0,
                rumours = 0,
                freeTransfers = 0,
                contracts = 0,
                averageConfidence = 0,
                topDestinationClubs = Array.Empty<object>()
            });
        }

        var completedTransfers =
            await _context.Transfers
                .CountAsync(t =>
                    t.TransferType == "Completed Transfer");

        var rumours =
            await _context.Transfers
                .CountAsync(t =>
                    t.TransferType == "Rumour");

        var freeTransfers =
            await _context.Transfers
                .CountAsync(t =>
                    t.TransferType == "Free Transfer");

        var contracts =
            await _context.Transfers
                .CountAsync(t =>
                    t.TransferType == "Contract");

        var averageConfidence =
            await _context.Transfers
                .AverageAsync(t => t.Confidence);

        var topDestinationClubs =
            await _context.Transfers
                .Where(t => !string.IsNullOrEmpty(t.ToClub))
                .GroupBy(t => t.ToClub)
                .Select(g => new
                {
                    club = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

        return Ok(new
        {
            totalTransfers,
            completedTransfers,
            rumours,
            freeTransfers,
            contracts,

            averageConfidence = Math.Round(
                Convert.ToDouble(averageConfidence ?? 0),
                2
            ),

            topDestinationClubs
        });
    }
}