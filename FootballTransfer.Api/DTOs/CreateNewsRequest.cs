namespace FootballTransfer.Api.DTOs;

public sealed class CreateNewsRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}
