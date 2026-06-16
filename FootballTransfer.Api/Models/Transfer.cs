namespace FootballTransfer.Api.Models;

public class Transfer
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public int FromClubId { get; set; }

    public int ToClubId { get; set; }

    public decimal Fee { get; set; }

    public string Status { get; set; } = string.Empty;
}