using ArTrading.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public interface IExecutionService
{
    Task<ExecutionResult> ExecuteTradeAsync(Trade trade, bool isLiveMode = false);
    Task<PortfolioSnapshot> GetPortfolioSnapshotAsync();
    Task<List<Position>> GetOpenPositionsAsync();
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public int? TradeId { get; set; }
    public string Message { get; set; } = "";
    public decimal ExecutionPrice { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class PortfolioSnapshot
{
    public decimal CashBalance { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal DailyPnLPercent { get; set; }
    public decimal TotalPnLPercent { get; set; }
    public List<Position> Positions { get; set; } = new();
}

public class Position
{
    public required string Ticker { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public string PositionType { get; set; } = "long"; // long or short
}

public class ExecutionService : IExecutionService
{
    private readonly ILogger<ExecutionService> _logger;
    private decimal _portfolioValue = 100000m; // Mock initial portfolio
    private decimal _cashBalance = 100000m;
    private Dictionary<string, Position> _positions = new();

    public ExecutionService(ILogger<ExecutionService> logger)
    {
        _logger = logger;
    }

    public async Task<ExecutionResult> ExecuteTradeAsync(Trade trade, bool isLiveMode = false)
    {
        var modeLabel = isLiveMode ? "LIVE" : "PAPER";

        try
        {
            // Mock broker API call (replace with actual broker)
            await Task.Delay(50);

            var executionPrice = trade.Price * (1 + 0.0001m); // Simulate 1 bp slippage
            var totalCost = (trade.Quantity * executionPrice) + trade.Commission;

            // Update cash and positions
            _cashBalance -= totalCost;
            _portfolioValue = _portfolioValue - totalCost;

            // Update position tracking
            UpdatePosition(trade, executionPrice);

            var result = new ExecutionResult
            {
                Success = true,
                Message = $"{modeLabel} trade executed: {trade.Action} {trade.Quantity} {trade.Ticker} @ ${executionPrice}",
                ExecutionPrice = executionPrice,
                ExecutedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "{Mode} EXECUTION: {Action} {Qty} {Ticker} @ ${Price} | Portfolio: ${Portfolio}",
                modeLabel, trade.Action, trade.Quantity, trade.Ticker, executionPrice, _portfolioValue);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trade execution failed: {Ticker}", trade.Ticker);
            return new ExecutionResult
            {
                Success = false,
                Message = $"Execution failed: {ex.Message}"
            };
        }
    }

    public async Task<PortfolioSnapshot> GetPortfolioSnapshotAsync()
    {
        var snapshot = new PortfolioSnapshot
        {
            CashBalance = _cashBalance,
            PortfolioValue = _portfolioValue,
            DailyPnL = 250m, // Mock
            TotalPnL = 5000m, // Mock
            DailyPnLPercent = 0.0025m, // Mock
            TotalPnLPercent = 0.05m, // Mock
            Positions = _positions.Values.ToList()
        };

        return await Task.FromResult(snapshot);
    }

    public async Task<List<Position>> GetOpenPositionsAsync()
    {
        return await Task.FromResult(_positions.Values.ToList());
    }

    private void UpdatePosition(Trade trade, decimal executionPrice)
    {
        if (!_positions.TryGetValue(trade.Ticker, out var position))
        {
            position = new Position
            {
                Ticker = trade.Ticker,
                AverageCost = executionPrice,
                PositionType = "long"
            };
            _positions[trade.Ticker] = position;
        }

        // Simple position tracking (replace with FIFO/LIFO logic)
        if (trade.Action == "BUY")
        {
            position.Quantity += trade.Quantity;
        }
        else if (trade.Action == "SELL" || trade.Action == "COVER")
        {
            position.Quantity -= trade.Quantity;
            if (position.Quantity == 0)
                _positions.Remove(trade.Ticker);
        }
    }
}
