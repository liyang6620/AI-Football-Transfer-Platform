using System.Globalization;
using System.Text.RegularExpressions;

namespace FootballTransfer.Api.Services;

internal readonly record struct MoneyAmountMatch(
    decimal Millions,
    string Currency,
    int Index,
    int Length);

public static partial class TransferFeeNormalizer
{
    public static decimal? NormalizeToMillions(decimal? modelValue, string? sourceText)
    {
        if (modelValue is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return modelValue;
        }

        var candidates = FindAmounts(sourceText).Select(candidate => candidate.Millions).ToList();

        var exactMatch = candidates.FirstOrDefault(candidate => ApproximatelyEqual(candidate, modelValue.Value));
        if (exactMatch > 0)
        {
            return exactMatch;
        }

        var thousandFoldCorrection = candidates.FirstOrDefault(candidate =>
            ApproximatelyEqual(candidate * 1000, modelValue.Value));
        if (thousandFoldCorrection > 0)
        {
            return thousandFoldCorrection;
        }

        return modelValue;
    }

    public static IReadOnlyList<(decimal Millions, string Currency)> FindAmounts(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return FindAmountMatches(text)
            .Select(match => (match.Millions, match.Currency))
            .ToList();
    }

    internal static IReadOnlyList<MoneyAmountMatch> FindAmountMatches(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var amounts = new List<MoneyAmountMatch>();
        foreach (Match match in MoneyAmountRegex().Matches(text))
        {
            var currency = NormalizeCurrency(match.Groups["currency"].Value);
            if (currency is null || !TryConvertToMillions(match, out var millions))
            {
                continue;
            }

            amounts.Add(new MoneyAmountMatch(millions, currency, match.Index, match.Length));
        }

        return amounts;
    }

    internal static bool ApproximatelyEqual(decimal left, decimal right)
    {
        var tolerance = Math.Max(0.001m, Math.Abs(right) * 0.001m);
        return Math.Abs(left - right) <= tolerance;
    }

    private static bool TryConvertToMillions(Match match, out decimal millions)
    {
        millions = 0;
        var rawAmount = match.Groups["amount"].Value;
        var normalizedAmount = rawAmount.Replace(",", string.Empty).Replace(" ", string.Empty);
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        millions = unit switch
        {
            "bn" or "billion" => amount * 1000,
            "m" or "mn" or "million" => amount,
            "k" or "thousand" => amount / 1000,
            _ when rawAmount.Contains(',') || rawAmount.Contains(' ') => amount / 1_000_000,
            _ => 0
        };

        return millions > 0;
    }

    private static string? NormalizeCurrency(string currency) => currency.ToUpperInvariant() switch
    {
        "£" or "GBP" => "GBP",
        "€" or "EUR" => "EUR",
        "$" or "USD" => "USD",
        _ => null
    };

    [GeneratedRegex(
        @"(?<!\p{L})(?<currency>GBP|EUR|USD|\p{Sc})\s*(?<amount>\d{1,3}(?:[,\s]\d{3})+|\d+(?:\.\d+)?)\s*(?<unit>billion|bn|million|mn|m|thousand|k)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MoneyAmountRegex();
}
