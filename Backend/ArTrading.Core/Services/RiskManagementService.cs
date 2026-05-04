using ArTrading.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public interface IRiskManagementService
{
    Task<RiskCheckResult> ValidateTradeAsync(Trade trade, RiskThreshold thresholds, decimal currentPortfolioValue, decimal currentDrawdown);
    Task<PositionSizeRecommendation> CalculatePositionSizeAsync(decimal portfolioValue, RiskThreshold thresholds, decimal tradePrice);
    Task<bool> CheckDailyLossLimitAsync(decimal dailyLoss, RiskThreshold thresholds);
}

public class RiskCheckResult
{
    public bool IsApproved { get; set; }
    public List<string> Violations { get; set; } = new();
    public string Reasoning { get; set; } = "";
}

public class PositionSizeRecommendation
{
    public decimal RecommendedQuantity { get; set; }
    public decimal PositionValue { get; set; }
    public decimal PercentageOfPortfolio { get; set; }
    public string Reasoning { get; set; } = "";
}

public class RiskManagementService : IRiskManagementService
{
    private readonly ILogger<RiskManagementService> _logger;

    public RiskManagementService(ILogger<RiskManagementService> logger)
    {
        _logger = logger;
    }

    public async Task<RiskCheckResult> ValidateTradeAsync(
        Trade trade,
        RiskThreshold thresholds,
        decimal currentPortfolioValue,
        decimal currentDrawdown)
    {
        var result = new RiskCheckResult { IsApproved = true };
        var violations = new List<string>();

        // Check max drawdown
        if (currentDrawdown > thresholds.MaxDrawdown)
        {
            violations.Add($"Current drawdown ({currentDrawdown:P}) exceeds maximum ({thresholds.MaxDrawdown:P})");
        }

        // Check position size
        var positionValue = trade.Quantity * trade.Price;
        var percentageOfPortfolio = positionValue / currentPortfolioValue;

        if (percentageOfPortfolio > thresholds.MaxPositionSize)
        {
            violations.Add($"Position size ({percentageOfPortfolio:P}) exceeds maximum ({thresholds.MaxPositionSize:P})");
        }

        // Check leverage
        if (thresholds.MaxLeverage < 1.0m && percentageOfPortfolio > 1.0m)
        {
            violations.Add("Leverage exceeds permitted maximum");
        }

        result.IsApproved = violations.Count == 0;
        result.Violations = violations;
        result.Reasoning = result.IsApproved
            ? $"Trade approved. Position size: {percentageOfPortfolio:P} of portfolio."
            : $"Trade rejected. Violations: {string.Join("; ", violations)}";

        _logger.LogInformation(
            "Risk check for trade {TradeAction} {Ticker}: {Result}",
            trade.Action, trade.Ticker, result.IsApproved ? "APPROVED" : "REJECTED");

        return await Task.FromResult(result);
    }

    public async Task<PositionSizeRecommendation> CalculatePositionSizeAsync(
        decimal portfolioValue,
        RiskThreshold thresholds,
        decimal tradePrice)
    {
        var recommendedValue = portfolioValue * thresholds.MaxPositionSize;
        var recommendedQuantity = recommendedValue / tradePrice;

        var recommendation = new PositionSizeRecommendation
        {
            RecommendedQuantity = recommendedQuantity,
            PositionValue = recommendedValue,
            PercentageOfPortfolio = thresholds.MaxPositionSize,
            Reasoning = $"Position size based on {thresholds.MaxPositionSize:P} of portfolio value"
        };

        _logger.LogInformation(
            "Position size calculated: {Quantity} shares @ {Price} = {Value}",
            recommendedQuantity, tradePrice, recommendedValue);

        return await Task.FromResult(recommendation);
    }

    public async Task<bool> CheckDailyLossLimitAsync(decimal dailyLoss, RiskThreshold thresholds)
    {
        var isWithinLimit = Math.Abs(dailyLoss) <= thresholds.DailyLossLimit;

        _logger.LogInformation(
            "Daily loss check: {Loss:P} vs limit {Limit:P} - {Result}",
            dailyLoss, thresholds.DailyLossLimit, isWithinLimit ? "OK" : "EXCEEDED");

        return await Task.FromResult(isWithinLimit);
    }
}
