using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public class AiAnalysisService
{
    private readonly FootballTransferDbContext _context;

    public AiAnalysisService(FootballTransferDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ProcessNewsAsync(int id)
    {
        var news = await _context.TransferNews.FindAsync(id);

        if (news == null)
        {
            return false;
        }

        ProcessSingleNews(news);

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
            ProcessSingleNews(news);
        }

        await _context.SaveChangesAsync();

        return unprocessedNews.Count;
    }

    private void ProcessSingleNews(TransferNews news)
    {
        var sourceText = !string.IsNullOrWhiteSpace(news.Content)
            ? news.Content
            : news.Title;

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            sourceText = "No content available.";
        }

        news.AiSummary = sourceText.Length > 150
            ? sourceText.Substring(0, 150) + "..."
            : sourceText;

        ApplyRuleBasedExtraction(news);

        news.IsProcessed = true;
    }

    private void ApplyRuleBasedExtraction(TransferNews news)
    {
        var text = $"{news.Title} {news.Content}".ToLower();

        if (text.Contains("free transfer"))
        {
            news.TransferType = "Free Transfer";
        }
        else if (text.Contains("interested") || text.Contains("monitoring"))
        {
            news.TransferType = "Rumour";
        }
        else if (text.Contains("sign") || text.Contains("joins") || text.Contains("links up"))
        {
            news.TransferType = "Completed Transfer";
        }
        else
        {
            news.TransferType = "Unknown";
        }

        if (text.Contains("liverpool"))
        {
            news.ExtractedClub = "Liverpool";
        }
        else if (text.Contains("real madrid"))
        {
            news.ExtractedClub = "Real Madrid";
        }
        else if (text.Contains("manchester city"))
        {
            news.ExtractedClub = "Manchester City";
        }
        else if (text.Contains("rangers"))
        {
            news.ExtractedClub = "Rangers";
        }

        if (text.Contains("florian wirtz"))
        {
            news.ExtractedPlayer = "Florian Wirtz";
        }
        else if (text.Contains("bellingham"))
        {
            news.ExtractedPlayer = "Jude Bellingham";
        }
        else if (text.Contains("silva"))
        {
            news.ExtractedPlayer = "Bernardo Silva";
        }
        else if (text.Contains("messi"))
        {
            news.ExtractedPlayer = "Lionel Messi";
        }
        else if (text.Contains("haaland"))
        {
            news.ExtractedPlayer = "Erling Haaland";
        }

        news.EstimatedFee = null;

        news.Confidence = string.IsNullOrWhiteSpace(news.ExtractedPlayer)
            ? 0.4
            : 0.8;
    }
}