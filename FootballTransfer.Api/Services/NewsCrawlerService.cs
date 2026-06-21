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

        var added = 0;

        foreach (var item in feed.Items.Take(100))
        {
            var title = item.Title ?? string.Empty;
            var rssContent = item.Description ?? string.Empty;
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

            var fullContent = await GetFullArticleContentAsync(url);

            var finalContent = string.IsNullOrWhiteSpace(fullContent)
                ? rssContent
                : fullContent;

            var news = new TransferNews
            {
                Title = title,
                Content = finalContent,
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