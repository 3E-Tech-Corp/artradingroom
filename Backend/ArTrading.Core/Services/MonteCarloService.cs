using ArTrading.Core.Entities;
using ArTrading.Core.Models;

namespace ArTrading.Core.Services;

public interface IMonteCarloService
{
    Task<MonteCarloResult> RunSimulationAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital, int simulations = 1000);
}

public class MonteCarloResult
{
    public int Simulations { get; set; }
    public MonteCarloMetrics TotalReturn { get; set; } = new();
    public MonteCarloMetrics MaxDrawdown { get; set; } = new();
    public MonteCarloMetrics SharpeRatio { get; set; } = new();
    public MonteCarloMetrics WinRate { get; set; } = new();
    public List<decimal> ReturnDistribution { get; set; } = new();
    public List<decimal> DrawdownDistribution { get; set; } = new();
    public BacktestResult OriginalResult { get; set; } = new();
}

public class MonteCarloMetrics
{
    public decimal Mean { get; set; }
    public decimal Median { get; set; }
    public decimal StdDev { get; set; }
    public decimal Percentile5 { get; set; }
    public decimal Percentile25 { get; set; }
    public decimal Percentile75 { get; set; }
    public decimal Percentile95 { get; set; }
}

public class MonteCarloService : IMonteCarloService
{
    private readonly IBacktestService _backtestService;

    public MonteCarloService(IBacktestService backtestService)
    {
        _backtestService = backtestService;
    }

    public async Task<MonteCarloResult> RunSimulationAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital, int simulations = 1000)
    {
        // Run the original backtest to get trade returns
        var original = await _backtestService.RunBacktestWithResultAsync(strategy, ticker, startDate, endDate, startingCapital);

        // Extract per-trade returns (sell trades only)
        var tradeReturns = new List<decimal>();
        for (int i = 0; i < original.Trades.Count; i++)
        {
            var trade = original.Trades[i];
            if (trade.Action == "SELL" && trade.PnL != 0)
            {
                // Find matching buy
                decimal buyValue = trade.Quantity * trade.Price - trade.PnL;
                if (buyValue > 0)
                    tradeReturns.Add(trade.PnL / buyValue);
            }
        }

        if (tradeReturns.Count < 2)
        {
            return new MonteCarloResult
            {
                Simulations = 0,
                OriginalResult = original
            };
        }

        var rng = new Random(42); // deterministic seed for reproducibility
        var simReturns = new List<decimal>();
        var simDrawdowns = new List<decimal>();
        var simSharpes = new List<decimal>();
        var simWinRates = new List<decimal>();

        for (int sim = 0; sim < simulations; sim++)
        {
            // Resample trades with replacement
            var equity = startingCapital;
            var peak = equity;
            var maxDd = 0m;
            var dailyReturns = new List<decimal>();
            int wins = 0;
            int numTrades = tradeReturns.Count;

            for (int t = 0; t < numTrades; t++)
            {
                var idx = rng.Next(tradeReturns.Count);
                var ret = tradeReturns[idx];
                var positionSize = equity * 0.05m; // use default sizing
                var pnl = positionSize * ret;
                equity += pnl;

                dailyReturns.Add(ret);
                if (ret > 0) wins++;

                if (equity > peak) peak = equity;
                var dd = peak > 0 ? (peak - equity) / peak : 0;
                if (dd > maxDd) maxDd = dd;
            }

            var totalReturn = (equity - startingCapital) / startingCapital;
            simReturns.Add(totalReturn);
            simDrawdowns.Add(maxDd);
            simWinRates.Add(numTrades > 0 ? (decimal)wins / numTrades : 0);

            // Compute Sharpe for this simulation
            if (dailyReturns.Count > 1)
            {
                var mean = dailyReturns.Average();
                var variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / dailyReturns.Count;
                var stdev = (decimal)Math.Sqrt((double)variance);
                simSharpes.Add(stdev > 0 ? mean / stdev * (decimal)Math.Sqrt(252) : 0);
            }
            else
            {
                simSharpes.Add(0);
            }
        }

        simReturns.Sort();
        simDrawdowns.Sort();
        simSharpes.Sort();
        simWinRates.Sort();

        return new MonteCarloResult
        {
            Simulations = simulations,
            TotalReturn = ComputeMetrics(simReturns),
            MaxDrawdown = ComputeMetrics(simDrawdowns),
            SharpeRatio = ComputeMetrics(simSharpes),
            WinRate = ComputeMetrics(simWinRates),
            ReturnDistribution = SampleDistribution(simReturns, 50),
            DrawdownDistribution = SampleDistribution(simDrawdowns, 50),
            OriginalResult = original
        };
    }

    private static MonteCarloMetrics ComputeMetrics(List<decimal> sorted)
    {
        if (sorted.Count == 0) return new MonteCarloMetrics();

        var mean = sorted.Average();
        var variance = sorted.Sum(v => (v - mean) * (v - mean)) / sorted.Count;

        return new MonteCarloMetrics
        {
            Mean = Math.Round(mean, 6),
            Median = Math.Round(Percentile(sorted, 50), 6),
            StdDev = Math.Round((decimal)Math.Sqrt((double)variance), 6),
            Percentile5 = Math.Round(Percentile(sorted, 5), 6),
            Percentile25 = Math.Round(Percentile(sorted, 25), 6),
            Percentile75 = Math.Round(Percentile(sorted, 75), 6),
            Percentile95 = Math.Round(Percentile(sorted, 95), 6),
        };
    }

    private static decimal Percentile(List<decimal> sorted, double p)
    {
        var idx = (p / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(idx);
        var upper = (int)Math.Ceiling(idx);
        if (lower == upper) return sorted[lower];
        var frac = (decimal)(idx - lower);
        return sorted[lower] + frac * (sorted[upper] - sorted[lower]);
    }

    private static List<decimal> SampleDistribution(List<decimal> sorted, int bins)
    {
        // Return evenly-spaced samples for histogram rendering
        if (sorted.Count <= bins) return sorted.Select(v => Math.Round(v, 6)).ToList();
        var result = new List<decimal>();
        for (int i = 0; i < bins; i++)
        {
            var idx = (int)((double)i / bins * sorted.Count);
            result.Add(Math.Round(sorted[idx], 6));
        }
        return result;
    }
}
