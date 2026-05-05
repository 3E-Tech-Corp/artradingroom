using ArTrading.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArTrading.Data;

public class ArTradingDbContext : DbContext
{
    public ArTradingDbContext(DbContextOptions<ArTradingDbContext> options) : base(options)
    {
    }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<Strategy> Strategies { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<BacktestRun> BacktestRuns { get; set; }
    public DbSet<RiskThreshold> RiskThresholds { get; set; }
    public DbSet<AgentTask> AgentTasks { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<MarketDataBar> MarketDataBars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agent relationships
        modelBuilder.Entity<Agent>()
            .HasMany(a => a.Tasks)
            .WithOne(t => t.Agent)
            .HasForeignKey(t => t.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Strategy relationships
        modelBuilder.Entity<Strategy>()
            .HasMany(s => s.BacktestRuns)
            .WithOne(b => b.Strategy)
            .HasForeignKey(b => b.StrategyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Strategy>()
            .HasMany(s => s.Trades)
            .WithOne(t => t.Strategy)
            .HasForeignKey(t => t.StrategyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        modelBuilder.Entity<Agent>().HasIndex(a => a.Status);
        modelBuilder.Entity<Strategy>().HasIndex(s => s.Status);
        modelBuilder.Entity<Trade>().HasIndex(t => t.StrategyId);
        modelBuilder.Entity<Trade>().HasIndex(t => t.ExecutedAt);
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Entity);
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Action);

        // MarketDataBars: unique on (Ticker, Date) to support upserts
        modelBuilder.Entity<MarketDataBar>()
            .HasIndex(m => new { m.Ticker, m.Date })
            .IsUnique();
        modelBuilder.Entity<MarketDataBar>().HasIndex(m => m.Ticker);
    }
}
