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

        foreach (var item in feed.Items.Take(20))
        {
            var title = item.Title ?? string.Empty;
            var content = item.Description ?? string.Empty;
            var url = item.Link ?? string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!IsTransferRelated(title, content))
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
                Title = title,
                Content = content,
                Source = "BBC Sport Football",
                Url = url,
                PublishedAt = item.PublishingDate ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                AiSummary = null,
                ExtractedPlayer = null,
                ExtractedClub = null,
                FromClub = null,
                ToClub = null,
                TransferType = null,
                EstimatedFee = null,
                Confidence = null
            };

            _context.TransferNews.Add(news);
            added++;
        }

        await _context.SaveChangesAsync();

        return added;
    }

    private bool IsTransferRelated(string title, string content)
    {
        var text = $"{title} {content}".ToLower();

        var keywords = new[]
        {
        "transfer",
        "signing",
        "signed from",
        "signs from",
        "joins from",
        "joined from",
        "complete signing",
        "complete £",
        "complete €",
        "bid for",
        "opening bid",
        "offer £",
        "offer €",
        "fee",
        "loan move",
        "on loan",
        "free transfer",
        "free agent",
        "release clause",
        "linked with",
        "interested in",
        "monitoring",
        "wanted by",
        "make contact with",
        "rejected by",
        "preparing bid"
    };

        return keywords.Any(keyword => text.Contains(keyword));
    }

}