using Microsoft.Extensions.DependencyInjection;

namespace FootballTransfer.Api.Services;

public class NewsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsBackgroundService> _logger;

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

                var processedCount = await aiAnalysisService.ProcessUnprocessedLimitAsync(5);

                _logger.LogInformation(
                    "AI processed {Count} news items.",
                    processedCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in news background service.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}