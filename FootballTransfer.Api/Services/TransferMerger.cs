using FootballTransfer.Api.Models;

namespace FootballTransfer.Api.Services;

public static class TransferMerger
{
    public static void Merge(
        Transfer existing,
        TransferNews incoming,
        OpenAiTransferResult incomingResult)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(incomingResult);

        MergeCore(
            existing,
            incoming.ExtractedPlayer,
            incoming.FromClub,
            incoming.ToClub,
            incoming.TransferType,
            incoming.EstimatedFee,
            incomingResult.FeeCurrency,
            incomingResult.IsLoan,
            incomingResult.IsLoan && incoming.EstimatedFee is > 0,
            incoming.Confidence,
            incoming.Id,
            incoming.PublishedAt,
            incoming.PublishedAt);
    }

    public static void Merge(Transfer existing, Transfer duplicate)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(duplicate);

        MergeCore(
            existing,
            duplicate.PlayerName,
            duplicate.FromClub,
            duplicate.ToClub,
            duplicate.TransferType,
            duplicate.EstimatedFee,
            duplicate.FeeCurrency,
            duplicate.IsLoan,
            duplicate.IsLoanFee,
            duplicate.Confidence,
            duplicate.TransferNewsId,
            duplicate.PublishedAt,
            duplicate.TransferNews?.PublishedAt ?? duplicate.PublishedAt);
    }

    private static void MergeCore(
        Transfer existing,
        string? incomingPlayer,
        string? incomingFromClub,
        string? incomingToClub,
        string? incomingType,
        decimal? incomingFee,
        string? incomingCurrency,
        bool incomingIsLoan,
        bool incomingIsLoanFee,
        double? incomingConfidence,
        int incomingPrimaryNewsId,
        DateTime incomingPublishedAt,
        DateTime incomingPrimaryPublishedAt)
    {
        var existingConfidence = existing.Confidence ?? 0;
        var newConfidence = incomingConfidence ?? 0;
        var existingPrimaryPublishedAt = existing.TransferNews?.PublishedAt ?? existing.PublishedAt;
        var incomingIsPreferred = newConfidence > existingConfidence
            || (newConfidence.Equals(existingConfidence)
                && incomingPrimaryPublishedAt > existingPrimaryPublishedAt);

        existing.PlayerName = MergeText(existing.PlayerName, incomingPlayer, incomingIsPreferred);
        existing.FromClub = MergeText(existing.FromClub, incomingFromClub, incomingIsPreferred);
        existing.ToClub = MergeText(existing.ToClub, incomingToClub, incomingIsPreferred);
        existing.TransferType = MergeType(
            existing.TransferType,
            incomingType,
            existingConfidence,
            newConfidence);
        existing.IsLoan |= incomingIsLoan;
        existing.IsLoanFee |= incomingIsLoanFee;

        if (incomingFee is not null
            && (existing.EstimatedFee is null || newConfidence > existingConfidence))
        {
            existing.EstimatedFee = incomingFee;
            existing.FeeCurrency = incomingCurrency;
        }

        if (incomingIsPreferred)
        {
            existing.TransferNewsId = incomingPrimaryNewsId;
        }

        existing.Confidence = Math.Max(existingConfidence, newConfidence);
        existing.PublishedAt = existing.PublishedAt >= incomingPublishedAt
            ? existing.PublishedAt
            : incomingPublishedAt;

        TransferResultPostProcessor.EnforceInvariants(existing);
    }

    private static string? MergeText(string? existing, string? incoming, bool incomingIsPreferred)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return Clean(incoming);
        }

        if (string.IsNullOrWhiteSpace(incoming))
        {
            return Clean(existing);
        }

        return incomingIsPreferred ? Clean(incoming) : Clean(existing);
    }

    private static string? MergeType(
        string? existingType,
        string? incomingType,
        double existingConfidence,
        double incomingConfidence)
    {
        var existingRank = TypeRank(existingType);
        var incomingRank = TypeRank(incomingType);
        if (incomingRank > existingRank)
        {
            return incomingType;
        }

        if (incomingRank == existingRank
            && IsCompletedFreeConflict(existingType, incomingType)
            && incomingConfidence > existingConfidence)
        {
            return incomingType;
        }

        return existingType;
    }

    private static int TypeRank(string? transferType) => transferType switch
    {
        "Completed Transfer" or "Free Transfer" => 3,
        "Rumour" => 1,
        _ => 0
    };

    private static bool IsCompletedFreeConflict(string? left, string? right) =>
        (left == "Completed Transfer" && right == "Free Transfer")
        || (left == "Free Transfer" && right == "Completed Transfer");

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
