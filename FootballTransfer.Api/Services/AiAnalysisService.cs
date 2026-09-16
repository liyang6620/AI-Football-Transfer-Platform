using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class AiAnalysisService
{
    private readonly FootballTransferDbContext _context;
    private readonly OpenAiAnalysisService _openAiService;
    private readonly ArticleContentService _articleContentService;
    private readonly ILogger<AiAnalysisService> _logger;

    public AiAnalysisService(
        FootballTransferDbContext context,
        OpenAiAnalysisService openAiService,
        ArticleContentService articleContentService,
        ILogger<AiAnalysisService> logger)
    {
        _context = context;
        _openAiService = openAiService;
        _articleContentService = articleContentService;
        _logger = logger;
    }

    public async Task<bool> ProcessNewsAsync(int id)
    {
        var news = await _context.TransferNews.FindAsync(id);

        if (news == null)
        {
            return false;
        }

        await ProcessSingleNews(news);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> ProcessAllAsync()
    {
        var unprocessedNews = await _context.TransferNews
            .Where(n => !n.IsProcessed)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync();

        var processedCount = 0;

        foreach (var news in unprocessedNews)
        {
            try
            {
                await ProcessSingleNews(news);
                processedCount++;
            }
            catch (Exception ex)
            {
                MarkAsFailed(news, ex);
            }
        }

        await _context.SaveChangesAsync();

        return processedCount;
    }

    public async Task<int> ReprocessAllAsync()
    {
        // Transfers are derived from AI output. Rebuild them from scratch so that
        // records which are no longer valid are removed instead of lingering.
        _context.Transfers.RemoveRange(await _context.Transfers.ToListAsync());
        var newsItems = await _context.TransferNews.ToListAsync();
        foreach (var news in newsItems)
        {
            news.IsProcessed = false;
            news.AiSummary = null;
            news.ExtractedPlayer = null;
            news.ExtractedClub = null;
            news.FromClub = null;
            news.ToClub = null;
            news.TransferType = null;
            news.EstimatedFee = null;
            news.Confidence = null;
        }

        await _context.SaveChangesAsync();
        return await ProcessAllAsync();
    }

    public async Task<(int Normalized, int DuplicatesRemoved)> NormalizeClubNamesAsync()
    {
        var normalized = 0;
        var newsItems = await _context.TransferNews.ToListAsync();
        foreach (var news in newsItems)
        {
            normalized += NormalizeClubField(news.ExtractedClub, value => news.ExtractedClub = value);
            normalized += NormalizeClubField(news.FromClub, value => news.FromClub = value);
            normalized += NormalizeClubField(news.ToClub, value => news.ToClub = value);
        }

        var transfers = await _context.Transfers
            .OrderByDescending(t => t.Confidence)
            .ThenByDescending(t => t.PublishedAt)
            .ToListAsync();

        foreach (var transfer in transfers)
        {
            normalized += NormalizeClubField(transfer.FromClub, value => transfer.FromClub = value);
            normalized += NormalizeClubField(transfer.ToClub, value => transfer.ToClub = value);
        }

        var kept = new List<Transfer>();
        var duplicatesRemoved = 0;
        foreach (var transfer in transfers)
        {
            var duplicate = kept.Any(existing =>
                ClubNameNormalizer.TextKey(existing.PlayerName) == ClubNameNormalizer.TextKey(transfer.PlayerName) &&
                ClubNameNormalizer.Key(existing.FromClub) == ClubNameNormalizer.Key(transfer.FromClub) &&
                ClubNameNormalizer.Key(existing.ToClub) == ClubNameNormalizer.Key(transfer.ToClub) &&
                string.Equals(existing.TransferType, transfer.TransferType, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs((existing.PublishedAt - transfer.PublishedAt).TotalDays) <= 14);

            if (duplicate)
            {
                _context.Transfers.Remove(transfer);
                duplicatesRemoved++;
            }
            else
            {
                kept.Add(transfer);
            }
        }

        await _context.SaveChangesAsync();
        return (normalized, duplicatesRemoved);
    }

    private static int NormalizeClubField(string? current, Action<string?> assign)
    {
        var normalized = ClubNameNormalizer.Normalize(current);
        if (string.Equals(current, normalized, StringComparison.Ordinal)) return 0;
        assign(normalized);
        return 1;
    }

    public async Task<int> ProcessUnprocessedLimitAsync(int limit)
    {
        var unprocessedNews = await _context.TransferNews
            .Where(n => !n.IsProcessed)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync();

        var processedCount = 0;

        foreach (var news in unprocessedNews)
        {
            try
            {
                await ProcessSingleNews(news);
                processedCount++;
            }
            catch (Exception ex)
            {
                MarkAsFailed(news, ex);
            }
        }

        await _context.SaveChangesAsync();

        return processedCount;
    }

    private async Task ProcessSingleNews(TransferNews news)
    {
        var fullContent = await _articleContentService.GetArticleContentAsync(news.Url);

        var contentForAi = string.IsNullOrWhiteSpace(fullContent)
            ? news.Content
            : fullContent;

        news.Content = contentForAi;

        if (!IsTransferRelated(news.Title, contentForAi))
        {
            MarkAsNotTransfer(news, contentForAi);
            return;
        }

        var aiResult = await _openAiService.AnalyzeNewsAsync(
            news.Title,
            contentForAi
        );

        news.AiSummary = aiResult.Summary;
        news.ExtractedPlayer = aiResult.Player;
        news.ExtractedClub = ClubNameNormalizer.Normalize(aiResult.Club);
        news.FromClub = ClubNameNormalizer.Normalize(aiResult.FromClub);
        news.ToClub = ClubNameNormalizer.Normalize(aiResult.ToClub);
        news.TransferType = aiResult.TransferType;
        news.EstimatedFee = aiResult.EstimatedFee;
        news.Confidence = aiResult.Confidence;

        if (news.TransferType == "Free Transfer")
        {
            news.EstimatedFee = 0;
            aiResult.FeeCurrency = null;
        }

        if (string.IsNullOrWhiteSpace(news.ToClub)
            && !string.IsNullOrWhiteSpace(news.ExtractedClub)
            && IsValidTransferType(news.TransferType))
        {
            news.ToClub = ClubNameNormalizer.Normalize(news.ExtractedClub);
        }

        if (string.IsNullOrWhiteSpace(news.ExtractedPlayer)
            || string.IsNullOrWhiteSpace(news.TransferType)
            || news.TransferType == "Unknown"
            || news.Confidence < 0.5)
        {
            MarkAsNotTransfer(news, contentForAi);
            return;
        }

        await CreateTransferIfValid(news, aiResult, contentForAi);

        news.IsProcessed = true;
    }

    private async Task CreateTransferIfValid(
        TransferNews news,
        OpenAiTransferResult aiResult,
        string contentForAi)
    {
        if (string.IsNullOrWhiteSpace(news.ExtractedPlayer))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(news.ToClub))
        {
            return;
        }

        if (!IsValidTransferType(news.TransferType))
        {
            return;
        }

        if (news.Confidence < 0.7)
        {
            return;
        }

        var existingForNews = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransferNewsId == news.Id);

        if (existingForNews != null)
        {
            return;
        }

        var windowStart = news.PublishedAt.AddDays(-14);
        var windowEnd = news.PublishedAt.AddDays(14);
        var possibleDuplicates = await _context.Transfers
            .Where(t => t.TransferType == news.TransferType &&
                        t.PublishedAt >= windowStart && t.PublishedAt <= windowEnd)
            .ToListAsync();

        var duplicate = possibleDuplicates.FirstOrDefault(t =>
            ClubNameNormalizer.TextKey(t.PlayerName) == ClubNameNormalizer.TextKey(news.ExtractedPlayer) &&
            ClubNameNormalizer.Key(t.FromClub) == ClubNameNormalizer.Key(news.FromClub) &&
            ClubNameNormalizer.Key(t.ToClub) == ClubNameNormalizer.Key(news.ToClub));

        if (duplicate != null)
        {
            if ((news.Confidence ?? 0) > (duplicate.Confidence ?? 0))
            {
                duplicate.PlayerName = news.ExtractedPlayer?.Trim();
                duplicate.FromClub = ClubNameNormalizer.Normalize(news.FromClub);
                duplicate.ToClub = ClubNameNormalizer.Normalize(news.ToClub);
                duplicate.EstimatedFee = news.EstimatedFee;
                duplicate.FeeCurrency = aiResult.FeeCurrency;
                duplicate.Confidence = news.Confidence;
                duplicate.TransferNewsId = news.Id;
                duplicate.PublishedAt = news.PublishedAt;
            }
            return;
        }

        var feeCurrency = news.TransferType == "Free Transfer"
            ? null
            : aiResult.FeeCurrency ?? GetFeeCurrencyFromText($"{news.Title} {news.Content} {news.AiSummary} {contentForAi}");

        var transfer = new Transfer
        {
            PlayerName = news.ExtractedPlayer,
            FromClub = ClubNameNormalizer.Normalize(news.FromClub),
            ToClub = ClubNameNormalizer.Normalize(news.ToClub),
            TransferType = news.TransferType,
            EstimatedFee = news.TransferType == "Free Transfer" ? 0 : news.EstimatedFee,
            FeeCurrency = feeCurrency,
            Confidence = news.Confidence,
            TransferNewsId = news.Id,
            PublishedAt = news.PublishedAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transfers.Add(transfer);
    }

    private static bool IsValidTransferType(string? transferType)
    {
        return transferType == "Rumour"
            || transferType == "Completed Transfer"
            || transferType == "Free Transfer"
            || transferType == "Contract";
    }

    private static string? GetFeeCurrencyFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (text.Contains('£')) return "GBP";
        if (text.Contains('€')) return "EUR";
        if (text.Contains('$')) return "USD";

        return null;
    }

    private static void MarkAsNotTransfer(TransferNews news, string content)
    {
        news.AiSummary = string.IsNullOrWhiteSpace(content)
            ? news.Content
            : content.Length > 200
                ? content.Substring(0, 200) + "..."
                : content;

        news.ExtractedPlayer = null;
        news.ExtractedClub = null;
        news.FromClub = null;
        news.ToClub = null;
        news.TransferType = "Unknown";
        news.EstimatedFee = null;
        news.Confidence = 0;
        news.IsProcessed = true;
    }

    private void MarkAsFailed(TransferNews news, Exception exception)
    {
        _logger.LogError(exception, "Failed to process news item {NewsId} from {Url}.", news.Id, news.Url);
        news.AiSummary = "AI processing failed; it will be retried on the next scheduled run.";
        news.ExtractedPlayer = null;
        news.ExtractedClub = null;
        news.FromClub = null;
        news.ToClub = null;
        news.TransferType = "Processing Failed";
        news.EstimatedFee = null;
        news.Confidence = 0;
        // Keep transient failures pending so the next scheduled run can retry them.
        news.IsProcessed = false;
    }

    private static bool IsTransferRelated(string title, string content)
    {
        var titleText = title.ToLowerInvariant();
        var fullText = $"{title} {content}".ToLowerInvariant();
        var wrappedTitle = $" {titleText} ";

        if (titleText.Contains("done deals"))
        {
            return false;
        }

        var strongTitleKeywords = new[]
        {
            " sign ",
            " signs ",
            " signed ",
            " signing ",
            " joins ",
            " joined ",
            " new deal",
            " transfer",
            " bid",
            " offer",
            " loan",
            " free transfer",
            " close in on",
            " close to signing",
            " set to sign",
            " set to join",
            " set to have a medical",
            " medical",
            " in talks",
            " interested in",
            " target",
            " wants",
            " agree ",
            " agrees ",
            " agreed ",
            " agreed deal",
            " agreed a deal",
            " agree deal",
            " agreed fee",
            " agree fee",
            " record fee"
        };

        if (strongTitleKeywords.Any(keyword => wrappedTitle.Contains(keyword)))
        {
            return true;
        }

        var excludeKeywords = new[]
        {
            "match report",
            "preview",
            "reaction",
            "injury update",
            "world cup group",
            "penalty",
            "highlights",
            "video"
        };

        if (excludeKeywords.Any(keyword => fullText.Contains(keyword)))
        {
            return false;
        }

        var includeKeywords = new[]
        {
            "transfer",
            "sign ",
            "signs ",
            "signed ",
            "signing",
            "joins",
            "joined",
            "departures",
            "new deal",
            "contract extension",
            "bid",
            "offer",
            "fee",
            "loan",
            "free agent",
            "free transfer",
            "release clause",
            "linked with",
            "interested in",
            "target",
            "chase",
            "wanted by",
            "preparing bid",
            "move to",
            "after leaving",
            "agreed a deal",
            "agreed deal",
            "agreed fee",
            "agree fee",
            "record fee",
            "set to have a medical",
            "set for medical",
            "medical before completing",
            "personal terms",
            "deal is close",
            "deal close",
            "close to securing",
            "close to completing"
        };

        return includeKeywords.Any(keyword => fullText.Contains(keyword));
    }
}
