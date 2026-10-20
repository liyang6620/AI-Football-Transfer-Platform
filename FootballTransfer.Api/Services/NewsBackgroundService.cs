using Microsoft.Extensions.DependencyInjection;

namespace FootballTransfer.Api.Services;

public class NewsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public NewsBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NewsBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            _logger.LogInformation("News background service is disabled by configuration.");
            return;
        }

        var initialDelaySeconds = Math.Max(0, _configuration.GetValue("BackgroundJobs:InitialDelaySeconds", 120));
        var intervalHours = Math.Max(1, _configuration.GetValue("BackgroundJobs:IntervalHours", 24));
        var batchSize = Math.Clamp(_configuration.GetValue("BackgroundJobs:BatchSize", 10), 1, 20);
        var maxBatchesPerRun = Math.Clamp(_configuration.GetValue("BackgroundJobs:MaxBatchesPerRun", 2), 1, 10);

        _logger.LogInformation("News background service started.");

        if (initialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(initialDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var maintenanceService = scope.ServiceProvider.GetRequiredService<DataMaintenanceService>();
                var crawlerService = scope.ServiceProvider.GetRequiredService<NewsCrawlerService>();
                var aiAnalysisService = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();

                await maintenanceService.RunPendingAsync(stoppingToken);

                _logger.LogInformation("Starting scheduled news crawling...");

                var addedCount = await crawlerService.CrawlAndSaveAsync();

                _logger.LogInformation(
                    "News crawling completed. Added {Count} new articles.",
                    addedCount
                );

                var totalProcessed = 0;

                for (var batch = 1; batch <= maxBatchesPerRun; batch++)
                {
                    _logger.LogInformation(
                        "Starting AI processing batch {Batch}/{MaxBatchesPerRun}...",
                        batch,
                        maxBatchesPerRun
                    );

                    var processedCount =
                        await aiAnalysisService.ProcessUnprocessedLimitAsync(batchSize);

                    totalProcessed += processedCount;

                    _logger.LogInformation(
                        "AI batch {Batch} completed. Processed {Count} news items.",
                        batch,
                        processedCount
                    );

                    if (processedCount == 0 || processedCount < batchSize)
                    {
                        break;
                    }
                }

                _logger.LogInformation(
                    "Scheduled job completed. Added {AddedCount}, processed {ProcessedCount}.",
                    addedCount,
                    totalProcessed
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in news background service.");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
