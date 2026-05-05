using ArTrading.API.Workers;
using ArTrading.Core.Services;
using ArTrading.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Services.AddControllers();

// Configure SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(local);Database=ArTrading;Trusted_Connection=true;Encrypt=false;";
builder.Services.AddDbContext<ArTradingDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Register application services (Singleton for stateful execution service)
builder.Services.AddHttpClient<IMarketDataService, YahooFinanceService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddScoped<IRiskManagementService, RiskManagementService>();
builder.Services.AddSingleton<IExecutionService, ExecutionService>();
builder.Services.AddScoped<IResearchAgentService, ResearchAgentService>();

// Background jobs
builder.Services.AddHostedService<ResearchWorker>();

// Enable CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-apply EF migrations on startup (runs as app pool identity which has DB access)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArTradingDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReact");
app.UseHttpsRedirection();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi();

app.MapGet("/", () => Results.Ok(new { message = "ArTrading API v1.0", timestamp = DateTime.UtcNow }))
    .WithName("Root")
    .WithOpenApi();

app.Run();
