using ArTrading.Core.Entities;
using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("[controller]")]
public class TradesController : ControllerBase
{
    private readonly ArTradingDbContext _db;
    private readonly IRiskManagementService _risk;
    private readonly IExecutionService _execution;
    private readonly ILogger<TradesController> _logger;

    public TradesController(
        ArTradingDbContext db,
        IRiskManagementService risk,
        IExecutionService execution,
        ILogger<TradesController> logger)
    {
        _db = db;
        _risk = risk;
        _execution = execution;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListTrades(
        [FromQuery] string? mode = null,
        [FromQuery] int? strategyId = null,
        [FromQuery] int limit = 50)
    {
        var query = _db.Trades.AsQueryable();
        if (mode != null) query = query.Where(t => t.Mode == mode);
        if (strategyId != null) query = query.Where(t => t.StrategyId == strategyId);

        var trades = await query
            .OrderByDescending(t => t.ExecutedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(trades);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTrade(int id)
    {
        var trade = await _db.Trades.FindAsync(id);
        if (trade == null) return NotFound();
        return Ok(trade);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceTrade([FromBody] PlaceTradeRequest request)
    {
        var strategy = await _db.Strategies.FindAsync(request.StrategyId);
        if (strategy == null)
            return BadRequest($"Strategy {request.StrategyId} not found");

        var thresholds = await _db.RiskThresholds.OrderByDescending(r => r.Id).FirstOrDefaultAsync()
            ?? new RiskThreshold();

        var trade = new Trade
        {
            StrategyId = request.StrategyId,
            Ticker = request.Ticker.ToUpper(),
            Action = request.Action.ToUpper(),
            Quantity = request.Quantity,
            Price = request.Price,
            Commission = request.Price * request.Quantity * 0.0001m, // 1 bp commission
            Mode = request.Mode ?? "paper",
            Reason = request.Reason
        };

        // Risk check
        var portfolio = await _execution.GetPortfolioSnapshotAsync();
        var riskCheck = await _risk.ValidateTradeAsync(trade, thresholds, portfolio.PortfolioValue, 0.05m);

        if (!riskCheck.IsApproved && thresholds.RequireApproval)
        {
            // Log rejected trade to audit
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "trade_rejected",
                Entity = "Trade",
                Details = System.Text.Json.JsonSerializer.Serialize(new { trade, riskCheck.Violations }),
                Reasoning = riskCheck.Reasoning
            });
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Trade rejected by risk management", violations = riskCheck.Violations });
        }

        // Execute trade
        var result = await _execution.ExecuteTradeAsync(trade, trade.Mode == "live");
        if (!result.Success)
            return StatusCode(500, new { message = result.Message });

        trade.Price = result.ExecutionPrice;
        trade.ExecutedAt = result.ExecutedAt;

        _db.Trades.Add(trade);

        // Log to audit
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "trade_executed",
            Entity = "Trade",
            EntityId = trade.Id,
            Details = System.Text.Json.JsonSerializer.Serialize(new { trade, riskCheck.Reasoning }),
            Reasoning = $"Risk approved. {riskCheck.Reasoning}"
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation("Trade placed: {Action} {Qty} {Ticker} @ {Price} [{Mode}]",
            trade.Action, trade.Quantity, trade.Ticker, trade.Price, trade.Mode);

        return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, trade);
    }
}

public class PlaceTradeRequest
{
    public int StrategyId { get; set; }
    public required string Ticker { get; set; }
    public required string Action { get; set; } // BUY, SELL, SHORT, COVER
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string? Mode { get; set; } = "paper";
    public string? Reason { get; set; }
}
