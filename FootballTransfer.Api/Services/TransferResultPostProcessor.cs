using FootballTransfer.Api.Models;

namespace FootballTransfer.Api.Services;

public sealed record PostProcessResult(
    OpenAiTransferResult Result,
    IReadOnlyList<string> Adjustments);

public static class TransferResultPostProcessor
{
    public const decimal MaxPlausibleFeeMillions = 250m;

    private static readonly HashSet<string> ValidTransferTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rumour", "Completed Transfer", "Free Transfer", "Contract", "Unknown"
    };

    private static readonly string[] NonTransferTitleSignals =
    [
        "world cup", "why ", "road to", "profile", "record", "story", "biography",
        "interview", "analysis", "different at this world cup", "greatest", "ranked",
        "explains", "tragedy", "driving", "what is", "plan for", "forgotten man"
    ];

    private static readonly string[] StrongCurrentTransferTitleSignals =
    [
        " sign ", " signs ", " signed ", " signing ", " joins ", " joined ", " new deal",
        " transfer", " bid", " offer", " loan", " close in on", " set to sign",
        " set to join", " set to have a medical", " medical", " reject ", " rejects ",
        " rejected ", " agree ", " agreed ", " agreed deal", " agreed fee", " record fee",
        " weigh up", " interested in"
    ];

    public static PostProcessResult Apply(OpenAiTransferResult raw, string title, string content)
    {
        ArgumentNullException.ThrowIfNull(raw);

        title = (title ?? string.Empty).Trim();
        content = (content ?? string.Empty).Trim();
        var adjustments = new List<string>();

        Normalize(raw);

        if (HasNonTransferTitle(title))
        {
            MarkUnknown(raw, "The article is not mainly about a current transfer story.");
            adjustments.Add("Marked Unknown because the title is not about a current transfer.");
            return Finish(raw, adjustments);
        }

        if (title.Contains("gossip", StringComparison.OrdinalIgnoreCase))
        {
            if (raw.TransferType is "Completed Transfer" or "Free Transfer")
            {
                raw.TransferType = "Rumour";
                adjustments.Add("Downgraded a gossip-column event to Rumour.");
            }

            if (raw.Confidence > 0.75)
            {
                raw.Confidence = 0.75;
                adjustments.Add("Capped gossip-column confidence at 0.75.");
            }
        }

        raw.EstimatedFee = TransferFeeNormalizer.NormalizeToMillions(
            raw.EstimatedFee,
            $"{title} {content}");

        if (ContainsUndisclosedFee($"{title} {content}"))
        {
            ClearFee(raw);
            adjustments.Add("Cleared the fee because the article says it is undisclosed.");
        }

        ApplyTypeRules(raw, adjustments);
        ApplyPlausibilityRules(raw, adjustments);
        ApplyResultInvariants(raw, adjustments);
        ApplyConfidenceRules(raw);

        return new PostProcessResult(raw, adjustments);
    }

    public static void EnforceInvariants(Transfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);

        if (transfer.TransferType == "Contract")
        {
            transfer.EstimatedFee = null;
            transfer.FeeCurrency = null;
        }
        else if (transfer.TransferType == "Free Transfer")
        {
            transfer.EstimatedFee = 0;
            transfer.FeeCurrency = null;
        }

        if (transfer.EstimatedFee is null
            || transfer.EstimatedFee <= 0
            || transfer.EstimatedFee > MaxPlausibleFeeMillions
            || !IsValidCurrency(transfer.FeeCurrency))
        {
            if (transfer.TransferType != "Free Transfer")
            {
                transfer.EstimatedFee = null;
            }

            transfer.FeeCurrency = null;
        }
    }

    public static void EnforceInvariants(TransferNews news)
    {
        ArgumentNullException.ThrowIfNull(news);

        if (news.TransferType == "Contract")
        {
            news.EstimatedFee = null;
        }
        else if (news.TransferType == "Free Transfer")
        {
            news.EstimatedFee = 0;
        }
        else if (news.EstimatedFee is <= 0 or > MaxPlausibleFeeMillions)
        {
            news.EstimatedFee = null;
        }
    }

    private static PostProcessResult Finish(OpenAiTransferResult result, List<string> adjustments)
    {
        ApplyResultInvariants(result, adjustments);
        ApplyConfidenceRules(result);
        return new PostProcessResult(result, adjustments);
    }

    private static void Normalize(OpenAiTransferResult result)
    {
        var type = result.TransferType?.Trim();
        result.TransferType = ValidTransferTypes.FirstOrDefault(candidate =>
            string.Equals(candidate, type, StringComparison.OrdinalIgnoreCase)) ?? "Unknown";

        result.Player = CleanField(result.Player);
        result.Club = CleanField(result.Club);
        result.FromClub = CleanField(result.FromClub);
        result.ToClub = CleanField(result.ToClub);
        result.Summary = CleanSummary(result.Summary);
        result.FeeCurrency = NormalizeCurrency(result.FeeCurrency);
    }

    private static void ApplyTypeRules(OpenAiTransferResult result, List<string> adjustments)
    {
        if (result.TransferType == "Unknown")
        {
            ClearFee(result);
            return;
        }

        if (result.TransferType == "Contract")
        {
            if (result.EstimatedFee is not null || result.FeeCurrency is not null)
            {
                adjustments.Add("Cleared the fee from a contract event.");
            }

            ClearFee(result);
            return;
        }

        if (result.TransferType == "Free Transfer")
        {
            result.EstimatedFee = 0;
            result.FeeCurrency = null;
        }
    }

    private static void ApplyPlausibilityRules(OpenAiTransferResult result, List<string> adjustments)
    {
        if (result.TransferType == "Free Transfer" || result.EstimatedFee is null)
        {
            return;
        }

        if (result.EstimatedFee <= 0 || result.EstimatedFee > MaxPlausibleFeeMillions)
        {
            ClearFee(result);
            adjustments.Add($"Cleared a fee outside the plausible range (0, {MaxPlausibleFeeMillions}].");
        }
    }

    private static void ApplyResultInvariants(OpenAiTransferResult result, List<string> adjustments)
    {
        if (result.EstimatedFee is null)
        {
            if (result.FeeCurrency is not null)
            {
                adjustments.Add("Cleared currency because no fee is present.");
            }

            result.FeeCurrency = null;
            return;
        }

        if (result.EstimatedFee > 0 && !IsValidCurrency(result.FeeCurrency))
        {
            ClearFee(result);
            adjustments.Add("Cleared a positive fee without a supported currency.");
        }
    }

    private static void ApplyConfidenceRules(OpenAiTransferResult result)
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

        result.Confidence = Math.Round(Math.Clamp(result.Confidence, 0, 1), 2);
    }

    private static bool HasNonTransferTitle(string title)
    {
        var titleText = title.ToLowerInvariant();
        var hasStrongSignal = StrongCurrentTransferTitleSignals
            .Any(signal => $" {titleText} ".Contains(signal, StringComparison.Ordinal));
        var hasNonTransferSignal = NonTransferTitleSignals
            .Any(signal => titleText.Contains(signal, StringComparison.Ordinal));

        return hasNonTransferSignal && !hasStrongSignal;
    }

    private static bool ContainsUndisclosedFee(string text)
    {
        var normalized = text.ToLowerInvariant();
        return normalized.Contains("undisclosed fee", StringComparison.Ordinal)
            || normalized.Contains("fee undisclosed", StringComparison.Ordinal)
            || normalized.Contains("unknown fee", StringComparison.Ordinal)
            || normalized.Contains("fee not disclosed", StringComparison.Ordinal)
            || normalized.Contains("it is unknown what fee", StringComparison.Ordinal)
            || normalized.Contains("unknown what fee", StringComparison.Ordinal);
    }

    private static string? CleanField(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CleanSummary(string? value)
    {
        var summary = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return summary.Length > 500 ? summary[..500] + "..." : summary;
    }

    private static string? NormalizeCurrency(string? currency) => currency?.Trim().ToUpperInvariant() switch
    {
        "GBP" => "GBP",
        "EUR" => "EUR",
        "USD" => "USD",
        _ => null
    };

    private static bool IsValidCurrency(string? currency) =>
        currency is "GBP" or "EUR" or "USD";

    private static void ClearFee(OpenAiTransferResult result)
    {
        result.EstimatedFee = null;
        result.FeeCurrency = null;
    }

    private static void MarkUnknown(OpenAiTransferResult result, string summary)
    {
        result.Player = null;
        result.Club = null;
        result.FromClub = null;
        result.ToClub = null;
        result.TransferType = "Unknown";
        ClearFee(result);
        result.Confidence = 0;
        result.Summary = summary;
    }
}
