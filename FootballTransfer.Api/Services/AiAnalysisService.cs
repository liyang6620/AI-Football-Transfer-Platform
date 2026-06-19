using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class AiAnalysisService
{
    private readonly FootballTransferDbContext _context;
    private readonly OpenAiAnalysisService _openAiService;

    public AiAnalysisService(
        FootballTransferDbContext context,
        OpenAiAnalysisService openAiService)
    {
        _context = context;
        _openAiService = openAiService;
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
        if (!IsTransferRelated(news))
        {
            news.AiSummary = news.Content;
            news.ExtractedPlayer = null;
            news.ExtractedClub = null;
            news.FromClub = null;
            news.ToClub = null;
            news.TransferType = "Not Transfer Related";
            news.EstimatedFee = null;
            news.Confidence = 0;
            news.IsProcessed = true;
            return;
        }

        var aiResult = await _openAiService.AnalyzeNewsAsync(
            news.Title,
            news.Content
        );

        news.AiSummary = aiResult.Summary;
        news.ExtractedPlayer = aiResult.Player;
        news.ExtractedClub = aiResult.Club;
        news.FromClub = aiResult.FromClub;
        news.ToClub = aiResult.ToClub;
        news.TransferType = aiResult.TransferType;
        news.EstimatedFee = aiResult.EstimatedFee;
        news.Confidence = aiResult.Confidence;

        if (string.IsNullOrWhiteSpace(news.ToClub)
            && !string.IsNullOrWhiteSpace(news.ExtractedClub)
            && (
                news.TransferType == "Completed Transfer"
                || news.TransferType == "Free Transfer"
                || news.TransferType == "Rumour"
            ))
        {
            news.ToClub = news.ExtractedClub;
        }

        news.IsProcessed = true;
    }

    private bool IsTransferRelated(TransferNews news)
    {
        var text = $"{news.Title} {news.Content}".ToLower();

        var keywords = new[]
        {
            "transfer",
            "sign",
            "signed",
            "signs",
            "joining",
            "joins",
            "joined",
            "move",
            "deal",
            "fee",
            "bid",
            "loan",
            "contract",
            "interest",
            "interested",
            "monitoring",
            "linked with",
            "free agent",
            "free transfer"
        };

        return keywords.Any(keyword => text.Contains(keyword));
    }
}