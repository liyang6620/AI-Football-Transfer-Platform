namespace FootballTransfer.Api.Models;

public class Transfer
{
    public int Id { get; set; }

    public string? PlayerName { get; set; }

    public string? FromClub { get; set; }

    public string? ToClub { get; set; }

    public string? TransferType { get; set; }

    public decimal? EstimatedFee { get; set; }

    public string? FeeCurrency { get; set; }

    public bool IsLoan { get; set; }

    public bool IsLoanFee { get; set; }

    public double? Confidence { get; set; }

    public string EventKey { get; set; } = string.Empty;

    public int TransferNewsId { get; set; }

    public TransferNews? TransferNews { get; set; }

    public ICollection<TransferNews> Sources { get; set; } = [];

    public DateTime PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
