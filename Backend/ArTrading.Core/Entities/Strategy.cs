namespace ArTrading.Core.Entities;

public class Strategy
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Rules { get; set; } // JSON format
    public string Status { get; set; } = "draft"; // draft, backtested, paper_trading, live, archived
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<BacktestRun> BacktestRuns { get; set; } = new List<BacktestRun>();
    public virtual ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
