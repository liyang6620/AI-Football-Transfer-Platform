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

        _logger.LogInformation("Starting data maintenance job {JobName}.", job.Name);

        try
        {
            var quarantined = 0;
            for (var batch = 1; batch <= MaxBatches; batch++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _aiAnalysisService.ProcessMaintenanceBatchAsync(
                    BatchSize,
                    cancellationToken);
                job.ItemsProcessed += result.Attempted;
                quarantined += result.Quarantined;

                var remaining = await _context.TransferNews
                    .CountAsync(news => !news.IsProcessed, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Maintenance job {JobName} batch {Batch} attempted {Attempted}, completed {Successful}, quarantined {Quarantined}; {Remaining} remain.",
                    job.Name,
                    batch,
                    result.Attempted,
                    result.Successful,
                    result.Quarantined,
                    remaining);

                if (remaining == 0)
                {
                    job.Status = "Completed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.LastError = quarantined == 0
                        ? null
                        : $"Withheld {quarantined} reports after repeated extraction failures.";
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
            job.Status = "Failed";
            job.LastError = exception.Message.Length > 500
                ? exception.Message[..500]
                : exception.Message;
            await _context.SaveChangesAsync(CancellationToken.None);
            _logger.LogError(exception, "Data maintenance job {JobName} failed.", job.Name);
            throw;
        }
    }
}
