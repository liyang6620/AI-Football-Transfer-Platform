using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Services;

public sealed class DataMaintenanceService
{
    public const string EvidenceRebuildJob = "transfer-evidence-rebuild-2026-10-21";
    private const int BatchSize = 20;
    private const int MaxBatches = 100;

    private readonly FootballTransferDbContext _context;
    private readonly AiAnalysisService _aiAnalysisService;
    private readonly ILogger<DataMaintenanceService> _logger;

    public DataMaintenanceService(
        FootballTransferDbContext context,
        AiAnalysisService aiAnalysisService,
        ILogger<DataMaintenanceService> logger)
    {
        _context = context;
        _aiAnalysisService = aiAnalysisService;
        _logger = logger;
    }

    public async Task<bool> RunPendingAsync(CancellationToken cancellationToken)
    {
        var job = await _context.DataMaintenanceJobs
            .SingleOrDefaultAsync(item => item.Name == EvidenceRebuildJob, cancellationToken);

        if (job is null || job.Status == "Completed")
        {
            return false;
        }

        job.Status = "Running";
        job.StartedAt ??= DateTime.UtcNow;
        job.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _context.TransferNews
                .Where(news => news.AiSummary ==
                    "Automated extraction was unavailable after three attempts; this report was withheld.")
                .ExecuteUpdateAsync(update => update
                    .SetProperty(news => news.IsProcessed, false)
                    .SetProperty(news => news.AiSummary, (string?)null)
                    .SetProperty(news => news.TransferType, (string?)null),
                    cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Starting data maintenance job {JobName}.", job.Name);

            for (var batch = 1; batch <= MaxBatches; batch++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _aiAnalysisService.ProcessMaintenanceBatchAsync(
                    BatchSize,
                    cancellationToken);

                // A failed extraction retry clears the shared context so the next
                // attempt starts clean. Reacquire the job in case that detached it.
                job = await _context.DataMaintenanceJobs
                    .SingleAsync(item => item.Name == EvidenceRebuildJob, cancellationToken);
                job.ItemsProcessed += result.Attempted;

                var remaining = await _context.TransferNews
                    .CountAsync(news => !news.IsProcessed, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Maintenance job {JobName} batch {Batch} attempted {Attempted}, completed {Successful}; {Remaining} remain.",
                    job.Name,
                    batch,
                    result.Attempted,
                    result.Successful,
                    remaining);

                if (remaining == 0)
                {
                    job.Status = "Completed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.LastError = null;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Completed data maintenance job {JobName}; processed {Processed} items.",
                        job.Name,
                        job.ItemsProcessed);
                    return true;
                }

                if (result.Attempted == 0)
                {
                    throw new InvalidOperationException(
                        $"Maintenance job made no progress with {remaining} items remaining.");
                }
            }

            throw new InvalidOperationException(
                $"Maintenance job exceeded the {MaxBatches}-batch safety limit.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The extraction retry path may have cleared the change tracker,
            // including the maintenance job loaded above.
            _context.ChangeTracker.Clear();
            var failedJob = await _context.DataMaintenanceJobs
                .SingleAsync(item => item.Name == EvidenceRebuildJob, CancellationToken.None);
            failedJob.Status = "Failed";
            failedJob.LastError = exception.Message.Length > 500
                ? exception.Message[..500]
                : exception.Message;
            await _context.SaveChangesAsync(CancellationToken.None);
            _logger.LogError(exception, "Data maintenance job {JobName} failed.", job.Name);
            throw;
        }
    }
}
