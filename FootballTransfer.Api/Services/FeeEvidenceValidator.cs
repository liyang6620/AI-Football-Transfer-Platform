using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FootballTransfer.Api.Services;

public static partial class FeeEvidenceValidator
{
    private static readonly HashSet<string> GenericClubWords = new(StringComparer.Ordinal)
    {
        "fc", "afc", "cf", "sc", "ac", "united", "city", "town", "club"
    };

    private static readonly string[] NonTransferAmountSignals =
    [
        "a week", "per week", "-a-week", "a year", "per year", "wages", "salary",
        "release clause", "buyout clause", "valued at", "valuation", "market value"
    ];

    public static (decimal? Fee, string? Currency, string? Reason) Validate(
        decimal? modelFee,
        string? modelCurrency,
        string? evidence,
        string? player,
        string? fromClub,
        string? toClub,
        string title,
        string content)
    {
        _ = modelCurrency;

        if (modelFee is null)
        {
            return (null, null, null);
        }

        if (string.IsNullOrWhiteSpace(evidence))
        {
            return (null, null, "missing evidence");
        }

        evidence = evidence.Trim();
        if (evidence.Length > 200)
        {
            return (null, null, "evidence exceeds 200 characters");
        }

        var normalizedEvidence = NormalizeForComparison(evidence);
        var normalizedArticle = NormalizeForComparison($"{title} {content}");
        if (!normalizedArticle.Contains(normalizedEvidence, StringComparison.Ordinal))
        {
            return (null, null, "evidence not found in article");
        }

        if (!MentionsCurrentEvent(normalizedEvidence, player, fromClub, toClub))
        {
            return (null, null, "evidence does not mention the player or clubs");
        }

        var normalizedModelFee = TransferFeeNormalizer.NormalizeToMillions(modelFee, evidence);
        var amountMatch = TransferFeeNormalizer.FindAmountMatches(evidence)
            .FirstOrDefault(candidate => normalizedModelFee.HasValue
                && TransferFeeNormalizer.ApproximatelyEqual(candidate.Millions, normalizedModelFee.Value));

        if (amountMatch.Millions <= 0)
        {
            return (null, null, "fee not in evidence");
        }

        var contextStart = Math.Max(0, amountMatch.Index - 40);
        var contextEnd = Math.Min(evidence.Length, amountMatch.Index + amountMatch.Length + 40);
        var amountContext = NormalizeForComparison(evidence[contextStart..contextEnd]);
        if (NonTransferAmountSignals.Any(signal =>
                amountContext.Contains(signal, StringComparison.Ordinal)))
        {
            return (null, null, "amount is wages/clause/valuation");
        }

        return (amountMatch.Millions, amountMatch.Currency, null);
    }

    private static bool MentionsCurrentEvent(
        string normalizedEvidence,
        string? player,
        string? fromClub,
        string? toClub)
    {
        var playerSurname = Tokenize(player)
            .LastOrDefault(token => token.Length >= 3);
        if (playerSurname is not null
            && normalizedEvidence.Contains(playerSurname, StringComparison.Ordinal))
        {
            return true;
        }

        return ClubIdentifiers(fromClub)
            .Concat(ClubIdentifiers(toClub))
            .Any(identifier => normalizedEvidence.Contains(identifier, StringComparison.Ordinal));
    }

    private static IEnumerable<string> ClubIdentifiers(string? club) =>
        Tokenize(club)
            .Where(token => token.Length >= 4 && !GenericClubWords.Contains(token));

    private static IEnumerable<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return TokenSeparatorRegex()
            .Split(NormalizeForComparison(value))
            .Where(token => token.Length > 0);
    }

    private static string NormalizeForComparison(string value)
    {
        var standardized = value
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201c', '"')
            .Replace('\u201d', '"')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Normalize(NormalizationForm.FormD);

        var withoutDiacritics = new StringBuilder(standardized.Length);
        foreach (var character in standardized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                withoutDiacritics.Append(character);
            }
        }

        return WhitespaceRegex()
            .Replace(withoutDiacritics.ToString().Normalize(NormalizationForm.FormC), " ")
            .Trim()
            .ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenSeparatorRegex();
}
