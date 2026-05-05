using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("[controller]")]
public class ResearchController : ControllerBase
{
    private readonly IResearchAgentService _research;
    private readonly ArTradingDbContext _db;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(IResearchAgentService research, ArTradingDbContext db, ILogger<ResearchController> logger)
    {
        _research = research;
        _db = db;
        _logger = logger;
    }

    [HttpPost("brief")]
    public async Task<IActionResult> GenerateBrief()
    {
        _logger.LogInformation("Generating weekly research brief");
        var brief = await _research.GenerateWeeklyBriefAsync();
        return Ok(brief);
    }

    [HttpPost("scan")]
    public async Task<IActionResult> ScanMarket([FromBody] ScanRequest? request)
    {
        var insights = await _research.ScanMarketDataAsync(request?.Symbol);
        return Ok(insights);
    }

    [HttpPost("idea")]
    public async Task<IActionResult> GenerateIdea([FromBody] IdeaRequest request)
    {
        var idea = await _research.GenerateStrategyIdeaAsync(request.Sector ?? "tech");
        return Ok(idea);
    }
}

public class ScanRequest
{
    public string? Symbol { get; set; }
}

public class IdeaRequest
{
    public string? Sector { get; set; }
}
