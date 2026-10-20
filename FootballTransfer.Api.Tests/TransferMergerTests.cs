using FootballTransfer.Api.Models;
using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class TransferMergerTests
{
    [Fact]
    public void Merge_promotes_rumour_to_completed_transfer()
    {
        var existing = CreateTransfer("Rumour", 0.75);
        var incoming = CreateNews("Completed Transfer", 0.95);

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal("Completed Transfer", existing.TransferType);
    }

    [Fact]
    public void Merge_does_not_downgrade_completed_transfer_to_rumour()
    {
        var existing = CreateTransfer("Completed Transfer", 0.95);
        var incoming = CreateNews("Rumour", 0.75);

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal("Completed Transfer", existing.TransferType);
    }

    [Fact]
    public void Merge_does_not_replace_known_fee_with_null()
    {
        var existing = CreateTransfer("Rumour", 0.75);
        existing.EstimatedFee = 123m;
        existing.FeeCurrency = "GBP";
        var incoming = CreateNews("Rumour", 0.95);
        incoming.EstimatedFee = null;

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal(123m, existing.EstimatedFee);
        Assert.Equal("GBP", existing.FeeCurrency);
        Assert.Equal(0.95, existing.Confidence);
    }

    [Fact]
    public void Merge_does_not_replace_known_club_with_null()
    {
        var existing = CreateTransfer("Rumour", 0.75);
        existing.FromClub = "Paris Saint-Germain";
        var incoming = CreateNews("Rumour", 0.95);
        incoming.FromClub = null;

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal("Paris Saint-Germain", existing.FromClub);
    }

    [Fact]
    public void Merge_fills_missing_club_from_incoming_source()
    {
        var existing = CreateTransfer("Rumour", 0.85);
        existing.FromClub = null;
        var incoming = CreateNews("Rumour", 0.75);
        incoming.FromClub = "Everton";

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal("Everton", existing.FromClub);
    }

    [Fact]
    public void Merge_enforces_contract_fee_invariants()
    {
        var existing = CreateTransfer("Contract", 0.75);
        existing.EstimatedFee = 60m;
        existing.FeeCurrency = "GBP";
        var incoming = CreateNews("Contract", 0.9);
        incoming.EstimatedFee = 45m;
        var result = CreateResult(incoming);
        result.FeeCurrency = "EUR";

        TransferMerger.Merge(existing, incoming, result);

        Assert.Null(existing.EstimatedFee);
        Assert.Null(existing.FeeCurrency);
    }

    [Fact]
    public void Merge_keeps_latest_publication_timestamp()
    {
        var existing = CreateTransfer("Rumour", 0.8);
        existing.PublishedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var incoming = CreateNews("Rumour", 0.8);
        incoming.PublishedAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

        TransferMerger.Merge(existing, incoming, CreateResult(incoming));

        Assert.Equal(incoming.PublishedAt, existing.PublishedAt);
        Assert.Equal(incoming.Id, existing.TransferNewsId);
    }

    [Fact]
    public void Merge_existing_entities_preserves_information_before_duplicate_removal()
    {
        var existing = CreateTransfer("Completed Transfer", 0.9);
        existing.FromClub = null;
        var duplicate = CreateTransfer("Rumour", 0.8);
        duplicate.FromClub = "Brighton";
        duplicate.EstimatedFee = 20m;
        duplicate.FeeCurrency = "GBP";

        TransferMerger.Merge(existing, duplicate);

        Assert.Equal("Completed Transfer", existing.TransferType);
        Assert.Equal("Brighton", existing.FromClub);
        Assert.Equal(20m, existing.EstimatedFee);
        Assert.Equal("GBP", existing.FeeCurrency);
    }

    private static Transfer CreateTransfer(string transferType, double confidence) => new()
    {
        PlayerName = "Alex Hart",
        FromClub = "Chelsea",
        ToClub = "Stoke City",
        TransferType = transferType,
        Confidence = confidence,
        EventKey = "alex hart|stoke city|movement",
        TransferNewsId = 1,
        PublishedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static TransferNews CreateNews(string transferType, double confidence) => new()
    {
        Id = 2,
        ExtractedPlayer = "Alex Hart",
        FromClub = "Chelsea",
        ToClub = "Stoke City",
        TransferType = transferType,
        Confidence = confidence,
        PublishedAt = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc)
    };

    private static OpenAiTransferResult CreateResult(TransferNews news) => new()
    {
        Player = news.ExtractedPlayer,
        FromClub = news.FromClub,
        ToClub = news.ToClub,
        TransferType = news.TransferType,
        EstimatedFee = news.EstimatedFee,
        FeeCurrency = news.EstimatedFee is null ? null : "GBP",
        Confidence = news.Confidence ?? 0
    };
}
