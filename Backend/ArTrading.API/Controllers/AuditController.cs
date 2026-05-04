using ArTrading.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly ArTradingDbContext _db;

    public AuditController(ArTradingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string? entity = null,
        [FromQuery] string? action = null,
        [FromQuery] int limit = 100)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (entity != null) query = query.Where(a => a.Entity == entity);
        if (action != null) query = query.Where(a => a.Action == action);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditEntry(int id)
    {
        var entry = await _db.AuditLogs.FindAsync(id);
        if (entry == null) return NotFound();
        return Ok(entry);
    }
}
