using ArTrading.Core.Models;
using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StrategiesController : ControllerBase
{
    private readonly IStrategyService _strategyService;
    private readonly IBacktestService _backtestService;
    private readonly ArTradingDbContext _db;
    private readonly ILogger<StrategiesController> _logger;

    public StrategiesController(
        IStrategyService strategyService,
        IBacktestService backtestService,
        ArTradingDbContext db,
        ILogger<StrategiesController> logger)
    {
        _strategyService = strategyService;
        _backtestService = backtestService;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListStrategies([FromQuery] string? status = null)
    {
        var query = _db.Strategies.AsQueryable();
        if (status != null)
            query = query.Where(s => s.Status == status);

        var strategies = await query.OrderByDescending(s => s.UpdatedAt).ToListAsync();
        return Ok(strategies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStrategy(int id)
    {
        var strategy = await _db.Strategies.FindAsync(id);
        if (strategy == null)
            return NotFound();

        return Ok(strategy);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStrategy([FromBody] CreateStrategyRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || request.Rule == null)
            return BadRequest("Name and Rule are required");

        var strategy = await _strategyService.CreateStrategyAsync(request.Name, request.Description, request.Rule);
        _db.Strategies.Add(strategy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Strategy created: {StrategyId}", strategy.Id);
        return CreatedAtAction(nameof(GetStrategy), new { id = strategy.Id }, strategy);
    }

    [HttpPost("{id}/backtest")]
    public async Task<IActionResult> RunBacktest(int id, [FromBody] BacktestRequest request)
    {
        var strategy = await _db.Strategies.FindAsync(id);
        if (strategy == null)
            return NotFound();

        var backtest = await _backtestService.RunBacktestAsync(
            strategy, request.StartDate, request.EndDate, request.StartingCapital);

        _db.BacktestRuns.Add(backtest);
        strategy.Status = "backtested";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Backtest completed for strategy {StrategyId}", id);
        return Ok(backtest);
    }

    [HttpGet("{id}/backtests")]
    public async Task<IActionResult> GetBacktestRuns(int id)
    {
        var backtests = await _db.BacktestRuns
            .Where(b => b.StrategyId == id)
            .OrderByDescending(b => b.RunAt)
            .ToListAsync();

        return Ok(backtests);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStrategy(int id, [FromBody] UpdateStrategyRequest request)
    {
        var strategy = await _db.Strategies.FindAsync(id);
        if (strategy == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.Name))
            strategy.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            strategy.Description = request.Description;

        if (request.Rule != null)
            strategy.Rules = _strategyService.SerializeRules(request.Rule);

        if (!string.IsNullOrEmpty(request.Status))
            strategy.Status = request.Status;

        strategy.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(strategy);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStrategy(int id)
    {
        var strategy = await _db.Strategies.FindAsync(id);
        if (strategy == null)
            return NotFound();

        _db.Strategies.Remove(strategy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Strategy deleted: {StrategyId}", id);
        return NoContent();
    }
}

public class CreateStrategyRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required StrategyRule Rule { get; set; }
}

public class UpdateStrategyRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public StrategyRule? Rule { get; set; }
    public string? Status { get; set; }
}

public class BacktestRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal StartingCapital { get; set; } = 100000m;
}
