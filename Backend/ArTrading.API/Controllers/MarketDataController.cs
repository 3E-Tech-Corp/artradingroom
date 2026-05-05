using ArTrading.Core.Entities;
using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly ArTradingDbContext _db;
    private readonly IMarketDataService _yahoo;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(ArTradingDbContext db, IMarketDataService yahoo, ILogger<MarketDataController> logger)
    {
        _db = db;
        _yahoo = yahoo;
        _logger = logger;
    }

    /// <summary>List all symbols we have stored, with bar count and date range.</summary>
    [HttpGet]
    public async Task<IActionResult> ListSymbols()
    {
        var summary = await _db.MarketDataBars
            .GroupBy(b => b.Ticker)
            .Select(g => new
            {
                Ticker    = g.Key,
                BarCount  = g.Count(),
                FirstDate = g.Min(b => b.Date),
                LastDate  = g.Max(b => b.Date),
                FetchedAt = g.Max(b => b.FetchedAt)
            })
            .OrderBy(s => s.Ticker)
            .ToListAsync();

        return Ok(summary);
    }

    /// <summary>Download (or refresh) OHLCV data for a symbol and persist to DB.</summary>
    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] DownloadRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Ticker))
            return BadRequest("Ticker is required");

        req.Ticker = req.Ticker.Trim().ToUpperInvariant();
        var start = req.StartDate == default ? DateTime.UtcNow.AddYears(-5) : req.StartDate;
        var end   = req.EndDate   == default ? DateTime.UtcNow              : req.EndDate;

        _logger.LogInformation("Downloading market data for {Ticker} {Start} to {End}", req.Ticker, start, end);

        List<OhlcvBar> bars;
        try
        {
            bars = await _yahoo.GetHistoricalDataAsync(req.Ticker, start, end);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data for {Ticker}", req.Ticker);
            return BadRequest($"Failed to fetch data from Yahoo Finance: {ex.Message}");
        }

        if (bars.Count == 0)
            return BadRequest($"No data returned for {req.Ticker} in the requested date range");

        // Upsert: delete existing rows in range then bulk insert
        var existing = await _db.MarketDataBars
            .Where(b => b.Ticker == req.Ticker && b.Date >= start && b.Date <= end)
            .ToListAsync();

        _db.MarketDataBars.RemoveRange(existing);

        var now = DateTime.UtcNow;
        var entities = bars.Select(b => new MarketDataBar
        {
            Ticker    = req.Ticker,
            Date      = b.Date,
            Open      = b.Open,
            High      = b.High,
            Low       = b.Low,
            Close     = b.Close,
            Volume    = b.Volume,
            FetchedAt = now
        }).ToList();

        await _db.MarketDataBars.AddRangeAsync(entities);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Saved {Count} bars for {Ticker}", entities.Count, req.Ticker);

        return Ok(new
        {
            Ticker    = req.Ticker,
            BarCount  = entities.Count,
            FirstDate = entities.Min(b => b.Date),
            LastDate  = entities.Max(b => b.Date)
        });
    }

    /// <summary>Get stored bars for a ticker, optional date filter.</summary>
    [HttpGet("{ticker}")]
    public async Task<IActionResult> GetBars(string ticker,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to   = null)
    {
        ticker = ticker.ToUpperInvariant();
        var query = _db.MarketDataBars.Where(b => b.Ticker == ticker);

        if (from.HasValue) query = query.Where(b => b.Date >= from.Value);
        if (to.HasValue)   query = query.Where(b => b.Date <= to.Value);

        var bars = await query.OrderBy(b => b.Date).ToListAsync();
        return Ok(bars);
    }

    /// <summary>Delete all stored data for a ticker.</summary>
    [HttpDelete("{ticker}")]
    public async Task<IActionResult> DeleteTicker(string ticker)
    {
        ticker = ticker.ToUpperInvariant();
        var rows = await _db.MarketDataBars.Where(b => b.Ticker == ticker).ToListAsync();
        if (rows.Count == 0) return NotFound();

        _db.MarketDataBars.RemoveRange(rows);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} bars for {Ticker}", rows.Count, ticker);
        return Ok(new { deleted = rows.Count });
    }
}

public class DownloadRequest
{
    public string Ticker { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
