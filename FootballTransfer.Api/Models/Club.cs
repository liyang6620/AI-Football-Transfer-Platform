namespace FootballTransfer.Api.Models;

public class Club
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string League { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}