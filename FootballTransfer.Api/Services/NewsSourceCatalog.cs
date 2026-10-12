namespace FootballTransfer.Api.Services;

public sealed record NewsSourceDefinition(string Name, string Url);

public static class NewsSourceCatalog
{
    public static IReadOnlyList<NewsSourceDefinition> All { get; } =
    [
        new("BBC Sport Football", "https://feeds.bbci.co.uk/sport/football/rss.xml"),
        new("The Guardian Football", "https://www.theguardian.com/football/transfer-window/rss"),
        new("Google News Football Transfers", "https://news.google.com/rss/search?q=football+transfer+OR+football+signing+OR+football+loan&hl=en-GB&gl=GB&ceid=GB:en")
    ];
}
