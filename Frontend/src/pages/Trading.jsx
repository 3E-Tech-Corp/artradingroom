import { useState, useEffect } from 'react'
import { getPortfolio, getPositions, getTrades, placeTrade, getStrategies } from '../api'

export default function Trading({ mode, setMode }) {
  const [portfolio, setPortfolio] = useState(null)
  const [positions, setPositions] = useState([])
  const [trades, setTrades] = useState([])
  const [strategies, setStrategies] = useState([])
  const [showTradeForm, setShowTradeForm] = useState(false)
  const [showLiveWarning, setShowLiveWarning] = useState(false)
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState({ ticker: '', action: 'BUY', quantity: '', price: '', reason: '', strategyId: '' })

  const load = () => {
    Promise.all([
      getPortfolio().catch(() => ({ portfolioValue: 100000, cashBalance: 100000, dailyPnL: 0, totalPnL: 0, dailyPnLPercent: 0, totalPnLPercent: 0 })),
      getPositions().catch(() => []),
      getTrades().catch(() => []),
      getStrategies().catch(() => [])
    ]).then(([p, pos, t, s]) => {
      setPortfolio(p)
      setPositions(pos)
      setTrades(t)
      setStrategies(s)
      setLoading(false)
    })
  }

  useEffect(load, [])

  const handleTrade = async (e) => {
    e.preventDefault()
    try {
      await placeTrade({
        ticker: form.ticker,
        action: form.action,
        quantity: Number(form.quantity),
        price: Number(form.price),
        reason: form.reason,
        strategyId: Number(form.strategyId),
        mode
      })
      setShowTradeForm(false)
      setForm({ ticker: '', action: 'BUY', quantity: '', price: '', reason: '', strategyId: '' })
      load()
    } catch (err) { alert('Trade rejected: ' + err.message) }
  }

  const toggleMode = () => {
    if (mode === 'paper') setShowLiveWarning(true)
    else setMode('paper')
  }

  if (loading) return <p className="loading">Loading trading dashboard...</p>

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2>Trading</h2>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <span style={{ fontSize: '0.8rem', color: '#8b949e' }}>Mode:</span>
          <button
            onClick={toggleMode}
            style={{
              background: mode === 'live' ? '#da3633' : '#1f6feb',
              color: 'white', border: 'none', padding: '0.4rem 1rem',
              borderRadius: '6px', cursor: 'pointer', fontFamily: 'inherit', fontSize: '0.8rem', fontWeight: 'bold'
            }}
          >
            {mode === 'paper' ? 'PAPER' : '⚡ LIVE'}
          </button>
          <button className="primary" onClick={() => setShowTradeForm(!showTradeForm)}>+ Place Trade</button>
        </div>
      </div>

      {showLiveWarning && (
        <div className="card" style={{ border: '1px solid #f85149' }}>
          <h3 style={{ color: '#f85149' }}>⚠ Switch to LIVE Trading</h3>
          <p style={{ fontSize: '0.85rem', margin: '0.5rem 0' }}>
            Real money will be at risk. All trades will route to your live broker account.
            Only proceed if a strategy has cleared 30+ days of paper trading and passed all risk checks.
          </p>
          <button className="danger" onClick={() => { setMode('live'); setShowLiveWarning(false) }}>Confirm: Go Live</button>
          <button className="nav-btn" onClick={() => setShowLiveWarning(false)}>Cancel</button>
        </div>
      )}

      {showTradeForm && (
        <div className="card">
          <h3>Place Trade ({mode.toUpperCase()})</h3>
          <form onSubmit={handleTrade}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
              <div className="form-row">
                <label>Strategy</label>
                <select value={form.strategyId} onChange={e => setForm({...form, strategyId: e.target.value})} required>
                  <option value="">Select strategy...</option>
                  {strategies.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
              <div className="form-row">
                <label>Action</label>
                <select value={form.action} onChange={e => setForm({...form, action: e.target.value})}>
                  <option>BUY</option><option>SELL</option><option>SHORT</option><option>COVER</option>
                </select>
              </div>
              <div className="form-row"><label>Ticker</label><input value={form.ticker} onChange={e => setForm({...form, ticker: e.target.value})} placeholder="e.g. AAPL" required /></div>
              <div className="form-row"><label>Quantity</label><input type="number" value={form.quantity} onChange={e => setForm({...form, quantity: e.target.value})} required /></div>
              <div className="form-row"><label>Price ($)</label><input type="number" step="0.01" value={form.price} onChange={e => setForm({...form, price: e.target.value})} required /></div>
              <div className="form-row"><label>Reason</label><input value={form.reason} onChange={e => setForm({...form, reason: e.target.value})} /></div>
            </div>
            <button className="primary" type="submit">Submit Trade</button>
          </form>
        </div>
      )}

      <div className="stats-row">
        <div className="stat"><label>Portfolio Value</label><div className="value">${portfolio?.portfolioValue?.toLocaleString()}</div></div>
        <div className="stat"><label>Cash</label><div className="value">${portfolio?.cashBalance?.toLocaleString()}</div></div>
        <div className="stat"><label>Daily P&L</label><div className={`value ${portfolio?.dailyPnL >= 0 ? 'positive' : 'negative'}`}>{portfolio?.dailyPnL >= 0 ? '+' : ''}${portfolio?.dailyPnL?.toFixed(2)}</div></div>
        <div className="stat"><label>Total P&L</label><div className={`value ${portfolio?.totalPnL >= 0 ? 'positive' : 'negative'}`}>{portfolio?.totalPnL >= 0 ? '+' : ''}${portfolio?.totalPnL?.toFixed(2)}</div></div>
      </div>

      <div className="card">
        <h3>Open Positions</h3>
        {positions.length === 0 ? <p className="empty">No open positions</p> : (
          <table>
            <thead><tr><th>Ticker</th><th>Qty</th><th>Avg Cost</th><th>Current</th><th>Unrealized P&L</th><th>%</th></tr></thead>
            <tbody>
              {positions.map((p, i) => (
                <tr key={i}>
                  <td><strong>{p.ticker}</strong></td>
                  <td>{p.quantity}</td>
                  <td>${p.averageCost?.toFixed(2)}</td>
                  <td>${p.currentPrice?.toFixed(2)}</td>
                  <td className={p.unrealizedPnL >= 0 ? 'positive' : 'negative'}>{p.unrealizedPnL >= 0 ? '+' : ''}${p.unrealizedPnL?.toFixed(2)}</td>
                  <td className={p.unrealizedPnLPercent >= 0 ? 'positive' : 'negative'}>{(p.unrealizedPnLPercent * 100)?.toFixed(2)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="card">
        <h3>Trade History</h3>
        {trades.length === 0 ? <p className="empty">No trades yet</p> : (
          <table>
            <thead><tr><th>Ticker</th><th>Action</th><th>Qty</th><th>Price</th><th>Mode</th><th>Time</th></tr></thead>
            <tbody>
              {trades.map((t, i) => (
                <tr key={i}>
                  <td><strong>{t.ticker}</strong></td>
                  <td style={{ color: t.action === 'BUY' ? '#3fb950' : '#f85149' }}>{t.action}</td>
                  <td>{t.quantity}</td>
                  <td>${t.price?.toFixed(2)}</td>
                  <td><span className={`badge ${t.mode}`}>{t.mode}</span></td>
                  <td>{new Date(t.executedAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
