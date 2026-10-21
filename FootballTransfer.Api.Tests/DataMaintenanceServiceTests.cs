using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using FootballTransfer.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FootballTransfer.Api.Tests;

public sealed class DataMaintenanceServiceTests
{
    [Fact]
    public async Task RunPendingAsync_returns_false_when_no_job_is_scheduled()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ran = await service.RunPendingAsync(CancellationToken.None);

        Assert.False(ran);
    }

    [Fact]
    public async Task RunPendingAsync_does_not_repeat_a_completed_job()
    {
        await using var context = CreateContext();
        context.DataMaintenanceJobs.Add(new DataMaintenanceJob
        {
            Name = DataMaintenanceService.EvidenceRebuildJob,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            ItemsProcessed = 42
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ran = await service.RunPendingAsync(CancellationToken.None);

        Assert.False(ran);
        Assert.Equal(42, context.DataMaintenanceJobs.Single().ItemsProcessed);
    }

    [Fact]
    public async Task RunPendingAsync_persists_failed_status_when_processing_throws()
    {
        await using var context = CreateContext();
        context.DataMaintenanceJobs.Add(new DataMaintenanceJob
        {
            Name = DataMaintenanceService.EvidenceRebuildJob,
            Status = "Pending"
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunPendingAsync(CancellationToken.None));

        context.ChangeTracker.Clear();
        var job = await context.DataMaintenanceJobs.SingleAsync();
        Assert.Equal("Failed", job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.Contains("not supported", job.LastError, StringComparison.OrdinalIgnoreCase);
    }

    private static FootballTransferDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FootballTransferDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FootballTransferDbContext(options);
    }

    private static DataMaintenanceService CreateService(FootballTransferDbContext context) =>
        new(context, null!, NullLogger<DataMaintenanceService>.Instance);
}
