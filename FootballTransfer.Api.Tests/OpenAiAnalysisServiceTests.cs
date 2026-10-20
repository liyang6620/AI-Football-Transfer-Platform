using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class OpenAiAnalysisServiceTests
{
    [Fact]
    public void ParseModelJson_reads_structured_fields()
    {
        const string json = """
            {
              "player": "Kai Brooks",
              "club": "Everton",
              "fromClub": "Brighton",
              "toClub": "Everton",
              "transferType": "Completed Transfer",
              "isLoan": true,
              "contractStatus": null,
              "estimatedFee": 2,
              "feeCurrency": "GBP",
              "feeEvidence": "Everton paid a £2m loan fee for Kai Brooks",
              "confidence": 0.94,
              "summary": "Everton signed Kai Brooks on loan."
            }
            """;

        var result = OpenAiAnalysisService.ParseModelJson(json);

        Assert.Equal("Kai Brooks", result.Player);
        Assert.True(result.IsLoan);
        Assert.Null(result.ContractStatus);
        Assert.Equal("Everton paid a £2m loan fee for Kai Brooks", result.FeeEvidence);
        Assert.Equal(2m, result.EstimatedFee);
    }

    [Fact]
    public void ParseModelJson_returns_unknown_for_invalid_json()
    {
        var result = OpenAiAnalysisService.ParseModelJson("not-json");

        Assert.Equal("Unknown", result.TransferType);
        Assert.Equal(0, result.Confidence);
        Assert.Null(result.Player);
        Assert.Null(result.EstimatedFee);
    }
}
