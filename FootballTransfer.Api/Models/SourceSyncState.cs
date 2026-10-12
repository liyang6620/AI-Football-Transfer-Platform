namespace FootballTransfer.Api.Models;

public sealed class SourceSyncState
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public int ItemsSeen { get; set; }
    public string? LastError { get; set; }
}
