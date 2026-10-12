using CodeHollow.FeedReader;
using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FootballTransfer.Api.Services;

public class NewsCrawlerService
{
    private readonly FootballTransferDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsCrawlerService> _logger;

    public NewsCrawlerService(FootballTransferDbContext context, HttpClient httpClient, ILogger<NewsCrawlerService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> CrawlAndSaveAsync()
    {
        var feedItems = new List<(string Title, string RssContent, string Url, DateTime PublishedAt, string Source)>();
        foreach (var source in NewsSourceCatalog.All)
        {
            var attemptedAt = DateTime.UtcNow;
            var syncState = await _context.SourceSyncStates.FirstOrDefaultAsync(state => state.Name == source.Name);
            if (syncState == null)
            {
                syncState = new SourceSyncState { Name = source.Name, Url = source.Url };
                _context.SourceSyncStates.Add(syncState);
            }
            syncState.Url = source.Url;
            syncState.LastAttemptAt = attemptedAt;

            try
            {
                var feed = await FeedReader.ReadAsync(source.Url);
                var sourceItems = feed.Items
                    .Take(100)
                    .Select(item => (
                        Title: item.Title ?? string.Empty,
                        RssContent: item.Description ?? string.Empty,
                        Url: item.Link ?? string.Empty,
                        PublishedAt: item.PublishingDate ?? DateTime.UtcNow,
                        Source: source.Name))
                    .Where(item => !string.IsNullOrWhiteSpace(item.Url))
                    .Where(item => IsTransferCandidate(item.Title, item.RssContent))
                    .ToList();
                feedItems.AddRange(sourceItems);
                syncState.ItemsSeen = sourceItems.Count;
                syncState.LastSuccessAt = attemptedAt;
                syncState.LastError = null;
            }
            catch (Exception ex)
            {
                syncState.ItemsSeen = 0;
                syncState.LastError = ex.GetType().Name;
                _logger.LogWarning(ex, "Unable to read news feed {Source} ({Url}).", source.Name, source.Url);
            }
        }

        if (feedItems.Count == 0)
        {
            await _context.SaveChangesAsync();
            return 0;
        }

        var normalizedItems = feedItems
            .Select(item => (
                Title: CleanTitle(item.Title),
                TitleKey: NormalizeTitle(item.Title),
                RssContent: item.RssContent,
                Url: item.Url.Trim(),
                CanonicalUrl: NormalizeUrl(item.Url),
                PublishedAt: item.PublishedAt,
                Source: item.Source))
            .Where(item => !string.IsNullOrWhiteSpace(item.CanonicalUrl) && !string.IsNullOrWhiteSpace(item.TitleKey))
            .GroupBy(item => item.CanonicalUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .GroupBy(item => item.TitleKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        // Compare canonical URLs and normalized titles against existing records so
        // syndicated stories are not reinserted under a different source URL.
        var existingRecords = await _context.TransferNews
            .AsNoTracking()
            .Select(n => new { n.Url, n.Title })
            .ToListAsync();

        var existingUrlSet = existingRecords
            .Select(n => NormalizeUrl(n.Url))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingTitleSet = existingRecords
            .Select(n => NormalizeTitle(n.Title))
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var item in normalizedItems)
        {
            if (existingUrlSet.Contains(item.CanonicalUrl) || existingTitleSet.Contains(item.TitleKey))
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
                Source = item.Source,
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
            existingUrlSet.Add(item.CanonicalUrl);
            existingTitleSet.Add(item.TitleKey);
            added++;
        }

        await _context.SaveChangesAsync();

        return added;
    }

    private static bool IsTransferCandidate(string title, string description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        var keywords = new[]
        {
            "transfer", "signing", "signed", "signs", "joins", "joined", "loan",
            "bid", "offer", "contract", "extension", "medical", "fee", "deal"
        };
        return keywords.Any(text.Contains);
    }

    private static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return value.Trim().TrimEnd('/').ToLowerInvariant();
        }

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty,
            Query = string.Empty
        };
        return builder.Uri.AbsoluteUri.TrimEnd('/').ToLowerInvariant();
    }

    private static string NormalizeTitle(string? value)
    {
        var title = CleanTitle(value).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        title = Regex.Replace(title, @"\s+[-|]\s+(bbc|the guardian|google news|sky sports).*$", string.Empty);
        title = Regex.Replace(title, @"[^\p{L}\p{N}]+", " ");
        return Regex.Replace(title, @"\s+", " ").Trim();
    }

    private static string CleanTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var title = Regex.Replace(HtmlEntity.DeEntitize(value), @"\s+", " ").Trim();
        return Regex.Replace(
            title,
            @"\s+[-|]\s+(bbc|the guardian|google news|sky sports).*$",
            string.Empty,
            RegexOptions.IgnoreCase).Trim();
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to download or parse article {Url}.", url);
            return string.Empty;
        }
    }
}
