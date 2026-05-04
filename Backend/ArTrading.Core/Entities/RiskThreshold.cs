namespace ArTrading.Core.Entities;

public class RiskThreshold
{
    public int Id { get; set; }
    public string Name { get; set; } = "default";
    public decimal MaxDrawdown { get; set; } = 0.20m; // 20%
    public decimal MaxPositionSize { get; set; } = 0.10m; // 10% of portfolio
    public decimal DailyLossLimit { get; set; } = 0.05m; // 5% daily loss
    public decimal MaxLeverage { get; set; } = 1.0m; // No margin by default
    public bool RequireApproval { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
