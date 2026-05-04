using System.Text.Json;
using ArTrading.Core.Entities;
using ArTrading.Core.Models;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public interface IStrategyService
{
    Task<Strategy> CreateStrategyAsync(string name, string description, StrategyRule rule);
    Task<Strategy?> GetStrategyAsync(int id);
    Task<List<Strategy>> ListStrategiesAsync(string? status = null);
    StrategyRule ParseRules(string rulesJson);
    string SerializeRules(StrategyRule rule);
}

public class StrategyService : IStrategyService
{
    private readonly ILogger<StrategyService> _logger;

    public StrategyService(ILogger<StrategyService> logger)
    {
        _logger = logger;
    }

    public Task<Strategy> CreateStrategyAsync(string name, string description, StrategyRule rule)
    {
        var strategy = new Strategy
        {
            Name = name,
            Description = description,
            Rules = SerializeRules(rule),
            Status = "draft"
        };

        _logger.LogInformation("Created strategy: {StrategyName}", name);
        return Task.FromResult(strategy);
    }

    public Task<Strategy?> GetStrategyAsync(int id)
    {
        _logger.LogInformation("Fetching strategy: {StrategyId}", id);
        return Task.FromResult<Strategy?>(null);
    }

    public Task<List<Strategy>> ListStrategiesAsync(string? status = null)
    {
        _logger.LogInformation("Listing strategies with status: {Status}", status ?? "all");
        return Task.FromResult(new List<Strategy>());
    }

    public StrategyRule ParseRules(string rulesJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rule = JsonSerializer.Deserialize<StrategyRule>(rulesJson, options);
            return rule ?? new StrategyRule();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse strategy rules");
            throw;
        }
    }

    public string SerializeRules(StrategyRule rule)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(rule, options);
    }
}
