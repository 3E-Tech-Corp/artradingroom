namespace ArTrading.Core.Entities;

public class BacktestRun
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal StartingCapital { get; set; }
    public decimal FinalValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public string? Results { get; set; } // JSON with detailed metrics
    public DateTime RunAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Strategy Strategy { get; set; } = null!;
}
