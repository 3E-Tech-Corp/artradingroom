using System.Text.Json;
using ArTrading.Core.Entities;
using ArTrading.Core.Models;

namespace ArTrading.Core.Services;

public interface ISensitivityService
{
    Task<SensitivityResult> RunAnalysisAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital, SensitivityRequest request);
}

public class SensitivityRequest
{
    public required string Parameter { get; set; } // e.g. "rsiPeriod", "entryValue", "exitValue", "smaFastPeriod"
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Step { get; set; }
}

public class SensitivityResult
{
    public string Parameter { get; set; } = "";
    public List<SensitivityPoint> Points { get; set; } = new();
}

public class SensitivityPoint
{
    public decimal ParameterValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
}

public class SensitivityService : ISensitivityService
{
    private readonly IBacktestService _backtestService;

    public SensitivityService(IBacktestService backtestService)
    {
        _backtestService = backtestService;
    }

    public async Task<SensitivityResult> RunAnalysisAsync(Strategy strategy, string ticker, DateTime startDate, DateTime endDate, decimal startingCapital, SensitivityRequest request)
    {
        var result = new SensitivityResult { Parameter = request.Parameter };
        var points = new List<SensitivityPoint>();

        // Limit to max 50 steps to prevent abuse
        var maxSteps = 50;
        var step = request.Step > 0 ? request.Step : 1;
        var steps = (int)Math.Ceiling((request.Max - request.Min) / step) + 1;
        if (steps > maxSteps)
            step = (request.Max - request.Min) / (maxSteps - 1);

        for (var val = request.Min; val <= request.Max; val += step)
        {
            var modifiedStrategy = CloneWithParameter(strategy, request.Parameter, val);
            var backtest = await _backtestService.RunBacktestWithResultAsync(modifiedStrategy, ticker, startDate, endDate, startingCapital);

            points.Add(new SensitivityPoint
            {
                ParameterValue = Math.Round(val, 2),
                TotalReturn = backtest.TotalReturn,
                SharpeRatio = backtest.SharpeRatio,
                MaxDrawdown = backtest.MaxDrawdown,
                WinRate = backtest.WinRate,
                TotalTrades = backtest.TotalTrades
            });
        }

        result.Points = points;
        return result;
    }

    private static Strategy CloneWithParameter(Strategy original, string parameter, decimal value)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rules = string.IsNullOrEmpty(original.Rules)
            ? new StrategyRule()
            : JsonSerializer.Deserialize<StrategyRule>(original.Rules, options) ?? new StrategyRule();

        rules.Parameters ??= new IndicatorParameters();

        switch (parameter.ToLowerInvariant())
        {
            case "rsiperiod":
                rules.Parameters.RsiPeriod = (int)value;
                break;
            case "smafastperiod":
                rules.Parameters.SmaFastPeriod = (int)value;
                break;
            case "smamediumperiod":
                rules.Parameters.SmaMediumPeriod = (int)value;
                break;
            case "smaslowperiod":
                rules.Parameters.SmaSlowPeriod = (int)value;
                break;
            case "bollingerperiod":
                rules.Parameters.BollingerPeriod = (int)value;
                break;
            case "bollingerstddev":
                rules.Parameters.BollingerStdDev = value;
                break;
            case "entryvalue":
                if (rules.EntryConditions.Count > 0)
                    rules.EntryConditions[0].Value = value;
                break;
            case "exitvalue":
                if (rules.ExitConditions.Count > 0)
                    rules.ExitConditions[0].Value = value;
                break;
            case "positionsize":
                rules.PositionSizing.PercentageOfPortfolio = value;
                break;
        }

        return new Strategy
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            Rules = JsonSerializer.Serialize(rules, options),
            Status = original.Status,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt
        };
    }
}
