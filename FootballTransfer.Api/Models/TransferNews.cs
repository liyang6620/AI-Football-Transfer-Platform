public class TransferNews
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsProcessed { get; set; } = false;

    public string? AiSummary { get; set; }
}