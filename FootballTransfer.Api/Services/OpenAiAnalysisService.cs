using System.Text.Json;
using FootballTransfer.Api.Models;
using OpenAI.Chat;

namespace FootballTransfer.Api.Services;

public class OpenAiAnalysisService
{
    private readonly ChatClient _chatClient;

    public OpenAiAnalysisService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is not set.");
        }

        _chatClient = new ChatClient(
            model: "gpt-4o-mini",
            apiKey: apiKey
        );
    }

    public async Task<OpenAiTransferResult> AnalyzeNewsAsync(string title, string content)
    {
        var prompt =
            "You are a football transfer intelligence analyst.\n\n" +
            "Analyse the following football news and return ONLY valid JSON.\n\n" +
            "JSON format:\n" +
            "{\n" +
            "  \"player\": \"player name or null\",\n" +
            "  \"club\": \"main relevant club or null\",\n" +
            "  \"fromClub\": \"previous club or null\",\n" +
            "  \"toClub\": \"destination club or interested club or null\",\n" +
            "  \"transferType\": \"Rumour / Completed Transfer / Free Transfer / Contract / Unknown\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"one sentence summary\"\n" +
            "}\n\n" +
            "Rules:\n" +
            "- Return ONLY valid JSON. Do not include explanations, markdown, or code blocks.\n" +
            "- Identify the main football player mentioned in the article.\n" +
            "- Set club as the most important club involved in the transfer story.\n" +
            "- If the article reports a completed transfer, identify both fromClub and toClub whenever possible.\n" +
            "- For completed transfers, toClub should be the destination club.\n" +
            "- For completed transfers, fromClub should be the player's previous club.\n" +
            "- If the article is a transfer rumour, set toClub as the interested club.\n" +
            "- For rumours, only set fromClub if the current club is clearly mentioned.\n" +
            "- If multiple clubs are mentioned, choose the clubs directly involved in the transfer.\n" +
            "- Determine transferType using one of these values only: Rumour, Completed Transfer, Free Transfer, Contract, Unknown.\n" +
            "- If phrases like 'free transfer', 'joined on a free', or 'available on a free' appear, use Free Transfer.\n" +
            "- If the article is not about a transfer, return transferType as Unknown.\n" +
            "- estimatedFee should be a number only when a transfer fee is clearly mentioned.\n" +
            "- confidence must be a number between 0 and 1.\n" +
            "- summary should be one concise sentence.\n" +
            "- Use null for any field that cannot be confidently determined.\n\n" +
            "News title:\n" +
            title + "\n\n" +
            "News content:\n" +
            content;

        ChatCompletion completion = await _chatClient.CompleteChatAsync(prompt);

        var json = CleanJson(completion.Content[0].Text);

        try
        {
            var result = JsonSerializer.Deserialize<OpenAiTransferResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? CreateFallbackResult(content);
        }
        catch
        {
            return CreateFallbackResult(content);
        }
    }

    private OpenAiTransferResult CreateFallbackResult(string content)
    {
        var summary = content.Length > 150
            ? content.Substring(0, 150) + "..."
            : content;

        return new OpenAiTransferResult
        {
            Player = null,
            Club = null,
            FromClub = null,
            ToClub = null,
            TransferType = "Unknown",
            EstimatedFee = null,
            Confidence = 0.3,
            Summary = summary
        };
    }

    private string CleanJson(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```json"))
        {
            text = text.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (text.StartsWith("```"))
        {
            text = text.Replace("```", "").Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }

        return text;
    }
}