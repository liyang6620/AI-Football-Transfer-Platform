using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class FeeEvidenceValidatorTests
{
    [Fact]
    public void Validate_accepts_verbatim_evidence_for_the_current_player()
    {
        const string content = "Chelsea signed Alex Hart for £35m after talks with Stoke.";

        var result = FeeEvidenceValidator.Validate(
            35m, "GBP", content, "Alex Hart", "Stoke City", "Chelsea", "Hart joins Chelsea", content);

        Assert.Equal(35m, result.Fee);
        Assert.Equal("GBP", result.Currency);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Validate_rejects_evidence_not_found_in_article()
    {
        var result = FeeEvidenceValidator.Validate(
            35m, "GBP", "Chelsea paid £35m for Alex Hart", "Alex Hart", "Stoke City", "Chelsea",
            "Hart joins Chelsea", "Chelsea announced the signing for an undisclosed fee.");

        Assert.Null(result.Fee);
        Assert.Equal("evidence not found in article", result.Reason);
    }

    [Fact]
    public void Validate_rejects_amount_for_another_player()
    {
        const string content = "Chelsea announced Alex Hart. Rangers signed Ben Reed for £35m.";

        var result = FeeEvidenceValidator.Validate(
            35m, "GBP", "Rangers signed Ben Reed for £35m", "Alex Hart", "Stoke City", "Chelsea",
            "Hart joins Chelsea", content);

        Assert.Null(result.Fee);
        Assert.Equal("evidence does not mention the player or clubs", result.Reason);
    }

    [Fact]
    public void Validate_rejects_weekly_wages()
    {
        const string evidence = "Alex Hart is earning £150,000 a week at Chelsea";

        var result = FeeEvidenceValidator.Validate(
            150m, "GBP", evidence, "Alex Hart", null, "Chelsea", "Hart signs contract", evidence);

        Assert.Null(result.Fee);
        Assert.Equal("amount is wages/clause/valuation", result.Reason);
    }

    [Fact]
    public void Validate_rejects_release_clause()
    {
        const string evidence = "Nico Vidal has a €60m release clause at Sociedad";

        var result = FeeEvidenceValidator.Validate(
            60m, "EUR", evidence, "Nico Vidal", "Real Sociedad", "Real Madrid",
            "Madrid interested in Vidal", evidence);

        Assert.Null(result.Fee);
        Assert.Equal("amount is wages/clause/valuation", result.Reason);
    }

    [Fact]
    public void Validate_uses_currency_from_evidence()
    {
        const string evidence = "Bayern paid €40m for Luca Ferri";

        var result = FeeEvidenceValidator.Validate(
            40m, "GBP", evidence, "Luca Ferri", "Atalanta", "Bayern Munich",
            "Bayern sign Ferri", evidence);

        Assert.Equal(40m, result.Fee);
        Assert.Equal("EUR", result.Currency);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Validate_corrects_grouped_base_currency_amount()
    {
        const string evidence = "Hearts paid £500,000 for Dan Kerr";

        var result = FeeEvidenceValidator.Validate(
            500m, "GBP", evidence, "Dan Kerr", "Ross County", "Hearts",
            "Hearts sign Kerr", evidence);

        Assert.Equal(0.5m, result.Fee);
        Assert.Equal("GBP", result.Currency);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Validate_returns_empty_result_when_model_fee_is_null()
    {
        var result = FeeEvidenceValidator.Validate(
            null, "GBP", "Chelsea paid £35m for Alex Hart", "Alex Hart", "Stoke City", "Chelsea",
            "Hart joins Chelsea", "Chelsea paid £35m for Alex Hart");

        Assert.Null(result.Fee);
        Assert.Null(result.Currency);
        Assert.Null(result.Reason);
    }
}
