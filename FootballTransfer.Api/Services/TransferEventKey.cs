namespace FootballTransfer.Api.Services;

public static class TransferEventKey
{
    public static string Create(string? playerName, string? toClub, string? transferType)
    {
        var player = ClubNameNormalizer.TextKey(playerName);
        var destination = ClubNameNormalizer.Key(toClub);
        var family = string.Equals(transferType, "Contract", StringComparison.OrdinalIgnoreCase)
            ? "contract"
            : "movement";
        return $"{player}|{destination}|{family}";
    }
}
