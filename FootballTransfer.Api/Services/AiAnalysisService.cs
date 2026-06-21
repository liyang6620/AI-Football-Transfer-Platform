using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class AiAnalysisService
{
    private readonly FootballTransferDbContext _context;
    private readonly OpenAiAnalysisService _openAiService;
    private readonly ArticleContentService _articleContentService;

    public AiAnalysisService(
        FootballTransferDbContext context,
        OpenAiAnalysisService openAiService,
        ArticleContentService articleContentService)
    {
        _context = context;
        _openAiService = openAiService;
        _articleContentService = articleContentService;
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

        foreach (var news in unprocessedNews)
        {
            await ProcessSingleNews(news);
        }

        await _context.SaveChangesAsync();

        return unprocessedNews.Count;
    }

    public async Task<int> ProcessUnprocessedLimitAsync(int limit)
    {
        var unprocessedNews = await _context.TransferNews
            .Where(n => !n.IsProcessed)
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .ToListAsync();

        foreach (var news in unprocessedNews)
        {
            await ProcessSingleNews(news);
        }

        await _context.SaveChangesAsync();

        return unprocessedNews.Count;
    }

    private async Task ProcessSingleNews(TransferNews news)
    {
        var fullContent = await _articleContentService.GetArticleContentAsync(news.Url);

        var contentForAi = string.IsNullOrWhiteSpace(fullContent)
            ? news.Content
            : fullContent;

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
        news.ExtractedClub = aiResult.Club;
        news.FromClub = aiResult.FromClub;
        news.ToClub = aiResult.ToClub;
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
            && (
                news.TransferType == "Completed Transfer"
                || news.TransferType == "Free Transfer"
                || news.TransferType == "Rumour"
                || news.TransferType == "Contract"
            ))
        {
            news.ToClub = news.ExtractedClub;
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

        if (news.TransferType == "Not Transfer Related" ||
            news.TransferType == "Unknown")
        {
            return;
        }

        if (news.Confidence < 0.7)
        {
            return;
        }

        var exists = await _context.Transfers
            .AnyAsync(t => t.TransferNewsId == news.Id);

        if (exists)
        {
            return;
        }

        var feeCurrency = news.TransferType == "Free Transfer"
            ? null
            : aiResult.FeeCurrency ?? GetFeeCurrencyFromText($"{news.Title} {news.Content} {news.AiSummary} {contentForAi}");

        var transfer = new Transfer
        {
            PlayerName = news.ExtractedPlayer,
            FromClub = news.FromClub,
            ToClub = news.ToClub,
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

    private static string? GetFeeCurrencyFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (text.Contains('£'))
        {
            return "GBP";
        }

        if (text.Contains('€'))
        {
            return "EUR";
        }

        if (text.Contains('$'))
        {
            return "USD";
        }

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
        news.TransferType = "Not Transfer Related";
        news.EstimatedFee = null;
        news.Confidence = 0;
        news.IsProcessed = true;
    }

    private static bool IsTransferRelated(string title, string content)
    {
        var titleText = title.ToLowerInvariant();
        var fullText = $"{title} {content}".ToLowerInvariant();

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
            " set to sign"
        };

        if (strongTitleKeywords.Any(keyword => $" {titleText} ".Contains(keyword)))
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
            "after leaving"
        };

        return includeKeywords.Any(keyword => fullText.Contains(keyword));
    }
}