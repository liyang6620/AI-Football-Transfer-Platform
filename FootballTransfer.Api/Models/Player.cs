namespace FootballTransfer.Api.Models;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Position { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;
}