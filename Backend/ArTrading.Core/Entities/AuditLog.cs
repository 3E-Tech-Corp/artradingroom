namespace ArTrading.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public required string Action { get; set; } // trade_executed, strategy_updated, trade_rejected, approval_given, etc.
    public required string Entity { get; set; } // Trade, Strategy, Agent, etc.
    public int? EntityId { get; set; }
    public string? Details { get; set; } // JSON with action details
    public string? Reasoning { get; set; } // Why the action was taken or rejected
    public string? ApprovedBy { get; set; } // User or agent who approved
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
