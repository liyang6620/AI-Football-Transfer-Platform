namespace FootballTransfer.Api.Models;

public class OpenAiTransferResult
{
    public string? Player { get; set; }

    public string? Club { get; set; }

    public string? FromClub { get; set; }

    public string? ToClub { get; set; }

    public string? TransferType { get; set; }

    public decimal? EstimatedFee { get; set; }

    public double Confidence { get; set; }

    public string? Summary { get; set; }
}