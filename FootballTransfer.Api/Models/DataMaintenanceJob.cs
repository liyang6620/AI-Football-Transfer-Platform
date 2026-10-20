namespace FootballTransfer.Api.Models;

public sealed class DataMaintenanceJob
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ItemsProcessed { get; set; }
    public string? LastError { get; set; }
}
