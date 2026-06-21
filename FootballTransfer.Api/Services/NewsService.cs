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
            .OrderByDescending(n => n.PublishedAt)
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