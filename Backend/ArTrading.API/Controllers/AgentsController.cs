using ArTrading.Core.Entities;
using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentsController : ControllerBase
{
    private readonly ArTradingDbContext _db;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(ArTradingDbContext db, ILogger<AgentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListAgents()
    {
        var agents = await _db.Agents.OrderBy(a => a.Id).ToListAsync();
        return Ok(agents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAgent(int id)
    {
        var agent = await _db.Agents.Include(a => a.Tasks).FirstOrDefaultAsync(a => a.Id == id);
        if (agent == null) return NotFound();
        return Ok(agent);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        var agent = new Agent
        {
            Name = request.Name,
            Role = request.Role,
            Mandate = request.Mandate,
            Status = "idle"
        };
        _db.Agents.Add(agent);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Agent created: {AgentName} ({AgentRole})", agent.Name, agent.Role);
        return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, agent);
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> Heartbeat(int id)
    {
        var agent = await _db.Agents.FindAsync(id);
        if (agent == null) return NotFound();
        agent.LastHeartbeat = DateTime.UtcNow;
        agent.Status = "idle";
        await _db.SaveChangesAsync();
        return Ok(new { agentId = id, timestamp = agent.LastHeartbeat });
    }

    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> GetTasks(int id, [FromQuery] string? status = null)
    {
        var query = _db.AgentTasks.Where(t => t.AgentId == id);
        if (status != null) query = query.Where(t => t.Status == status);
        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(tasks);
    }

    [HttpPost("{id}/tasks")]
    public async Task<IActionResult> CreateTask(int id, [FromBody] CreateTaskRequest request)
    {
        var agent = await _db.Agents.FindAsync(id);
        if (agent == null) return NotFound();

        var task = new AgentTask
        {
            AgentId = id,
            TaskType = request.TaskType,
            Payload = request.Payload,
            Status = "pending"
        };
        _db.AgentTasks.Add(task);
        agent.Status = "working";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Task created for agent {AgentId}: {TaskType}", id, request.TaskType);
        return Ok(task);
    }

    [HttpPut("{agentId}/tasks/{taskId}")]
    public async Task<IActionResult> UpdateTask(int agentId, int taskId, [FromBody] UpdateTaskRequest request)
    {
        var task = await _db.AgentTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.AgentId == agentId);
        if (task == null) return NotFound();

        task.Status = request.Status;
        task.Result = request.Result;
        task.Error = request.Error;
        if (request.Status is "completed" or "failed")
            task.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedAgents()
    {
        if (await _db.Agents.AnyAsync())
            return BadRequest("Agents already seeded");

        var agents = new[]
        {
            new Agent { Name = "CEO", Role = "ceo", Mandate = "Manage the trading firm, delegate tasks, run weekly board briefings, and serve as the primary interface for strategy decisions.", Status = "idle" },
            new Agent { Name = "Research Agent", Role = "research", Mandate = "Scan market data, sentiment, and financial news every night. Generate weekly strategy briefs for the CEO.", Status = "idle" },
            new Agent { Name = "Backtest Agent", Role = "backtest", Mandate = "Test every strategy idea, log every result permanently. Institutional memory of all backtest outcomes.", Status = "idle" },
            new Agent { Name = "Risk Management Agent", Role = "risk", Mandate = "Gate all strategies before live trading. No strategy executes without risk sign-off. Enforce hard thresholds.", Status = "idle" },
            new Agent { Name = "Execution Agent", Role = "execution", Mandate = "Place trades when Risk Agent clears a strategy. Paper trading by default. Flips to live on CEO instruction.", Status = "idle" },
            new Agent { Name = "Cost Optimizer", Role = "cost_optimizer", Mandate = "Monitor token and API usage weekly. Identify and eliminate inefficiencies across the firm.", Status = "idle" }
        };

        _db.Agents.AddRange(agents);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded 6 agents");
        return Ok(new { message = "6 agents seeded", agents });
    }
}

public class CreateAgentRequest
{
    public required string Name { get; set; }
    public required string Role { get; set; }
    public string? Mandate { get; set; }
}

public class CreateTaskRequest
{
    public required string TaskType { get; set; }
    public string? Payload { get; set; }
}

public class UpdateTaskRequest
{
    public required string Status { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
