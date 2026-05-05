using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Services;

/// <summary>
/// Reads market data from the MarketDataBars table first.
/// Falls back to Yahoo Finance only if the ticker isn't pre-downloaded.
/// </summary>
public class DatabaseFirstMarketDataService : IMarketDataService
{
    private readonly ArTradingDbContext _db;
    private readonly YahooFinanceService _yahoo;
    private readonly ILogger<DatabaseFirstMarketDataService> _logger;

    public DatabaseFirstMarketDataService(ArTradingDbContext db, YahooFinanceService yahoo, ILogger<DatabaseFirstMarketDataService> logger)
    {
        _db = db;
        _yahoo = yahoo;
        _logger = logger;
    }

    public async Task<List<OhlcvBar>> GetHistoricalDataAsync(string ticker, DateTime start, DateTime end)
    {
        var normalizedTicker = ticker.ToUpperInvariant();

        var dbBars = await _db.MarketDataBars
            .Where(b => b.Ticker == normalizedTicker && b.Date >= start && b.Date <= end)
            .OrderBy(b => b.Date)
            .ToListAsync();

        if (dbBars.Count >= 15)
        {
            _logger.LogInformation("Using {Count} pre-downloaded bars for {Ticker} from database", dbBars.Count, ticker);
            return dbBars.Select(b => new OhlcvBar
            {
                Date = b.Date,
                Open = b.Open,
                High = b.High,
                Low = b.Low,
                Close = b.Close,
                Volume = b.Volume
            }).ToList();
        }

        _logger.LogInformation("Insufficient DB data for {Ticker} ({Count} bars found), falling back to Yahoo Finance", ticker, dbBars.Count);
        return await _yahoo.GetHistoricalDataAsync(ticker, start, end);
    }
}
