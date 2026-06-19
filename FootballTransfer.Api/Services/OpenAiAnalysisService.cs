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
            "  \"club\": \"main target club or null\",\n" +
            "  \"transferType\": \"Rumour / Completed Transfer / Free Transfer / Contract / Unknown\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"one sentence summary\"\n" +
            "}\n\n" +
            "News title:\n" +
            title + "\n\n" +
            "News content:\n" +
            content;

        ChatCompletion completion = await _chatClient.CompleteChatAsync(prompt);

        var json = completion.Content[0].Text;

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
            TransferType = "Unknown",
            EstimatedFee = null,
            Confidence = 0.3,
            Summary = summary
        };
    }
}