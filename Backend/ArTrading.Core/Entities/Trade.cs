namespace ArTrading.Core.Entities;

public class Trade
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    public required string Ticker { get; set; }
    public required string Action { get; set; } // BUY, SELL, COVER, SHORT
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public string Mode { get; set; } = "paper"; // paper or live
    public string? Reason { get; set; } // Why the trade was placed
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Strategy Strategy { get; set; } = null!;
}
