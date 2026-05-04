using ArTrading.Core.Services;

namespace ArTrading.API.Workers;

public class ResearchWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ResearchWorker> _logger;
    private static readonly TimeSpan _dailyScanInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan _weeklyBriefInterval = TimeSpan.FromDays(7);
    private DateTime _lastScan = DateTime.MinValue;
    private DateTime _lastBrief = DateTime.MinValue;

    public ResearchWorker(IServiceProvider services, ILogger<ResearchWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Research Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Daily market scan
                if (now - _lastScan >= _dailyScanInterval)
                {
                    await RunDailyScanAsync(stoppingToken);
                    _lastScan = now;
                }

                // Weekly research brief
                if (now - _lastBrief >= _weeklyBriefInterval)
                {
                    await RunWeeklyBriefAsync(stoppingToken);
                    _lastBrief = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Research Worker encountered an error");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Research Worker stopped");
    }

    private async Task RunDailyScanAsync(CancellationToken ct)
    {
        _logger.LogInformation("Running daily market scan...");
        using var scope = _services.CreateScope();
        var research = scope.ServiceProvider.GetRequiredService<IResearchAgentService>();
        var insights = await research.ScanMarketDataAsync();
        _logger.LogInformation("Daily scan complete: {InsightCount} insights generated", insights.Count);
    }

    private async Task RunWeeklyBriefAsync(CancellationToken ct)
    {
        _logger.LogInformation("Generating weekly research brief...");
        using var scope = _services.CreateScope();
        var research = scope.ServiceProvider.GetRequiredService<IResearchAgentService>();
        var brief = await research.GenerateWeeklyBriefAsync();
        _logger.LogInformation("Weekly brief complete: '{Theme}' | {Insights} insights, {Ideas} strategy ideas",
            brief.WeeklyTheme, brief.TopInsights.Count, brief.StrategyIdeas.Count);
    }
}
