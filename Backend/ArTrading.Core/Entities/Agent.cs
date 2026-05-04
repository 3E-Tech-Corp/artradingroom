namespace ArTrading.Core.Entities;

public class Agent
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public string? Mandate { get; set; }
    public string Status { get; set; } = "idle"; // idle, working, error
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<AgentTask> Tasks { get; set; } = new List<AgentTask>();
}
