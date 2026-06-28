using CodeHollow.FeedReader;
using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class NewsCrawlerService
{
    private readonly FootballTransferDbContext _context;
    private readonly HttpClient _httpClient;

    public NewsCrawlerService(FootballTransferDbContext context)
    {
        _context = context;
        _httpClient = new HttpClient();
    }

    public async Task<int> CrawlAndSaveAsync()
    {
        var feedUrl = "https://feeds.bbci.co.uk/sport/football/rss.xml";

        var feed = await FeedReader.ReadAsync(feedUrl);

        var feedItems = feed.Items
            .Take(500)
            .Select(item => new
            {
                Title = item.Title ?? string.Empty,
                RssContent = item.Description ?? string.Empty,
                Url = item.Link ?? string.Empty,
                PublishedAt = item.PublishingDate ?? DateTime.UtcNow
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .ToList();

        if (feedItems.Count == 0)
        {
            return 0;
        }

        var urls = feedItems
            .Select(item => item.Url)
            .Distinct()
            .ToList();

        var existingUrls = await _context.TransferNews
            .Where(n => urls.Contains(n.Url))
            .Select(n => n.Url)
            .ToListAsync();

        var existingUrlSet = existingUrls.ToHashSet();

        var added = 0;

        foreach (var item in feedItems)
        {
            if (existingUrlSet.Contains(item.Url))
            {
                continue;
            }

            var fullContent = await GetFullArticleContentAsync(item.Url);

            var finalContent = string.IsNullOrWhiteSpace(fullContent)
                ? item.RssContent
                : fullContent;

            var news = new TransferNews
            {
                Title = item.Title,
                Content = finalContent,
                Source = "BBC Sport Football",
                Url = item.Url,
                PublishedAt = item.PublishedAt,
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
            existingUrlSet.Add(item.Url);
            added++;
        }

        await _context.SaveChangesAsync();

        return added;
    }

    private async Task<string> GetFullArticleContentAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var paragraphs = doc.DocumentNode
                .SelectNodes("//article//p | //main//p")
                ?.Select(p => HtmlEntity.DeEntitize(p.InnerText.Trim()))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct()
                .ToList();

            if (paragraphs == null || paragraphs.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n\n", paragraphs);
        }
        catch
        {
            return string.Empty;
        }
    }
}