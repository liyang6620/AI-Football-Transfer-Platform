using Microsoft.Extensions.DependencyInjection;

namespace FootballTransfer.Api.Services;

public class NewsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsBackgroundService> _logger;

    private const int BatchSize = 20;
    private const int MaxBatchesPerRun = 5;

    public NewsBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NewsBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("News background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var crawlerService = scope.ServiceProvider.GetRequiredService<NewsCrawlerService>();
                var aiAnalysisService = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();

                _logger.LogInformation("Starting scheduled news crawling...");

                var addedCount = await crawlerService.CrawlAndSaveAsync();

                _logger.LogInformation(
                    "News crawling completed. Added {Count} new articles.",
                    addedCount
                );

                var totalProcessed = 0;

                for (var batch = 1; batch <= MaxBatchesPerRun; batch++)
                {
                    _logger.LogInformation(
                        "Starting AI processing batch {Batch}/{MaxBatchesPerRun}...",
                        batch,
                        MaxBatchesPerRun
                    );

                    var processedCount =
                        await aiAnalysisService.ProcessUnprocessedLimitAsync(BatchSize);

                    totalProcessed += processedCount;

                    _logger.LogInformation(
                        "AI batch {Batch} completed. Processed {Count} news items.",
                        batch,
                        processedCount
                    );

                    if (processedCount == 0 || processedCount < BatchSize)
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

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}