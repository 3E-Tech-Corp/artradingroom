using ArTrading.Core.Entities;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RiskController : ControllerBase
{
    private readonly ArTradingDbContext _db;
    private readonly ILogger<RiskController> _logger;

    public RiskController(ArTradingDbContext db, ILogger<RiskController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("thresholds")]
    public async Task<IActionResult> GetThresholds()
    {
        var thresholds = await _db.RiskThresholds.OrderByDescending(r => r.Id).FirstOrDefaultAsync();
        if (thresholds == null)
        {
            thresholds = new RiskThreshold();
            _db.RiskThresholds.Add(thresholds);
            await _db.SaveChangesAsync();
        }
        return Ok(thresholds);
    }

    [HttpPut("thresholds/{id}")]
    public async Task<IActionResult> UpdateThresholds(int id, [FromBody] UpdateThresholdsRequest request)
    {
        var thresholds = await _db.RiskThresholds.FindAsync(id);
        if (thresholds == null) return NotFound();

        thresholds.MaxDrawdown = request.MaxDrawdown;
        thresholds.MaxPositionSize = request.MaxPositionSize;
        thresholds.DailyLossLimit = request.DailyLossLimit;
        thresholds.MaxLeverage = request.MaxLeverage;
        thresholds.RequireApproval = request.RequireApproval;
        thresholds.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "risk_thresholds_updated",
            Entity = "RiskThreshold",
            EntityId = id,
            Details = System.Text.Json.JsonSerializer.Serialize(request),
            Reasoning = "Manual update via Risk Console"
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Risk thresholds updated: MaxDD={MaxDD}, MaxPos={MaxPos}",
            thresholds.MaxDrawdown, thresholds.MaxPositionSize);

        return Ok(thresholds);
    }

    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        // Trades flagged but not yet executed in live mode pending manual review
        var pendingTrades = await _db.Trades
            .Where(t => t.Mode == "pending_approval")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return Ok(pendingTrades);
    }

    [HttpPost("approve/{tradeId}")]
    public async Task<IActionResult> ApproveTrade(int tradeId, [FromQuery] string approvedBy = "manual")
    {
        var trade = await _db.Trades.FindAsync(tradeId);
        if (trade == null) return NotFound();

        trade.Mode = "live";

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "trade_approved",
            Entity = "Trade",
            EntityId = tradeId,
            ApprovedBy = approvedBy,
            Reasoning = "Manual approval via Risk Console"
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Trade {TradeId} approved by {ApprovedBy}", tradeId, approvedBy);
        return Ok(trade);
    }

    [HttpPost("reject/{tradeId}")]
    public async Task<IActionResult> RejectTrade(int tradeId, [FromBody] RejectTradeRequest request)
    {
        var trade = await _db.Trades.FindAsync(tradeId);
        if (trade == null) return NotFound();

        _db.Trades.Remove(trade);

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "trade_rejected",
            Entity = "Trade",
            EntityId = tradeId,
            Reasoning = request.Reason ?? "Rejected via Risk Console"
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Trade {TradeId} rejected", tradeId);
        return NoContent();
    }
}

public class UpdateThresholdsRequest
{
    public decimal MaxDrawdown { get; set; }
    public decimal MaxPositionSize { get; set; }
    public decimal DailyLossLimit { get; set; }
    public decimal MaxLeverage { get; set; }
    public bool RequireApproval { get; set; }
}

public class RejectTradeRequest
{
    public string? Reason { get; set; }
}
