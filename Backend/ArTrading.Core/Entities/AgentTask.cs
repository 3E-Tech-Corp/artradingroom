namespace ArTrading.Core.Entities;

public class AgentTask
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public required string TaskType { get; set; } // research, backtest, validate, execute, etc.
    public string? Payload { get; set; } // JSON with task parameters
    public string Status { get; set; } = "pending"; // pending, running, completed, failed
    public string? Result { get; set; } // JSON with task result
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public virtual Agent Agent { get; set; } = null!;
}
