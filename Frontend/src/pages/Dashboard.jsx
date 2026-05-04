import { useState, useEffect } from 'react'
import { getPortfolio, getTrades, getAgents, getAuditLog } from '../api'
import StatusDot from '../components/StatusDot'
import SvgChart from '../components/SvgChart'

export default function Dashboard() {
  const [portfolio, setPortfolio] = useState(null)
  const [trades, setTrades] = useState([])
  const [agents, setAgents] = useState([])
  const [audit, setAudit] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    Promise.all([
      getPortfolio().catch(() => ({ portfolioValue: 100000, dailyPnL: 0, totalPnL: 0, cashBalance: 100000, dailyPnLPercent: 0, totalPnLPercent: 0 })),
      getTrades().catch(() => []),
      getAgents().catch(() => []),
      getAuditLog({ limit: 5 }).catch(() => [])
    ]).then(([p, t, a, au]) => {
      setPortfolio(p)
      setTrades(t.slice(0, 5))
      setAgents(a)
      setAudit(au.slice(0, 5))
      setLoading(false)
    }).catch(e => { setError(e.message); setLoading(false) })
  }, [])

  if (loading) return <p className="loading">Loading dashboard...</p>
  if (error) return <p className="error">{error}</p>

  const defaultAgents = agents.length > 0 ? agents : [
    { name: 'CEO', role: 'ceo', status: 'idle' },
    { name: 'Research', role: 'research', status: 'idle' },
    { name: 'Backtest', role: 'backtest', status: 'idle' },
    { name: 'Risk Mgmt', role: 'risk', status: 'idle' },
    { name: 'Execution', role: 'execution', status: 'idle' },
    { name: 'Cost Optimizer', role: 'cost_optimizer', status: 'idle' }
  ]

  return (
    <div>
      <h2 style={{ marginBottom: '1rem' }}>Dashboard</h2>

      <div className="stats-row">
        <div className="stat">
          <label>Portfolio Value</label>
          <div className="value">${portfolio?.portfolioValue?.toLocaleString()}</div>
        </div>
        <div className="stat">
          <label>Daily P&L</label>
          <div className={`value ${portfolio?.dailyPnL >= 0 ? 'positive' : 'negative'}`}>
            {portfolio?.dailyPnL >= 0 ? '+' : ''}${portfolio?.dailyPnL?.toFixed(2)} ({(portfolio?.dailyPnLPercent * 100)?.toFixed(2)}%)
          </div>
        </div>
        <div className="stat">
          <label>Total P&L</label>
          <div className={`value ${portfolio?.totalPnL >= 0 ? 'positive' : 'negative'}`}>
            {portfolio?.totalPnL >= 0 ? '+' : ''}${portfolio?.totalPnL?.toFixed(2)} ({(portfolio?.totalPnLPercent * 100)?.toFixed(2)}%)
          </div>
        </div>
        <div className="stat">
          <label>Cash</label>
          <div className="value">${portfolio?.cashBalance?.toLocaleString()}</div>
        </div>
      </div>

      <div className="card">
        <h3>Equity Curve</h3>
        <SvgChart data={[100000, 100500, 101200, 100800, 102000, 103000, 102500, 104000, 105000]} />
      </div>

      <div className="card">
        <h3>Agent Status</h3>
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          {defaultAgents.map(a => (
            <div key={a.name} style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <StatusDot status={a.status} />
              <span style={{ fontSize: '0.8rem' }}>{a.name}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="card">
        <h3>Recent Trades</h3>
        {trades.length === 0 ? <p className="empty">No trades yet</p> : (
          <table>
            <thead><tr><th>Ticker</th><th>Action</th><th>Qty</th><th>Price</th><th>Mode</th><th>Time</th></tr></thead>
            <tbody>
              {trades.map((t, i) => (
                <tr key={i}>
                  <td>{t.ticker}</td>
                  <td>{t.action}</td>
                  <td>{t.quantity}</td>
                  <td>${t.price?.toFixed(2)}</td>
                  <td><span className={`badge ${t.mode}`}>{t.mode}</span></td>
                  <td>{new Date(t.executedAt).toLocaleTimeString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="card">
        <h3>Audit Log</h3>
        {audit.length === 0 ? <p className="empty">No audit entries</p> : (
          <table>
            <thead><tr><th>Action</th><th>Entity</th><th>Time</th></tr></thead>
            <tbody>
              {audit.map((a, i) => (
                <tr key={i}>
                  <td>{a.action}</td>
                  <td>{a.entity}</td>
                  <td>{new Date(a.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
