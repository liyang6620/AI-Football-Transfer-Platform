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
        return await _context.TransferNews.ToListAsync();
    }

    public async Task<TransferNews> CreateNewsAsync(TransferNews news)
    {
        var existingNews = await _context.TransferNews
            .FirstOrDefaultAsync(n => n.Url == news.Url);

        if (existingNews != null)
        {
            return existingNews;
        }

        _context.TransferNews.Add(news);

        await _context.SaveChangesAsync();

        return news;
    }
}