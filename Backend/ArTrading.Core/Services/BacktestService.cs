using System.Text.Json;
using ArTrading.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public interface IBacktestService
{
    Task<BacktestRun> RunBacktestAsync(Strategy strategy, DateTime startDate, DateTime endDate, decimal startingCapital);
    Task<BacktestResult> AnalyzeBacktestAsync(BacktestRun backtest);
}

public class BacktestResult
{
    public decimal TotalReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public int WinningTrades { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public List<TradePoint> EquityCurve { get; set; } = new();
}

public class TradePoint
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
}

public class BacktestService : IBacktestService
{
    private readonly ILogger<BacktestService> _logger;

    public BacktestService(ILogger<BacktestService> logger)
    {
        _logger = logger;
    }

    public async Task<BacktestRun> RunBacktestAsync(Strategy strategy, DateTime startDate, DateTime endDate, decimal startingCapital)
    {
        _logger.LogInformation(
            "Starting backtest for strategy {StrategyId} from {StartDate} to {EndDate}",
            strategy.Id, startDate, endDate);

        // Simulated backtest (replace with actual OHLC data + trade simulation)
        var result = await SimulateBacktestAsync(strategy, startDate, endDate, startingCapital);

        var backtest = new BacktestRun
        {
            StrategyId = strategy.Id,
            StartDate = startDate,
            EndDate = endDate,
            StartingCapital = startingCapital,
            FinalValue = result.TotalReturn * startingCapital,
            TotalReturn = result.TotalReturn,
            SharpeRatio = result.SharpeRatio,
            MaxDrawdown = result.MaxDrawdown,
            TotalTrades = result.TotalTrades,
            WinningTrades = result.WinningTrades,
            Results = JsonSerializer.Serialize(result)
        };

        _logger.LogInformation(
            "Backtest complete: TotalReturn={Return}, SharpeRatio={Sharpe}, MaxDD={MaxDD}",
            backtest.TotalReturn, backtest.SharpeRatio, backtest.MaxDrawdown);

        return backtest;
    }

    public async Task<BacktestResult> AnalyzeBacktestAsync(BacktestRun backtest)
    {
        if (backtest.Results == null)
            return new BacktestResult();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<BacktestResult>(backtest.Results, options);
            return result ?? new BacktestResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze backtest results");
            return new BacktestResult();
        }
    }

    private async Task<BacktestResult> SimulateBacktestAsync(Strategy strategy, DateTime startDate, DateTime endDate, decimal startingCapital)
    {
        // Mock implementation — replace with actual OHLC data simulation
        await Task.Delay(100); // Simulate work

        var daysInPeriod = (endDate - startDate).Days;
        var totalTrades = Math.Max(1, daysInPeriod / 7); // ~1 trade per week
        var winningTrades = (int)(totalTrades * 0.55m); // 55% win rate

        return new BacktestResult
        {
            TotalReturn = 0.25m, // 25% return
            SharpeRatio = 1.8m,
            MaxDrawdown = 0.12m, // 12% max drawdown
            WinningTrades = winningTrades,
            TotalTrades = totalTrades,
            WinRate = (decimal)winningTrades / totalTrades,
            EquityCurve = GenerateMockEquityCurve(startDate, endDate, startingCapital)
        };
    }

    private List<TradePoint> GenerateMockEquityCurve(DateTime startDate, DateTime endDate, decimal startingCapital)
    {
        var curve = new List<TradePoint>();
        var currentValue = startingCapital;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            curve.Add(new TradePoint { Date = currentDate, PortfolioValue = currentValue });
            currentValue *= 1.001m; // 0.1% daily growth (mock)
            currentDate = currentDate.AddDays(1);
        }

        return curve;
    }
}
