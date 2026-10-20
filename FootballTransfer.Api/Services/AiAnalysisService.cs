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
        await ResetForReprocessAsync();
        return await ProcessAllAsync();
    }

    public async Task<int> ResetForReprocessAsync()
    {
        // Transfers are derived from AI output. Rebuild them from scratch so that
        // records which are no longer valid are removed instead of lingering.
        var newsItems = await _context.TransferNews.ToListAsync();
        foreach (var news in newsItems)
        {
            news.TransferId = null;
            news.IsProcessed = false;
            news.AiSummary = null;
            news.ExtractedPlayer = null;
            news.ExtractedClub = null;
            news.FromClub = null;
            news.ToClub = null;
            news.TransferType = null;
            news.EstimatedFee = null;
            news.FeeCurrency = null;
            news.IsLoan = false;
            news.IsLoanFee = false;
            news.Confidence = null;
        }

        await _context.SaveChangesAsync();
        _context.Transfers.RemoveRange(await _context.Transfers.ToListAsync());
        await _context.SaveChangesAsync();
        return newsItems.Count;
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
            .Include(t => t.TransferNews)
            .OrderByDescending(t => t.Confidence)
            .ThenByDescending(t => t.PublishedAt)
            .ToListAsync();

        foreach (var transfer in transfers)
        {
            normalized += NormalizeClubField(transfer.FromClub, value => transfer.FromClub = value);
            normalized += NormalizeClubField(transfer.ToClub, value => transfer.ToClub = value);
        }

        var kept = new Dictionary<string, Transfer>(StringComparer.Ordinal);
        var duplicatesRemoved = 0;
        foreach (var transfer in transfers)
        {
            var eventKey = TransferEventKey.Create(transfer.PlayerName, transfer.ToClub, transfer.TransferType);
            transfer.EventKey = eventKey;
            if (kept.TryGetValue(eventKey, out var existing))
            {
                TransferMerger.Merge(existing, transfer);
                await _context.TransferNews
                    .Where(news => news.TransferId == transfer.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(news => news.TransferId, existing.Id));
                _context.Transfers.Remove(transfer);
                duplicatesRemoved++;
            }
            else
            {
                kept[eventKey] = transfer;
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

        if (!TransferRelevanceFilter.IsTransferRelated(news.Title, contentForAi))
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
        news.FeeCurrency = aiResult.FeeCurrency;
        news.IsLoan = aiResult.IsLoan;
        news.IsLoanFee = aiResult.IsLoan && aiResult.EstimatedFee is > 0;
        news.Confidence = aiResult.Confidence;
        TransferResultPostProcessor.EnforceInvariants(news);

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

        var existingForNews = _context.Transfers.Local
            .FirstOrDefault(t => t.TransferNewsId == news.Id)
            ?? await _context.Transfers.FirstOrDefaultAsync(t => t.TransferNewsId == news.Id);

        if (existingForNews != null)
        {
            news.Transfer = existingForNews;
            return;
        }

        var eventKey = TransferEventKey.Create(news.ExtractedPlayer, news.ToClub, news.TransferType);
        var duplicate = _context.Transfers.Local.FirstOrDefault(t => t.EventKey == eventKey)
            ?? await _context.Transfers
                .Include(t => t.TransferNews)
                .FirstOrDefaultAsync(t => t.EventKey == eventKey);

        if (duplicate != null)
        {
            news.Transfer = duplicate;
            TransferMerger.Merge(duplicate, news, aiResult);
            return;
        }

        var transfer = new Transfer
        {
            PlayerName = news.ExtractedPlayer,
            FromClub = ClubNameNormalizer.Normalize(news.FromClub),
            ToClub = ClubNameNormalizer.Normalize(news.ToClub),
            TransferType = news.TransferType,
            EstimatedFee = news.TransferType == "Free Transfer" ? 0 : news.EstimatedFee,
            FeeCurrency = aiResult.FeeCurrency,
            IsLoan = aiResult.IsLoan,
            IsLoanFee = aiResult.IsLoan && news.EstimatedFee is > 0,
            Confidence = news.Confidence,
            EventKey = eventKey,
            TransferNewsId = news.Id,
            TransferNews = news,
            Sources = [news],
            PublishedAt = news.PublishedAt,
            CreatedAt = DateTime.UtcNow
        };

        TransferResultPostProcessor.EnforceInvariants(transfer);
        _context.Transfers.Add(transfer);
    }

    private static bool IsValidTransferType(string? transferType)
    {
        return transferType == "Rumour"
            || transferType == "Completed Transfer"
            || transferType == "Free Transfer"
            || transferType == "Contract";
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
        news.FeeCurrency = null;
        news.IsLoan = false;
        news.IsLoanFee = false;
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
        news.FeeCurrency = null;
        news.IsLoan = false;
        news.IsLoanFee = false;
        news.Confidence = 0;
        // Keep transient failures pending so the next scheduled run can retry them.
        news.IsProcessed = false;
    }

}
