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
    public List<SimulatedTrade> Trades { get; set; } = new();
}

public class SimulatedTrade
{
    public DateTime Date { get; set; }
    public string Ticker { get; set; } = "";
    public string Action { get; set; } = ""; // BUY or SELL
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PnL { get; set; }
    public string Signal { get; set; } = ""; // e.g. "RSI < 30"
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
        var trades = GenerateMockTrades(startDate, endDate, totalTrades, winningTrades);

        return new BacktestResult
        {
            TotalReturn = 0.25m, // 25% return
            SharpeRatio = 1.8m,
            MaxDrawdown = 0.12m, // 12% max drawdown
            WinningTrades = winningTrades,
            TotalTrades = totalTrades,
            WinRate = (decimal)winningTrades / totalTrades,
            EquityCurve = GenerateMockEquityCurve(startDate, endDate, startingCapital),
            Trades = trades
        };
    }

    private List<SimulatedTrade> GenerateMockTrades(DateTime startDate, DateTime endDate, int totalTrades, int winningTrades)
    {
        var tickers = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META", "TSLA" };
        var signals = new[] { "RSI < 30", "RSI > 70", "MACD crossover", "MA(20) cross MA(50)", "Volume spike" };
        var rng = new Random(42); // deterministic seed for reproducibility
        var trades = new List<SimulatedTrade>();
        var daysInPeriod = (endDate - startDate).Days;

        for (int i = 0; i < totalTrades; i++)
        {
            var isBuy = i % 2 == 0;
            var isWin = i < winningTrades;
            var dayOffset = (int)((double)i / totalTrades * daysInPeriod);
            var date = startDate.AddDays(dayOffset);
            var ticker = tickers[rng.Next(tickers.Length)];
            var price = 100m + rng.Next(-20, 50) + rng.Next(0, 100) * 0.01m;
            var qty = rng.Next(5, 50) * 10;
            var pnl = isWin ? rng.Next(50, 800) : -rng.Next(30, 500);

            trades.Add(new SimulatedTrade
            {
                Date = date,
                Ticker = ticker,
                Action = isBuy ? "BUY" : "SELL",
                Quantity = qty,
                Price = Math.Round(price, 2),
                PnL = pnl,
                Signal = signals[rng.Next(signals.Length)]
            });
        }

        return trades.OrderBy(t => t.Date).ToList();
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
