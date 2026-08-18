using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class NewsService
{
    private readonly FootballTransferDbContext _context;

    public NewsService(FootballTransferDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransferNews>> GetAllNewsAsync()
    {
        return await _context.TransferNews
            .AsNoTracking()
            .OrderByDescending(n => n.PublishedAt)
            .Take(200)
            .ToListAsync();
    }

    public Task<TransferNews?> GetNewsByIdAsync(int id) =>
        _context.TransferNews.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);

    public Task<List<TransferNews>> GetExtractedTransfersAsync(int limit = 200) =>
        _context.TransferNews
            .AsNoTracking()
            .Where(n => n.ExtractedPlayer != null && n.ExtractedPlayer != "")
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync();

    public Task<List<TransferNews>> SearchNewsAsync(string keyword)
    {
        var pattern = $"%{keyword.Trim()}%";
        return _context.TransferNews
            .AsNoTracking()
            .Where(n =>
                EF.Functions.ILike(n.Title, pattern) ||
                EF.Functions.ILike(n.Content, pattern) ||
                (n.AiSummary != null && EF.Functions.ILike(n.AiSummary, pattern)) ||
                (n.ExtractedPlayer != null && EF.Functions.ILike(n.ExtractedPlayer, pattern)) ||
                (n.ExtractedClub != null && EF.Functions.ILike(n.ExtractedClub, pattern)) ||
                (n.FromClub != null && EF.Functions.ILike(n.FromClub, pattern)) ||
                (n.ToClub != null && EF.Functions.ILike(n.ToClub, pattern)) ||
                (n.TransferType != null && EF.Functions.ILike(n.TransferType, pattern)))
            .OrderByDescending(n => n.PublishedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<TransferNews> CreateNewsAsync(TransferNews news)
    {
        var existingNews = await _context.TransferNews
            .FirstOrDefaultAsync(n => n.Url == news.Url);

        if (existingNews != null)
        {
            return existingNews;
        }

        news.CreatedAt = DateTime.UtcNow;
        news.IsProcessed = false;

        _context.TransferNews.Add(news);

        await _context.SaveChangesAsync();

        return news;
    }

    public async Task<List<TransferNews>> GetUnprocessedNewsAsync()
    {
        return await _context.TransferNews
            .AsNoTracking()
            .Where(n => !n.IsProcessed)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync();
    }

    public async Task<List<Transfer>> GetTransfersAsync()
    {
        return await _context.Transfers
            .Include(t => t.TransferNews)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TransferNews?> MarkAsProcessedAsync(int id, string aiSummary)
    {
        var news = await _context.TransferNews.FindAsync(id);

        if (news == null)
        {
            return null;
        }

        news.AiSummary = aiSummary;
        news.IsProcessed = true;

        await _context.SaveChangesAsync();

        return news;
    }
}
