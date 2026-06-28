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

            "CRITICAL DECISION RULE:\n" +
            "- Before extracting anything, decide whether the article's MAIN SUBJECT is a current transfer event.\n" +
            "- If the transfer information is only background, career history, old signing history, loan history, squad planning, player profile, World Cup profile, interview, match preview, match report, tactical analysis, or personal story, return Unknown.\n" +
            "- A player being mentioned as previously signed, previously bought, previously loaned, returning from loan, or having cost a fee in the past is NOT a current transfer event.\n" +
            "- Do NOT extract previous transfer fees, previous signings, previous loans, or old moves.\n" +
            "- Do NOT convert a return from loan into a Completed Transfer.\n" +
            "- Do NOT extract a transfer just because the article contains transfer-related words.\n" +
            "- The title and the main focus of the article must both support a current transfer story.\n\n" +

            "ARTICLE UNDERSTANDING RULES:\n" +
            "- First decide what the article is mainly about.\n" +
            "- Only extract a transfer if the MAIN SUBJECT of the article is a current transfer, current signing, current transfer rumour, current bid, current offer, current loan, free transfer, or current contract extension.\n" +
            "- Do NOT extract historical transfers mentioned only as background information, career history, biography, match context, World Cup profile, interview, or historical reference.\n" +
            "- Do NOT use external football knowledge. Use only the title and content provided.\n" +
            "- If the title is mainly about World Cup performance, player profile, biography, interview, match report, injury, record, ranking, analysis, squad plan, manager plan, future role, or personal story, return Unknown even if the content mentions an old transfer.\n" +
            "- If the article is mainly asking what a club's plan is for a player, whether a player is ready, or how a player performed last season, return Unknown unless the main subject is a new current bid, offer, signing, loan, or contract extension.\n\n" +

            "CURRENT TRANSFER SIGNALS:\n" +
            "- Current transfer story usually uses signals such as: has signed, have signed, completed the signing, agreed a deal, agreed a fee, agreed record fee, made a bid, rejected a bid, submitted an offer, preparing an offer, willing to pay, close to signing, set to have a medical, in talks, interested in signing, wants to sign, set to join, joins on loan, signs on a free transfer.\n" +
            "- These signals must describe the article's main current event, not an old event mentioned in background.\n\n" +

            "OLD / BACKGROUND TRANSFER SIGNALS THAT SHOULD USUALLY RETURN UNKNOWN:\n" +
            "- spent last season on loan\n" +
            "- signed in January 2025\n" +
            "- joined last year\n" +
            "- moved from X to Y previously\n" +
            "- was bought for £Xm\n" +
            "- cost the club £Xm in the past\n" +
            "- a £Xm signing from another club in a previous year\n" +
            "- returned from loan\n" +
            "- will be back in the building following his loan spell\n" +
            "- will be given a fresh start\n" +
            "- expected to join pre-season tour\n" +
            "- may go out on loan again\n" +
            "- reports dismissed as speculation\n\n" +

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

            "CONFIDENCE RULES:\n" +
            "- Return a confidence score between 0.00 and 1.00.\n" +
            "- There is no predefined correct score.\n" +
            "- Do not assign confidence based only on transferType.\n" +
            "- Do not use fixed template values for Rumour, Completed Transfer, Free Transfer, or Contract.\n" +
            "- Imagine you are an experienced football journalist reading this article for the first time.\n" +
            "- After reading the entire article, ask yourself: If this were the only article I had, how convinced would I personally be that this transfer information is true?\n" +
            "- Base your judgement on the overall evidence in this specific article.\n" +
            "- Consider the wording of the title, the wording of the article, whether the transfer is the main subject, whether the language is direct or speculative, whether important facts are explicit, whether the article sounds official or speculative, and whether information is quoted or merely suggested.\n" +
            "- Different articles with the same transferType should naturally receive different confidence scores.\n" +
            "- Different completed transfers may also receive different confidence scores.\n" +
            "- Avoid repeatedly returning the same confidence values such as 0.85, 0.88, 0.90, or 0.95.\n" +
            "- If you notice that your confidence values are becoming repetitive, reconsider your reasoning and choose the value that genuinely reflects the strength of evidence in THIS article.\n" +
            "- Confidence should be your own judgement, not a template.\n" +
            "- If transferType is Unknown, confidence must be 0.0.\n\n" +

            "TRANSFER TYPE RULES:\n" +
            "- transferType must be exactly one of: Rumour, Completed Transfer, Free Transfer, Contract, Unknown.\n" +
            "- Use Completed Transfer ONLY if the article explicitly states that the transfer has already been officially completed AND the buying club has officially confirmed or announced the signing.\n" +
            "- Strong Completed Transfer signals include: the club announced, officially announced, officially confirmed, official statement, official club website, confirmed the signing, completed the signing, the player has officially signed, the player has officially joined, unveiled by the club.\n" +
            "- Do NOT classify as Completed Transfer if the article only says: agreed a fee, agreed a deal, agreement reached, record fee agreed, bid accepted, personal terms agreed, expected to sign, close to joining, close to completing, set to join, set to have a medical, medical booked, medical scheduled, subject to medical, formalities remain, or awaiting official confirmation.\n" +
            "- All of the situations above are still Rumour because the transfer has not yet been officially confirmed by the buying club.\n" +
            "- Even if journalists describe the transfer as almost certain, classify it as Rumour until the buying club officially announces the signing.\n" +
            "- Use Rumour for all transfer reports that have not yet received an official club announcement.\n" +
            "- Use Free Transfer only if the player has officially joined on a free transfer.\n" +
            "- Use Contract only if the article is mainly about a contract renewal, contract extension, or a newly signed contract.\n" +
            "- Use Unknown if the article is not mainly about a current transfer story.\n\n" +

            "FEE RULES:\n" +
            "- Valid examples: £86m -> estimatedFee 86 and feeCurrency GBP; €100m -> estimatedFee 100 and feeCurrency EUR; $40m -> estimatedFee 40 and feeCurrency USD.\n" +
            "- If the article says 'undisclosed fee', 'fee undisclosed', 'unknown fee', 'fee not disclosed', or 'it is unknown what fee', return estimatedFee null and feeCurrency null.\n" +
            "- Free Transfer means estimatedFee 0 and feeCurrency null.\n" +
            "- For loans, only extract a fee if the article clearly states the CURRENT loan fee. If no current loan fee is stated, return estimatedFee null and feeCurrency null.\n" +
            "- If the article says 'no option to buy', 'no buy option', or only discusses a loan agreement without a current fee, do not infer a fee.\n" +
            "- Do NOT use the player's old transfer fee, old purchase price, previous signing fee, market value, or historical fee as the current estimatedFee.\n" +
            "- Do NOT use numbers from phrases like 'a £47.2m signing from Inter Milan in 2023' as the current fee.\n" +
            "- Do NOT use numbers for player age, appearances, goals, assists, shirt numbers, contract length, years remaining, seasons, rankings, match statistics, or dates as fees.\n" +
            "- Do NOT use historical fees from old signings as current estimatedFee.\n\n" +

            "NEGATIVE EXAMPLE 1:\n" +
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
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"Newcastle United have rejected Tottenham Hotspur's bid of about £80m for Sandro Tonali.\"\n" +
            "}\n\n" +

            "NEGATIVE FEE EXAMPLE:\n" +
            "Title: Man Utd player set for loan move\n" +
            "Content says: The player is close to joining another club on loan. He was a £47.2m signing from Inter Milan in 2023. There is no option to buy.\n" +
            "Correct JSON:\n" +
            "{\n" +
            "  \"player\": \"Player\",\n" +
            "  \"club\": \"Another Club\",\n" +
            "  \"fromClub\": \"Manchester United\",\n" +
            "  \"toClub\": \"Another Club\",\n" +
            "  \"transferType\": \"Rumour\",\n" +
            "  \"estimatedFee\": null,\n" +
            "  \"feeCurrency\": null,\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"summary\": \"The player is close to joining another club on loan, but no current loan fee is stated.\"\n" +
            "}\n\n" +

            "IMPORTANT FINAL CHECK:\n" +
            "- The examples above use confidence 0.0 only to avoid teaching fixed scores.\n" +
            "- For the real article below, you must choose your own confidence score based on the evidence in the article.\n" +
            "- Do not copy confidence values from the examples.\n" +
            "- Before returning JSON, check that estimatedFee belongs to the CURRENT transfer event, not an old transfer mentioned as background.\n" +
            "- Before deciding transferType, ask yourself: Has the buying club officially confirmed or announced this signing?\n" +
            "- If the answer is NO, transferType should almost always be Rumour rather than Completed Transfer.\n" +
            "- Never classify a transfer as Completed Transfer solely because a fee has been agreed, a medical has been scheduled, or journalists believe the deal is close.\n\n" +

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
        ApplyConfidenceRules(result, title, content);

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
            "explains",
            "tragedy",
            "driving",
            "what is",
            "plan for",
            "forgotten man"
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
            " set to sign",
            " set to join",
            " set to have a medical",
            " medical",
            " reject ",
            " rejects ",
            " rejected ",
            " agree ",
            " agreed ",
            " agreed deal",
            " agreed fee",
            " record fee",
            " weigh up",
            " interested in"
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
            result.FeeCurrency = null;
        }
    }

    private static void ApplyConfidenceRules(OpenAiTransferResult result, string title, string content)
    {
        if (result.TransferType == "Unknown")
        {
            result.Confidence = 0;
            return;
        }

        if (double.IsNaN(result.Confidence) || double.IsInfinity(result.Confidence))
        {
            result.Confidence = 0;
        }

        result.Confidence = Math.Clamp(result.Confidence, 0, 1);
        result.Confidence = Math.Round(result.Confidence, 2);
    }

    private static void ApplyFeeFallback(OpenAiTransferResult result, string title, string content)
    {
        var text = $"{title} {content}";
        var lowerText = text.ToLowerInvariant();

        if (ContainsUndisclosedFee(lowerText))
        {
            result.EstimatedFee = null;
            result.FeeCurrency = null;
            return;
        }

        if (result.TransferType == "Unknown")
        {
            result.EstimatedFee = null;
            result.FeeCurrency = null;
            return;
        }

        if (result.TransferType == "Free Transfer")
        {
            result.EstimatedFee = 0;
            result.FeeCurrency = null;
            return;
        }

        if (IsLoanWithoutCurrentFee(lowerText))
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

    private static bool IsLoanWithoutCurrentFee(string text)
    {
        var hasLoanSignal =
            text.Contains("loan") ||
            text.Contains("on loan") ||
            text.Contains("loan move") ||
            text.Contains("loan deal");

        if (!hasLoanSignal)
        {
            return false;
        }

        var hasCurrentFeeSignal =
            text.Contains("loan fee") ||
            text.Contains("loan fee of") ||
            text.Contains("fee for the loan") ||
            text.Contains("paid a loan fee");

        if (hasCurrentFeeSignal)
        {
            return false;
        }

        return text.Contains("no option to buy")
            || text.Contains("no buy option")
            || text.Contains("no option")
            || text.Contains("without an option to buy")
            || text.Contains("no fee")
            || text.Contains("no current fee")
            || text.Contains("close to securing")
            || text.Contains("set for")
            || text.Contains("set to join")
            || text.Contains("return to");
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

        if (text.Contains('£')) return "GBP";
        if (text.Contains('€')) return "EUR";
        if (text.Contains('$')) return "USD";

        var lowerText = text.ToLowerInvariant();

        if (lowerText.Contains("pounds")) return "GBP";
        if (lowerText.Contains("euros")) return "EUR";
        if (lowerText.Contains("dollars")) return "USD";

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
        result.FeeCurrency = null;
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