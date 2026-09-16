using System.Text.RegularExpressions;

namespace FootballTransfer.Api.Services;

public static partial class ClubNameNormalizer
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FC Barcelona"] = "Barcelona",
        ["Barcelona FC"] = "Barcelona",
        ["Chelsea Football Club"] = "Chelsea",
        ["Paris Saint-Germain FC"] = "Paris Saint-Germain",
        ["Paris Saint Germain"] = "Paris Saint-Germain",
        ["PSG"] = "Paris Saint-Germain",
        ["FC Bayern Munich"] = "Bayern Munich",
        ["FC Bayern Munchen"] = "Bayern Munich",
        ["Internazionale"] = "Inter Milan",
        ["FC Internazionale Milano"] = "Inter Milan",
        ["Internazionale Milano"] = "Inter Milan",
        ["Real Madrid CF"] = "Real Madrid",
        ["Atletico de Madrid"] = "Atletico Madrid",
        ["Atletico Madrid"] = "Atletico Madrid"
    };

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var name = WhitespaceRegex().Replace(value.Trim(), " ");
        if (Aliases.TryGetValue(name, out var canonical)) return canonical;

        name = FootballClubSuffixRegex().Replace(name, string.Empty).Trim();
        name = FcSuffixRegex().Replace(name, string.Empty).Trim();
        return Aliases.TryGetValue(name, out canonical) ? canonical : name;
    }

    public static string Key(string? value) =>
        NonAlphaNumericRegex().Replace(Normalize(value)?.ToLowerInvariant() ?? string.Empty, string.Empty);

    public static string TextKey(string? value) =>
        NonAlphaNumericRegex().Replace(value?.Trim().ToLowerInvariant() ?? string.Empty, string.Empty);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\s+Football Club$", RegexOptions.IgnoreCase)]
    private static partial Regex FootballClubSuffixRegex();

    [GeneratedRegex(@"\s+F\.?C\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex FcSuffixRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonAlphaNumericRegex();
}
