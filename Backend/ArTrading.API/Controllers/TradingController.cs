using ArTrading.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("[controller]")]
public class TradingController : ControllerBase
{
    private readonly IExecutionService _execution;
    private readonly ILogger<TradingController> _logger;

    public TradingController(IExecutionService execution, ILogger<TradingController> logger)
    {
        _execution = execution;
        _logger = logger;
    }

    [HttpGet("portfolio")]
    public async Task<IActionResult> GetPortfolio()
    {
        var snapshot = await _execution.GetPortfolioSnapshotAsync();
        return Ok(snapshot);
    }

    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions()
    {
        var positions = await _execution.GetOpenPositionsAsync();
        return Ok(positions);
    }
}
