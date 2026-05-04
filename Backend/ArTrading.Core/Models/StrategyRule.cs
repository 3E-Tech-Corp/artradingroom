namespace ArTrading.Core.Models;

public class StrategyRule
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Condition> EntryConditions { get; set; } = new();
    public List<Condition> ExitConditions { get; set; } = new();
    public PositionSizing PositionSizing { get; set; } = new();
}

public class Condition
{
    public required string Indicator { get; set; } // RSI, MACD, SMA, etc.
    public required string Operator { get; set; } // >, <, ==, >=, <=
    public required decimal Value { get; set; }
}

public class PositionSizing
{
    public decimal PercentageOfPortfolio { get; set; } = 0.05m; // 5% per trade
    public decimal MaxPositionSize { get; set; } = 0.20m; // 20% max
    public string SizingMethod { get; set; } = "fixed"; // fixed or proportional
}
