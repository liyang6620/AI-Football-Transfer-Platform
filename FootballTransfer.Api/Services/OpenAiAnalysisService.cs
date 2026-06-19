using FootballTransfer.Api.Models;

namespace FootballTransfer.Api.Services;

public class OpenAiAnalysisService
{
    public async Task<OpenAiTransferResult> AnalyzeNewsAsync(
        string title,
        string content)
    {
        await Task.Delay(100);

        return new OpenAiTransferResult
        {
            Summary = content,
            Player = null,
            Club = null,
            TransferType = "Unknown",
            Confidence = 0.5
        };
    }
}