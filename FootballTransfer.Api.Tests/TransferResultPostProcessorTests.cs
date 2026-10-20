using FootballTransfer.Api.Models;
using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class TransferResultPostProcessorTests
{
    [Fact]
    public void Apply_clears_fee_for_contract_events()
    {
        var raw = CreateResult("Contract", 42m, "GBP");
        raw.FeeEvidence = "Chelsea confirmed Alex Hart's contract worth £42m";

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Alex Hart signs a new Chelsea contract",
            "Chelsea confirmed Alex Hart's contract worth £42m.");

        Assert.Null(processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
    }

    [Fact]
    public void Apply_sets_free_transfer_fee_to_zero_without_currency()
    {
        var raw = CreateResult("Free Transfer", 5m, "GBP");

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Alex Hart joins Stoke on a free transfer",
            "Stoke announced the signing.");

        Assert.Equal(0, processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
    }

    [Fact]
    public void Apply_clears_fee_above_plausible_limit()
    {
        var raw = CreateResult("Completed Transfer", 300m, "GBP");
        raw.FeeEvidence = "Stoke signed Alex Hart for £300m";

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Stoke sign Alex Hart",
            "Stoke signed Alex Hart for £300m.");

        Assert.Null(processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
        Assert.Contains(processed.Adjustments, reason => reason.Contains("plausible range", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_clears_currency_when_fee_is_null()
    {
        var raw = CreateResult("Rumour", null, "GBP");

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Stoke interested in Alex Hart",
            "The clubs have not discussed a fee.");

        Assert.Null(processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
    }

    [Fact]
    public void Apply_caps_and_downgrades_gossip_column_results()
    {
        var raw = CreateResult("Completed Transfer", null, null);
        raw.Confidence = 0.92;

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Hart joins Stoke - Saturday's gossip",
            "A newspaper reports that Hart has joined Stoke.");

        Assert.Equal("Rumour", processed.Result.TransferType);
        Assert.Equal(0.75, processed.Result.Confidence);
    }

    [Fact]
    public void Apply_marks_non_transfer_profile_as_unknown()
    {
        var raw = CreateResult("Completed Transfer", 35m, "GBP");

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "World Cup profile: Alex Hart",
            "This profile mentions a transfer from several years ago.");

        Assert.Equal("Unknown", processed.Result.TransferType);
        Assert.Equal(0, processed.Result.Confidence);
        Assert.Null(processed.Result.Player);
        Assert.Null(processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
    }

    [Fact]
    public void Apply_marks_contract_talks_as_unknown()
    {
        var raw = CreateResult("Contract", null, null);
        raw.ContractStatus = "talks";

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Alex Hart set for talks over new Chelsea deal",
            "Chelsea plan to open contract talks with Alex Hart next month.");

        Assert.Equal("Unknown", processed.Result.TransferType);
        Assert.Equal(0, processed.Result.Confidence);
        Assert.Equal("Contract talks are not a completed renewal.", processed.Result.Summary);
    }

    [Fact]
    public void Apply_clears_loan_amount_without_explicit_loan_fee_evidence()
    {
        var raw = CreateResult("Completed Transfer", 2m, "GBP");
        raw.IsLoan = true;
        raw.FeeEvidence = "Everton paid £2m for Alex Hart";

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Everton sign Alex Hart on loan",
            "Everton paid £2m for Alex Hart in the season-long loan deal.");

        Assert.True(processed.Result.IsLoan);
        Assert.Null(processed.Result.EstimatedFee);
        Assert.Null(processed.Result.FeeCurrency);
    }

    [Fact]
    public void Apply_keeps_explicit_loan_fee()
    {
        var raw = CreateResult("Completed Transfer", 2m, "GBP");
        raw.IsLoan = true;
        raw.FeeEvidence = "Everton paid a £2m loan fee for Alex Hart";

        var processed = TransferResultPostProcessor.Apply(
            raw,
            "Everton sign Alex Hart on loan",
            "Everton paid a £2m loan fee for Alex Hart in the season-long deal.");

        Assert.True(processed.Result.IsLoan);
        Assert.Equal(2m, processed.Result.EstimatedFee);
        Assert.Equal("GBP", processed.Result.FeeCurrency);
    }

    private static OpenAiTransferResult CreateResult(
        string transferType,
        decimal? fee,
        string? currency) => new()
    {
        Player = "Alex Hart",
        Club = "Stoke City",
        FromClub = "Chelsea",
        ToClub = "Stoke City",
        TransferType = transferType,
        EstimatedFee = fee,
        FeeCurrency = currency,
        Confidence = 0.9,
        Summary = "A fictional transfer report."
    };
}
