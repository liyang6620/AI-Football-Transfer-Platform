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

    private async Task ProcessSingleNews(TransferNews news)
    {
        var aiResult = await _openAiService.AnalyzeNewsAsync(
            news.Title,
            news.Content
        );

        news.AiSummary = aiResult.Summary;
        news.ExtractedPlayer = aiResult.Player;
        news.ExtractedClub = aiResult.Club;
        news.TransferType = aiResult.TransferType;
        news.EstimatedFee = aiResult.EstimatedFee;
        news.Confidence = aiResult.Confidence;

        news.IsProcessed = true;
    }
}