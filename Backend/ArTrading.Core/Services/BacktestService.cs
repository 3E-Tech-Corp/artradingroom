using System.Text.Json;
using ArTrading.Core.Entities;
using ArTrading.Core.Models;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public interface IBacktestService
{
    Task<BacktestRun> RunBacktestAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital);
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
    public List<OhlcvBar> PriceData { get; set; } = new();
}

public class SimulatedTrade
{
    public DateTime Date { get; set; }
    public string Ticker { get; set; } = "";
    public string Action { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PnL { get; set; }
    public string Signal { get; set; } = "";
    public int BarIndex { get; set; } // index into PriceData for chart overlay
}

public class TradePoint
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
}

public class BacktestService : IBacktestService
{
    private readonly ILogger<BacktestService> _logger;
    private readonly IMarketDataService _marketData;

    public BacktestService(ILogger<BacktestService> logger, IMarketDataService marketData)
    {
        _logger = logger;
        _marketData = marketData;
    }

    public async Task<BacktestRun> RunBacktestAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital)
    {
        _logger.LogInformation(
            "Starting backtest for strategy {StrategyId} on {Ticker} from {StartDate} to {EndDate}",
            strategy.Id, ticker, startDate, endDate);

        var result = await ExecuteBacktestAsync(strategy, ticker, startDate, endDate, startingCapital);

        var backtest = new BacktestRun
        {
            StrategyId = strategy.Id,
            Ticker = ticker,
            StartDate = startDate,
            EndDate = endDate,
            StartingCapital = startingCapital,
            FinalValue = startingCapital * (1 + result.TotalReturn),
            TotalReturn = result.TotalReturn,
            SharpeRatio = result.SharpeRatio,
            MaxDrawdown = result.MaxDrawdown,
            TotalTrades = result.TotalTrades,
            WinningTrades = result.WinningTrades,
            Results = JsonSerializer.Serialize(result)
        };

        _logger.LogInformation(
            "Backtest complete: TotalReturn={Return:P2}, SharpeRatio={Sharpe:F2}, MaxDD={MaxDD:P2}, Trades={Trades}",
            backtest.TotalReturn, backtest.SharpeRatio, backtest.MaxDrawdown, backtest.TotalTrades);

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

    private async Task<BacktestResult> ExecuteBacktestAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital)
    {
        // Fetch real market data — request extra bars before startDate for RSI warmup
        var warmupStart = startDate.AddDays(-220); // enough warmup for SMA200
        var bars = await _marketData.GetHistoricalDataAsync(ticker, warmupStart, endDate);

        if (bars.Count < 15)
            throw new InvalidOperationException($"Not enough historical data for {ticker}. Got {bars.Count} bars, need at least 15.");

        // Parse strategy rules
        var rules = ParseStrategyRules(strategy.Rules);

        // Pre-compute all indicators
        var indicators = new Dictionary<string, decimal[]>
        {
            ["RSI"]             = IndicatorCalculator.CalculateRsi(bars),
            ["PRICE_VS_SMA20"]  = IndicatorCalculator.PriceVsSma(bars, 20),
            ["PRICE_VS_SMA50"]  = IndicatorCalculator.PriceVsSma(bars, 50),
            ["PRICE_VS_SMA200"] = IndicatorCalculator.PriceVsSma(bars, 200),
            ["SMA_CROSS_20_50"] = IndicatorCalculator.SmaCrossover(bars, 20, 50),
            ["SMA_CROSS_50_200"]= IndicatorCalculator.SmaCrossover(bars, 50, 200),
            ["BOLLINGER"]       = IndicatorCalculator.BollingerPosition(bars),
        };

        // Find the index where our actual backtest period starts (after warmup)
        var startIdx = bars.FindIndex(b => b.Date >= startDate);
        if (startIdx < 0) startIdx = 0;
        startIdx = Math.Max(startIdx, 200); // enough for SMA200 warmup

        // Run simulation
        var trades = new List<SimulatedTrade>();
        var equityCurve = new List<TradePoint>();
        decimal cash = startingCapital;
        int sharesHeld = 0;
        decimal entryPrice = 0;
        int entryBarIdx = 0;
        string entrySignal = "";

        // Only include bars from the actual start date in PriceData
        var priceData = bars.Skip(startIdx).ToList();
        var barOffset = startIdx; // offset to map simulation index to priceData index

        for (int i = startIdx; i < bars.Count; i++)
        {
            var bar = bars[i];
            var portfolioValue = cash + sharesHeld * bar.Close;
            equityCurve.Add(new TradePoint { Date = bar.Date, PortfolioValue = portfolioValue });

            // Check entry conditions (not in position)
            if (sharesHeld == 0)
            {
                var (shouldEnter, signal) = EvaluateConditions(rules.EntryConditions, indicators, i, bar);
                if (shouldEnter && i + 1 < bars.Count)
                {
                    // Buy at next bar's open
                    var buyPrice = bars[i + 1].Open;
                    var positionSize = cash * rules.PositionSizing.PercentageOfPortfolio;
                    var maxSize = cash * rules.PositionSizing.MaxPositionSize;
                    positionSize = Math.Min(positionSize, maxSize);
                    sharesHeld = (int)(positionSize / buyPrice);

                    if (sharesHeld > 0)
                    {
                        cash -= sharesHeld * buyPrice;
                        entryPrice = buyPrice;
                        entryBarIdx = i + 1;
                        entrySignal = signal;

                        trades.Add(new SimulatedTrade
                        {
                            Date = bars[i + 1].Date,
                            Ticker = ticker,
                            Action = "BUY",
                            Quantity = sharesHeld,
                            Price = Math.Round(buyPrice, 2),
                            PnL = 0,
                            Signal = signal,
                            BarIndex = i + 1 - barOffset
                        });
                    }
                }
            }
            // Check exit conditions (in position)
            else
            {
                var (shouldExit, signal) = EvaluateConditions(rules.ExitConditions, indicators, i, bar);
                if (shouldExit && i + 1 < bars.Count)
                {
                    // Sell at next bar's open
                    var sellPrice = bars[i + 1].Open;
                    var proceeds = sharesHeld * sellPrice;
                    var pnl = (sellPrice - entryPrice) * sharesHeld;
                    cash += proceeds;

                    trades.Add(new SimulatedTrade
                    {
                        Date = bars[i + 1].Date,
                        Ticker = ticker,
                        Action = "SELL",
                        Quantity = sharesHeld,
                        Price = Math.Round(sellPrice, 2),
                        PnL = Math.Round(pnl, 2),
                        Signal = signal,
                        BarIndex = i + 1 - barOffset
                    });

                    sharesHeld = 0;
                }
            }
        }

        // Close any open position at end
        if (sharesHeld > 0)
        {
            var lastBar = bars[^1];
            var pnl = (lastBar.Close - entryPrice) * sharesHeld;
            cash += sharesHeld * lastBar.Close;

            trades.Add(new SimulatedTrade
            {
                Date = lastBar.Date,
                Ticker = ticker,
                Action = "SELL",
                Quantity = sharesHeld,
                Price = Math.Round(lastBar.Close, 2),
                PnL = Math.Round(pnl, 2),
                Signal = "End of period",
                BarIndex = bars.Count - 1 - barOffset
            });

            sharesHeld = 0;
        }

        // Compute metrics
        var finalValue = cash;
        var totalReturn = (finalValue - startingCapital) / startingCapital;
        var winningTrades = trades.Count(t => t.Action == "SELL" && t.PnL > 0);
        var sellTrades = trades.Count(t => t.Action == "SELL");
        var maxDrawdown = ComputeMaxDrawdown(equityCurve);
        var sharpeRatio = ComputeSharpeRatio(equityCurve);

        return new BacktestResult
        {
            TotalReturn = Math.Round(totalReturn, 6),
            SharpeRatio = Math.Round(sharpeRatio, 4),
            MaxDrawdown = Math.Round(maxDrawdown, 6),
            WinningTrades = winningTrades,
            TotalTrades = sellTrades,
            WinRate = sellTrades > 0 ? Math.Round((decimal)winningTrades / sellTrades, 4) : 0,
            EquityCurve = equityCurve,
            Trades = trades,
            PriceData = priceData
        };
    }

    private StrategyRule ParseStrategyRules(string? rulesJson)
    {
        if (string.IsNullOrEmpty(rulesJson))
            return new StrategyRule();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<StrategyRule>(rulesJson, options) ?? new StrategyRule();
        }
        catch
        {
            return new StrategyRule();
        }
    }

    private (bool triggered, string signal) EvaluateConditions(List<Condition> conditions, Dictionary<string, decimal[]> indicators, int barIdx, OhlcvBar bar)
    {
        foreach (var cond in conditions)
        {
            var key = cond.Indicator.ToUpperInvariant();
            decimal indicatorValue = indicators.TryGetValue(key, out var arr) && barIdx < arr.Length
                ? arr[barIdx]
                : 50m; // unsupported indicator defaults to neutral

            bool met = cond.Operator switch
            {
                "<" => indicatorValue < cond.Value,
                "<=" => indicatorValue <= cond.Value,
                ">" => indicatorValue > cond.Value,
                ">=" => indicatorValue >= cond.Value,
                "==" => indicatorValue == cond.Value,
                _ => false
            };

            if (met)
                return (true, $"{cond.Indicator} {cond.Operator} {cond.Value} (actual: {indicatorValue:F1})");
        }

        return (false, "");
    }

    private decimal ComputeMaxDrawdown(List<TradePoint> curve)
    {
        if (curve.Count == 0) return 0;

        decimal peak = curve[0].PortfolioValue;
        decimal maxDd = 0;

        foreach (var point in curve)
        {
            if (point.PortfolioValue > peak)
                peak = point.PortfolioValue;

            var dd = (peak - point.PortfolioValue) / peak;
            if (dd > maxDd)
                maxDd = dd;
        }

        return maxDd;
    }

    private decimal ComputeSharpeRatio(List<TradePoint> curve)
    {
        if (curve.Count < 2) return 0;

        var returns = new List<decimal>();
        for (int i = 1; i < curve.Count; i++)
        {
            if (curve[i - 1].PortfolioValue == 0) continue;
            returns.Add((curve[i].PortfolioValue - curve[i - 1].PortfolioValue) / curve[i - 1].PortfolioValue);
        }

        if (returns.Count == 0) return 0;

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / returns.Count;
        var stdev = (decimal)Math.Sqrt((double)variance);

        if (stdev == 0) return 0;

        // Annualize: multiply by sqrt(252 trading days)
        return Math.Round(mean / stdev * (decimal)Math.Sqrt(252), 4);
    }
}
