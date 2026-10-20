namespace FootballTransfer.Api.Services;

public static class TransferRelevanceFilter
{
    public static bool IsTransferRelated(string title, string content)
    {
        var titleText = title.ToLowerInvariant();
        var fullText = $"{title} {content}".ToLowerInvariant();
        var wrappedTitle = $" {titleText} ";

        if (titleText.Contains("done deals"))
        {
            return false;
        }

        string[] strongTitleKeywords =
        [
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
            " free transfer",
            " close in on",
            " close to signing",
            " set to sign",
            " set to join",
            " set to have a medical",
            " medical",
            " in talks",
            " interested in",
            " target",
            " wants",
            " agree ",
            " agrees ",
            " agreed ",
            " agreed deal",
            " agreed a deal",
            " agree deal",
            " agreed fee",
            " agree fee",
            " record fee"
        ];

        if (strongTitleKeywords.Any(keyword => wrappedTitle.Contains(keyword)))
        {
            return true;
        }

        string[] excludeKeywords =
        [
            "match report",
            "preview",
            "reaction",
            "injury update",
            "world cup group",
            "penalty",
            "highlights",
            "video"
        ];

        if (excludeKeywords.Any(keyword => fullText.Contains(keyword)))
        {
            return false;
        }

        string[] includeKeywords =
        [
            "transfer",
            "sign ",
            "signs ",
            "signed ",
            "signing",
            "joins",
            "joined",
            "departures",
            "new deal",
            "contract extension",
            "bid",
            "offer",
            "fee",
            "loan",
            "free agent",
            "free transfer",
            "release clause",
            "linked with",
            "interested in",
            "target",
            "chase",
            "wanted by",
            "preparing bid",
            "move to",
            "after leaving",
            "agreed a deal",
            "agreed deal",
            "agreed fee",
            "agree fee",
            "record fee",
            "set to have a medical",
            "set for medical",
            "medical before completing",
            "personal terms",
            "deal is close",
            "deal close",
            "close to securing",
            "close to completing"
        ];

        return includeKeywords.Any(keyword => fullText.Contains(keyword));
    }
}
