using System.Globalization;
using System.Text.RegularExpressions;

namespace FootballTransfer.Api.Services;

public static partial class TransferFeeNormalizer
{
    public static decimal? NormalizeToMillions(decimal? modelValue, string? sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return modelValue;
        }

        var candidates = new List<decimal>();

        foreach (Match match in MoneyAmountRegex().Matches(sourceText))
        {
            var rawAmount = match.Groups["amount"].Value;
            var normalizedAmount = rawAmount.Replace(",", string.Empty).Replace(" ", string.Empty);
            if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                continue;
            }

            var unit = match.Groups["unit"].Value.ToLowerInvariant();
            if (unit is "bn" or "billion")
            {
                candidates.Add(amount * 1000);
                continue;
            }

            if (unit is "m" or "mn" or "million")
            {
                candidates.Add(amount);
                continue;
            }

            if (unit is "k" or "thousand")
            {
                candidates.Add(amount / 1000);
                continue;
            }

            // A grouped amount such as GBP 500,000 is expressed in base currency,
            // while the application stores all fees in millions.
            if (rawAmount.Contains(',') || rawAmount.Contains(' '))
            {
                candidates.Add(amount / 1_000_000);
            }
        }

        if (modelValue is null)
        {
            return candidates.Count > 0 ? candidates[0] : null;
        }

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

    private static bool ApproximatelyEqual(decimal left, decimal right)
    {
        var tolerance = Math.Max(0.001m, Math.Abs(right) * 0.001m);
        return Math.Abs(left - right) <= tolerance;
    }

    [GeneratedRegex(
        @"(?:GBP|EUR|USD|\p{Sc})\s*(?<amount>\d{1,3}(?:[,\s]\d{3})+|\d+(?:\.\d+)?)\s*(?<unit>billion|bn|million|mn|m|thousand|k)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MoneyAmountRegex();
}
