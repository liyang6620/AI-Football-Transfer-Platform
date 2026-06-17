using CodeHollow.FeedReader;
using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class NewsCrawlerService
{
    private readonly FootballTransferDbContext _context;

    public NewsCrawlerService(FootballTransferDbContext context)
    {
        _context = context;
    }

    public async Task<int> CrawlAndSaveAsync()
    {
        var feedUrl = "https://feeds.bbci.co.uk/sport/football/rss.xml";

        var feed = await FeedReader.ReadAsync(feedUrl);

        var added = 0;

        foreach (var item in feed.Items.Take(10))
        {
            var url = item.Link ?? string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var exists = await _context.TransferNews
                .AnyAsync(n => n.Url == url);

            if (exists)
            {
                continue;
            }

            var news = new TransferNews
            {
                Title = item.Title ?? string.Empty,
                Content = item.Description ?? string.Empty,
                Source = "BBC Sport Football",
                Url = url,
                PublishedAt = item.PublishingDate ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                AiSummary = null
            };

            _context.TransferNews.Add(news);
            added++;
        }

        await _context.SaveChangesAsync();

        return added;
    }
}