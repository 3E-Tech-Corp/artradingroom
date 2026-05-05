namespace ArTrading.Core.Entities;

public class MarketDataBar
{
    public int Id { get; set; }
    public string Ticker { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
