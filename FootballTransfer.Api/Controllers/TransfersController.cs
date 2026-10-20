using FootballTransfer.Api.Data;
using FootballTransfer.Api.DTOs;
using FootballTransfer.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TransfersController : ControllerBase
{
    private readonly FootballTransferDbContext _context;

    public TransfersController(FootballTransferDbContext context) => _context = context;

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"])]
    [ProducesResponseType<PagedResponse<TransferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "newest")
    {
        if (page < 1) return BadRequest(new { message = "Page must be at least 1." });
        if (pageSize is < 1 or > 100) return BadRequest(new { message = "Page size must be between 1 and 100." });
        if (search?.Length > 100) return BadRequest(new { message = "Search must not exceed 100 characters." });

        var query = _context.Transfers
            .AsNoTracking()
            .Include(transfer => transfer.TransferNews)
            .Include(transfer => transfer.Sources)
            .AsQueryable();

        query = ApplyCategory(query, category);
        query = ApplySearch(query, search);
        query = ApplySort(query, sort);

        var totalCount = await query.CountAsync();
        var transfers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<TransferResponse>(
            transfers.Select(transfer => ToResponse(transfer, false)).ToList(),
            page,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)));
    }

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var transfer = await _context.Transfers
            .AsNoTracking()
            .Include(item => item.TransferNews)
            .Include(item => item.Sources)
            .FirstOrDefaultAsync(item => item.Id == id);

        return transfer == null
            ? NotFound(new { message = "Transfer not found." })
            : Ok(ToResponse(transfer, true));
    }

    [HttpGet("search")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"])]
    [ProducesResponseType<PagedResponse<TransferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult<IActionResult>(BadRequest(new { message = "Keyword is required." }));
        }

        return GetAll(page, pageSize, search: keyword);
    }

    [HttpGet("stats")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetStats()
    {
        var transfers = await _context.Transfers.AsNoTracking().ToListAsync();
        var sourceCoverage = await _context.TransferNews
            .AsNoTracking()
            .Where(news => news.TransferId != null)
            .GroupBy(news => news.Source)
            .Select(group => new
            {
                source = group.Key,
                reports = group.Count(),
                events = group.Select(news => news.TransferId).Distinct().Count()
            })
            .OrderByDescending(group => group.reports)
            .ToListAsync();

        var confidence = transfers.Where(item => item.Confidence is > 0).Select(item => item.Confidence!.Value).ToList();
        var fees = transfers.Where(item => item.EstimatedFee is > 0).ToList();

        return Ok(new
        {
            totalTransfers = transfers.Count,
            completedTransfers = transfers.Count(item => item.TransferType == "Completed Transfer"),
            rumours = transfers.Count(item => item.TransferType == "Rumour"),
            freeTransfers = transfers.Count(item => item.TransferType == "Free Transfer"),
            contracts = transfers.Count(item => item.TransferType == "Contract"),
            averageConfidence = confidence.Count == 0 ? 0 : Math.Round(confidence.Average(), 2),
            highestConfidence = confidence.Count == 0 ? 0 : confidence.Max(),
            highConfidenceCount = confidence.Count(value => value >= 0.85),
            lowConfidenceCount = confidence.Count(value => value < 0.7),
            knownFeeCount = fees.Count,
            undisclosedCount = transfers.Count(item => item.EstimatedFee == null),
            feesByCurrency = fees
                .GroupBy(item => string.IsNullOrWhiteSpace(item.FeeCurrency) ? "UNSPECIFIED" : item.FeeCurrency!.ToUpperInvariant())
                .Select(group => new
                {
                    currency = group.Key,
                    count = group.Count(),
                    total = group.Sum(item => item.EstimatedFee!.Value),
                    average = group.Average(item => item.EstimatedFee!.Value),
                    highest = group.Max(item => item.EstimatedFee!.Value)
                })
                .OrderByDescending(group => group.count),
            topDestinationClubs = TopClubs(transfers.Select(item => item.ToClub)),
            topSourceClubs = TopClubs(transfers.Select(item => item.FromClub)),
            recentActivity = transfers
                .GroupBy(item => item.PublishedAt.Date)
                .OrderByDescending(group => group.Key)
                .Take(6)
                .Select(group => new { date = group.Key, count = group.Count() }),
            sourceCoverage
        });
    }

    private static IQueryable<Transfer> ApplyCategory(IQueryable<Transfer> query, string? category) => category?.ToLowerInvariant() switch
    {
        "official" => query.Where(item => item.TransferType == "Completed Transfer"),
        "rumours" => query.Where(item => item.TransferType == "Rumour"),
        "contracts" => query.Where(item => item.TransferType == "Contract"),
        "free" => query.Where(item => item.TransferType == "Free Transfer"),
        _ => query
    };

    private static IQueryable<Transfer> ApplySearch(IQueryable<Transfer> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        var pattern = $"%{search.Trim()}%";
        return query.Where(item =>
            (item.PlayerName != null && EF.Functions.ILike(item.PlayerName, pattern)) ||
            (item.FromClub != null && EF.Functions.ILike(item.FromClub, pattern)) ||
            (item.ToClub != null && EF.Functions.ILike(item.ToClub, pattern)) ||
            (item.TransferType != null && EF.Functions.ILike(item.TransferType, pattern)) ||
            (item.TransferNews != null && EF.Functions.ILike(item.TransferNews.Title, pattern)) ||
            item.Sources.Any(source => EF.Functions.ILike(source.Source, pattern)));
    }

    private static IQueryable<Transfer> ApplySort(IQueryable<Transfer> query, string sort) => sort.ToLowerInvariant() switch
    {
        "oldest" => query.OrderBy(item => item.PublishedAt),
        "confidence" => query.OrderByDescending(item => item.Confidence).ThenByDescending(item => item.PublishedAt),
        "fee" => query.OrderByDescending(item => item.EstimatedFee).ThenByDescending(item => item.PublishedAt),
        _ => query.OrderByDescending(item => item.PublishedAt)
    };

    private static TransferResponse ToResponse(Transfer transfer, bool includeSummaries)
    {
        var sources = transfer.Sources
            .OrderByDescending(source => source.PublishedAt)
            .Select(source => new TransferSourceResponse(
                source.Id,
                source.Title,
                source.Source,
                source.Url,
                source.PublishedAt,
                includeSummaries ? source.AiSummary : null))
            .ToList();
        var primary = transfer.TransferNews;

        return new TransferResponse(
            transfer.Id,
            transfer.EventKey,
            transfer.PlayerName,
            transfer.FromClub,
            transfer.ToClub,
            transfer.TransferType,
            transfer.EstimatedFee,
            transfer.FeeCurrency,
            transfer.IsLoan,
            transfer.IsLoanFee,
            transfer.Confidence,
            transfer.PublishedAt,
            transfer.CreatedAt,
            primary?.Title,
            primary?.Url,
            primary?.Source,
            includeSummaries ? primary?.AiSummary : null,
            sources.Count,
            sources);
    }

    private static IEnumerable<object> TopClubs(IEnumerable<string?> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Unknown")
        .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key)
        .Take(5)
        .Select(group => new { name = group.Key, count = group.Count() });
}
