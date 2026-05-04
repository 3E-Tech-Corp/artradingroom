# ArTrading — AI-Powered Autonomous Trading Firm

A full-stack platform for building and managing autonomous AI trading agents. Built on .NET 8 + React + SQL Server.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                React Frontend                        │
│     (Dashboard, Strategy Lab, Risk Console)         │
└────────────────────┬────────────────────────────────┘
                     │ (HTTP/WebSocket)
                     ↓
┌─────────────────────────────────────────────────────┐
│              .NET 8 Core API                         │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐│
│  │   Agent      │  │  Strategy    │  │    Risk    ││
│  │ Orchestration│  │   Engine     │  │ Management ││
│  └──────────────┘  └──────────────┘  └────────────┘│
│  ┌──────────────┐  ┌──────────────┐  ┌────────────┐│
│  │  Backtest    │  │  Execution   │  │   Audit    ││
│  │   Runner     │  │    Service   │  │    Log     ││
│  └──────────────┘  └──────────────┘  └────────────┘│
└────────────────────┬────────────────────────────────┘
                     │ (EF Core)
                     ↓
┌─────────────────────────────────────────────────────┐
│           SQL Server Database                        │
│  Agents | Strategies | Trades | BacktestRuns       │
│  RiskThresholds | AgentTasks | AuditLog            │
└─────────────────────────────────────────────────────┘
```

## Sprint 1: Core Infrastructure ✅

- **DbContext & Entities**: Agent, Strategy, Trade, BacktestRun, RiskThreshold, AgentTask, AuditLog
- **EF Core 9.0**: SQL Server provider with migrations
- **API Foundation**: Health check, CORS, dependency injection
- **React Setup**: Vite + React 19, build system ready

## Entities

### Agent
- `id`, `name`, `role`, `mandate`, `status`, `lastHeartbeat`
- Roles: CEO, Research, Backtest, RiskManagement, Execution, CostOptimizer

### Strategy
- `id`, `name`, `description`, `rules` (JSON), `status`
- Status: draft → backtested → paper_trading → live → archived

### Trade
- `id`, `strategyId`, `ticker`, `action`, `quantity`, `price`, `mode` (paper/live)
- `reason` - why the trade was placed
- `executedAt` - timestamp

### BacktestRun
- `id`, `strategyId`, `startDate`, `endDate`
- `finalValue`, `totalReturn`, `sharpeRatio`, `maxDrawdown`
- `totalTrades`, `winningTrades`, `results` (JSON)

### RiskThreshold
- `maxDrawdown` (default 20%)
- `maxPositionSize` (default 10%)
- `dailyLossLimit` (default 5%)
- `maxLeverage` (default 1.0 — no margin)
- `requireApproval` (bool)

### AgentTask
- `id`, `agentId`, `taskType`, `payload` (JSON), `status`
- Status: pending → running → completed/failed

### AuditLog
- `id`, `action`, `entity`, `entityId`, `details`, `reasoning`
- Every trade, approval, rejection logged

## Development Setup

### Backend
```bash
cd Backend
# Build
dotnet build

# Create migration (after schema changes)
export PATH="$PATH:/home/feng/.dotnet/tools"
dotnet ef migrations add MigrationName \
  --project ArTrading.Data \
  --startup-project ArTrading.API

# Run migrations (on app startup)
# Handled automatically in Program.cs

# Run API
dotnet run --project ArTrading.API
```

### Frontend
```bash
cd Frontend
npm install
npm run dev  # http://localhost:5173
```

### Database
Connection string in `Backend/ArTrading.API/appsettings.json`:
```json
"DefaultConnection": "Server=(local);Database=ArTrading;Trusted_Connection=true;Encrypt=false;"
```

## Next Steps (Sprint 2-6)

### Sprint 2: Strategy & Backtest
- [ ] Strategy rule DSL or JSON schema
- [ ] Backtest runner (historical data loop)
- [ ] Equity curve charting
- [ ] React components: Strategy upload, backtest results

### Sprint 3: Risk Management
- [ ] RiskManagement service (threshold enforcement)
- [ ] Approval workflow (Risk Agent → CEO → Execute)
- [ ] Position sizing calculator
- [ ] React: Risk console, pending approvals

### Sprint 4: Trading & Execution
- [ ] Broker API integration (start with paper)
- [ ] ExecutionAgent service
- [ ] P&L tracking, position monitoring
- [ ] React: Live dashboard, open positions, trade journal

### Sprint 5: Research Automation
- [ ] ResearchAgent background job
- [ ] Market data sources (alpha, vol surfaces, sentiment)
- [ ] Weekly briefing generator
- [ ] React: Research feed

### Sprint 6: Dashboard & Polish
- [ ] Real-time updates (SignalR)
- [ ] Agent status, org chart
- [ ] Export institutional memory
- [ ] Deployment & hardening

## Key Decisions

### Strategy Format
Currently flexible (JSON in `rules` field). Can add DSL parser if needed.

### Broker Integration
Not yet implemented. Need to decide:
- Interactive Brokers, Alpaca, TD Ameritrade?
- Paper trading first, then live with approval gate

### Agent Communication
Currently task-based (AgentTask queue). Can upgrade to:
- Message broker (RabbitMQ) for real-time
- Claude API calls for agent reasoning

### Decimal Precision
All financial fields (Price, Quantity, Return, etc.) use `decimal` for accuracy. Add `.HasPrecision(18, 8)` in migrations if needed.

## Deployment

### Local IIS
After development:
```bash
# Build
dotnet publish -c Release -o "F:\New_WWW\artradingroom\API"

# Frontend
npm run build
# Copy dist/ to F:\New_WWW\artradingroom\WWW
```

### CI/CD
GitHub Actions workflow ready (add to `.github/workflows/`):
- Runs `dotnet build` + tests
- Publishes to FTPB1 IIS
- Deploys React bundle

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make changes, build locally, test
3. Commit: `git commit -m "feat: description"`
4. Push & create PR

## Resources

- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [React Vite Docs](https://vitejs.dev/guide/)
- [SQL Server T-SQL Reference](https://learn.microsoft.com/en-us/sql/t-sql/language-reference)

---

**Status**: Sprint 1 Complete ✅ — Ready for Sprint 2
