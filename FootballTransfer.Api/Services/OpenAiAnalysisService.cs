using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            "You are a football transfer data extraction system.\n\n" +
            "Your task is to understand the article and extract ONLY the MAIN CURRENT transfer event.\n" +
            "Return ONLY valid JSON. No markdown. No explanations.\n\n" +

            "ARTICLE UNDERSTANDING RULES:\n" +
            "- First decide what the article is mainly about.\n" +
            "- Only extract a transfer if the MAIN SUBJECT of the article is a current transfer, current signing, current transfer rumour, current bid, current offer, current loan, free transfer, or current contract extension.\n" +
            "- Do NOT extract historical transfers mentioned only as background information, career history, biography, match context, World Cup profile, interview, or historical reference.\n" +
            "- Do NOT use external football knowledge. Use only the title and content provided.\n" +
            "- If the title is mainly about World Cup performance, player profile, biography, interview, match report, injury, record, ranking, analysis, or personal story, return Unknown even if the content mentions an old transfer.\n\n" +

            "Return this JSON format exactly:\n" +
            "{\n" +
            "  \"player\": null,\n" +
            "  \"club\": null,\n" +
            "  \"fromClub\": null,\n" +
            "  \"toClub\": null,\n" +
            "  \"transferType\": \"Unknown\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"feeCurrency\": null,\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"\"\n" +
            "}\n\n" +

            "FIELD RULES:\n" +
            "- transferType must be exactly one of: Rumour, Completed Transfer, Free Transfer, Contract, Unknown.\n" +
            "- estimatedFee must be a number in millions only, or null.\n" +
            "- feeCurrency must be exactly one of: GBP, EUR, USD, or null.\n" +
            "- If estimatedFee is null, feeCurrency must be null.\n" +
            "- If transferType is Free Transfer, estimatedFee must be 0 and feeCurrency must be null.\n\n" +

            "TRANSFER TYPE RULES:\n" +
            "- Use Completed Transfer only if the article's main subject says the player has already signed, joined, completed a move, or the club confirmed the signing.\n" +
            "- Use Rumour if the article's main subject says bid, offer, interested, target, linked with, want, chase, talks, close to signing, or in pole position.\n" +
            "- Use Free Transfer if the main move is clearly a free transfer, free agent signing, out-of-contract signing, or joined on a free.\n" +
            "- Use Contract if the main subject is renewal, extension, new deal, or contract talks.\n" +
            "- Use Unknown if the article is not mainly about a current transfer story.\n\n" +

            "FEE RULES:\n" +
            "- Valid examples: £86m -> estimatedFee 86 and feeCurrency GBP; €100m -> estimatedFee 100 and feeCurrency EUR; $40m -> estimatedFee 40 and feeCurrency USD.\n" +
            "- If the article says 'undisclosed fee', 'fee undisclosed', 'unknown fee', 'fee not disclosed', or 'it is unknown what fee', return estimatedFee null and feeCurrency null.\n" +
            "- Free Transfer means estimatedFee 0 and feeCurrency null.\n" +
            "- Do NOT use numbers for player age, appearances, goals, assists, shirt numbers, contract length, years remaining, seasons, rankings, match statistics, or dates as fees.\n\n" +

            "NEGATIVE EXAMPLE:\n" +
            "Title: Why Kane is different at this World Cup\n" +
            "Correct JSON:\n" +
            "{\n" +
            "  \"player\": null,\n" +
            "  \"club\": null,\n" +
            "  \"fromClub\": null,\n" +
            "  \"toClub\": null,\n" +
            "  \"transferType\": \"Unknown\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"feeCurrency\": null,\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"The article is about Harry Kane's World Cup performance, not a current transfer.\"\n" +
            "}\n\n" +

            "POSITIVE EXAMPLE 1:\n" +
            "Title: Newcastle reject Spurs bid of about £80m for Tonali\n" +
            "Correct JSON:\n" +
            "{\n" +
            "  \"player\": \"Sandro Tonali\",\n" +
            "  \"club\": \"Tottenham Hotspur\",\n" +
            "  \"fromClub\": \"Newcastle United\",\n" +
            "  \"toClub\": \"Tottenham Hotspur\",\n" +
            "  \"transferType\": \"Rumour\",\n" +
            "  \"estimatedFee\": 80,\n" +
            "  \"feeCurrency\": \"GBP\",\n" +
            "  \"confidence\": 0.9,\n" +
            "  \"summary\": \"Newcastle United have rejected Tottenham Hotspur's bid of about £80m for Sandro Tonali.\"\n" +
            "}\n\n" +

            "POSITIVE EXAMPLE 2:\n" +
            "Title: Tottenham sign Scotland forward Hanson from Villa\n" +
            "Content says: undisclosed fee and 22 matches.\n" +
            "Correct JSON:\n" +
            "{\n" +
            "  \"player\": \"Kirsty Hanson\",\n" +
            "  \"club\": \"Tottenham Hotspur\",\n" +
            "  \"fromClub\": \"Aston Villa\",\n" +
            "  \"toClub\": \"Tottenham Hotspur\",\n" +
            "  \"transferType\": \"Completed Transfer\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"feeCurrency\": null,\n" +
            "  \"confidence\": 0.95,\n" +
            "  \"summary\": \"Tottenham Hotspur have signed Kirsty Hanson from Aston Villa for an undisclosed fee.\"\n" +
            "}\n\n" +

            "POSITIVE EXAMPLE 3:\n" +
            "Title: Player joins Club on free transfer\n" +
            "Correct JSON:\n" +
            "{\n" +
            "  \"player\": \"Player\",\n" +
            "  \"club\": \"Club\",\n" +
            "  \"fromClub\": null,\n" +
            "  \"toClub\": \"Club\",\n" +
            "  \"transferType\": \"Free Transfer\",\n" +
            "  \"estimatedFee\": 0,\n" +
            "  \"feeCurrency\": null,\n" +
            "  \"confidence\": 0.9,\n" +
            "  \"summary\": \"Player has joined Club on a free transfer.\"\n" +
            "}\n\n" +

            "News title:\n" +
            title + "\n\n" +
            "News content:\n" +
            content;

        Console.WriteLine("============== OPENAI INPUT ==============");
        Console.WriteLine("TITLE:");
        Console.WriteLine(title);
        Console.WriteLine();
        Console.WriteLine("CONTENT:");
        Console.WriteLine(content);
        Console.WriteLine("============== END OPENAI INPUT ==============");

        ChatCompletion completion = await _chatClient.CompleteChatAsync(prompt);

        var json = CleanJson(completion.Content[0].Text);

        OpenAiTransferResult result;

        try
        {
            result = JsonSerializer.Deserialize<OpenAiTransferResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? CreateFallbackResult(title, content);
        }
        catch
        {
            result = CreateFallbackResult(title, content);
        }

        ApplySafetyRules(result, title, content);
        ApplyFeeFallback(result, title, content);

        if (result.TransferType == "Free Transfer")
        {
            result.EstimatedFee = 0;
            result.FeeCurrency = null;
        }

        return result;
    }

    private OpenAiTransferResult CreateFallbackResult(string title, string content)
    {
        var summarySource = !string.IsNullOrWhiteSpace(content) ? content : title;

        var summary = summarySource.Length > 150
            ? summarySource.Substring(0, 150) + "..."
            : summarySource;

        return new OpenAiTransferResult
        {
            Player = null,
            Club = null,
            FromClub = null,
            ToClub = null,
            TransferType = "Unknown",
            EstimatedFee = null,
            FeeCurrency = null,
            Confidence = 0,
            Summary = summary
        };
    }

    private static void ApplySafetyRules(OpenAiTransferResult result, string title, string content)
    {
        var titleText = title.ToLowerInvariant();
        var text = $"{title} {content}".ToLowerInvariant();

        var nonTransferTitleSignals = new[]
        {
            "world cup",
            "why ",
            "road to",
            "profile",
            "record",
            "story",
            "biography",
            "interview",
            "analysis",
            "different at this world cup",
            "greatest",
            "ranked",
            "explains"
        };

        var strongCurrentTransferTitleSignals = new[]
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
            " close in on",
            " set to sign"
        };

        var hasStrongTransferTitle = strongCurrentTransferTitleSignals
            .Any(signal => $" {titleText} ".Contains(signal));

        var hasNonTransferTitle = nonTransferTitleSignals
            .Any(signal => titleText.Contains(signal));

        if (hasNonTransferTitle && !hasStrongTransferTitle)
        {
            MarkUnknown(result, "The article is not mainly about a current transfer story.");
            return;
        }

        if (ContainsUndisclosedFee(text))
        {
            result.EstimatedFee = null;
        }
    }

    private static void ApplyFeeFallback(OpenAiTransferResult result, string title, string content)
    {
        var text = $"{title} {content}";

        if (ContainsUndisclosedFee(text.ToLowerInvariant()))
        {
            result.EstimatedFee = null;
            result.FeeCurrency = null;
            return;
        }

        if (result.EstimatedFee.HasValue && result.EstimatedFee.Value > 0)
        {
            if (string.IsNullOrWhiteSpace(result.FeeCurrency))
            {
                result.FeeCurrency = ExtractFeeCurrency(text);
            }

            return;
        }

        var extractedFee = ExtractFeeNumber(text);

        result.EstimatedFee = extractedFee;

        result.FeeCurrency = extractedFee.HasValue
            ? ExtractFeeCurrency(text)
            : null;
    }

    private static decimal? ExtractFeeNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lowerText = text.ToLowerInvariant();

        if (ContainsUndisclosedFee(lowerText))
        {
            return null;
        }

        var patterns = new[]
        {
            @"[£€$]\s?(\d+(?:\.\d+)?)\s?(?:m|million|mn)?",
            @"(?:fee|bid|offer|deal|package|clause|worth)\s+(?:of\s+|worth\s+|about\s+|around\s+)?[£€$]\s?(\d+(?:\.\d+)?)\s?(?:m|million|mn)?",
            @"[£€$]\s?(\d+(?:\.\d+)?)\s?(?:bn|billion)",
            @"(\d+(?:\.\d+)?)\s?(?:m|million|mn)\s?(?:pounds|euros|dollars)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                continue;
            }

            var matchedText = match.Value.ToLowerInvariant();

            if (Regex.IsMatch(matchedText, @"\b(goals?|matches?|appearances?|years?|year-old|assists?|caps?|seasons?)\b"))
            {
                continue;
            }

            if (!decimal.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var fee))
            {
                continue;
            }

            if (matchedText.Contains("bn") || matchedText.Contains("billion"))
            {
                fee *= 1000;
            }

            return fee;
        }

        return null;
    }

    private static string? ExtractFeeCurrency(string text)
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

        var lowerText = text.ToLowerInvariant();

        if (lowerText.Contains("pounds"))
        {
            return "GBP";
        }

        if (lowerText.Contains("euros"))
        {
            return "EUR";
        }

        if (lowerText.Contains("dollars"))
        {
            return "USD";
        }

        return null;
    }

    private static bool ContainsUndisclosedFee(string text)
    {
        return text.Contains("undisclosed fee")
            || text.Contains("fee undisclosed")
            || text.Contains("unknown fee")
            || text.Contains("fee not disclosed")
            || text.Contains("it is unknown what fee")
            || text.Contains("unknown what fee");
    }

    private static void MarkUnknown(OpenAiTransferResult result, string summary)
    {
        result.Player = null;
        result.Club = null;
        result.FromClub = null;
        result.ToClub = null;
        result.TransferType = "Unknown";
        result.EstimatedFee = null;
        result.Confidence = 0;
        result.Summary = summary;
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