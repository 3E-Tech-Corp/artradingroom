import { useState, useEffect } from 'react'
import { getStrategies, createStrategy, deleteStrategy, runBacktest, getBacktests } from '../api'
import SvgChart from '../components/SvgChart'

export default function Strategies() {
  const [strategies, setStrategies] = useState([])
  const [selected, setSelected] = useState(null)
  const [backtests, setBacktests] = useState([])
  const [showForm, setShowForm] = useState(false)
  const [showBacktest, setShowBacktest] = useState(false)
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState({ name: '', description: '', rules: '{\n  "entryConditions": [{"indicator": "RSI", "operator": "<", "value": 30}],\n  "exitConditions": [{"indicator": "RSI", "operator": ">", "value": 70}],\n  "positionSizing": {"percentageOfPortfolio": 0.05}\n}' })
  const [btForm, setBtForm] = useState({ ticker: 'SPY', startDate: '2024-01-01', endDate: '2024-12-31', startingCapital: 100000 })
  const [showOhlcv, setShowOhlcv] = useState(false)

  const load = () => {
    getStrategies().then(s => { setStrategies(s); setLoading(false) }).catch(() => setLoading(false))
  }

  useEffect(load, [])

  const handleCreate = async (e) => {
    e.preventDefault()
    try {
      const rule = JSON.parse(form.rules)
      await createStrategy({ name: form.name, description: form.description, rule })
      setShowForm(false)
      setForm({ name: '', description: '', rules: form.rules })
      load()
    } catch (err) { alert('Error: ' + err.message) }
  }

  const handleDelete = async (id) => {
    if (!confirm('Delete this strategy?')) return
    await deleteStrategy(id)
    setSelected(null)
    load()
  }

  const handleBacktest = async (e) => {
    e.preventDefault()
    try {
      await runBacktest(selected.id, { ticker: btForm.ticker, startDate: btForm.startDate, endDate: btForm.endDate, startingCapital: Number(btForm.startingCapital) })
      const bts = await getBacktests(selected.id)
      setBacktests(bts)
      setShowBacktest(false)
      load()
    } catch (err) { alert('Error: ' + err.message) }
  }

  const selectStrategy = async (s) => {
    setSelected(s)
    const bts = await getBacktests(s.id).catch(() => [])
    setBacktests(bts)
  }

  if (loading) return <p className="loading">Loading strategies...</p>

  if (selected) {
    const latest = backtests[0]
    const latestResults = latest?.results ? (() => { try { return JSON.parse(latest.results) } catch { return null } })() : null
    const equityCurveData = latestResults?.EquityCurve?.map(p => p.PortfolioValue) || []
    const trades = latestResults?.Trades || []
    const priceData = latestResults?.PriceData || []
    const closePrices = priceData.map(b => b.Close)
    const tradeMarkers = trades.filter(t => t.BarIndex != null && t.BarIndex >= 0).map(t => ({ index: t.BarIndex, type: t.Action }))

    return (
      <div>
        <button className="nav-btn" onClick={() => setSelected(null)}>&larr; Back</button>
        <h2 style={{ margin: '0.5rem 0' }}>{selected.name}</h2>
        <span className={`badge ${selected.status}`}>{selected.status}</span>
        <p style={{ margin: '0.5rem 0', fontSize: '0.85rem', color: '#8b949e' }}>{selected.description}</p>

        <div style={{ margin: '1rem 0' }}>
          <button className="primary" onClick={() => setShowBacktest(true)}>Run Backtest</button>
          <button className="danger" style={{ marginLeft: '0.5rem' }} onClick={() => handleDelete(selected.id)}>Delete</button>
        </div>

        {showBacktest && (
          <div className="card">
            <h3>Run Backtest</h3>
            <form onSubmit={handleBacktest}>
              <div className="form-row"><label>Symbol / Ticker</label><input value={btForm.ticker} onChange={e => setBtForm({...btForm, ticker: e.target.value.toUpperCase()})} placeholder="e.g. SPY, AAPL, MSFT" required /></div>
              <div className="form-row"><label>Start Date</label><input type="date" value={btForm.startDate} onChange={e => setBtForm({...btForm, startDate: e.target.value})} /></div>
              <div className="form-row"><label>End Date</label><input type="date" value={btForm.endDate} onChange={e => setBtForm({...btForm, endDate: e.target.value})} /></div>
              <div className="form-row"><label>Starting Capital ($)</label><input type="number" value={btForm.startingCapital} onChange={e => setBtForm({...btForm, startingCapital: e.target.value})} /></div>
              <button className="primary" type="submit">Run</button>
            </form>
          </div>
        )}

        {latest && (
          <>
            <div className="card">
              <h3>Backtest Parameters</h3>
              <div className="stats-row">
                <div className="stat"><label>Symbol</label><div className="value" style={{ color: '#58a6ff' }}>{latest.ticker || 'N/A'}</div></div>
                <div className="stat"><label>Date Range</label><div className="value" style={{ fontSize: '0.95rem' }}>{new Date(latest.startDate).toLocaleDateString()} — {new Date(latest.endDate).toLocaleDateString()}</div></div>
                <div className="stat"><label>Starting Capital</label><div className="value">${Number(latest.startingCapital).toLocaleString()}</div></div>
                <div className="stat"><label>Final Value</label><div className="value">${Number(latest.finalValue).toLocaleString()}</div></div>
                <div className="stat"><label>Run Date</label><div className="value" style={{ fontSize: '0.85rem' }}>{new Date(latest.runAt).toLocaleString()}</div></div>
              </div>
            </div>

            <div className="card">
              <h3>Performance Summary</h3>
              <div className="stats-row">
                <div className="stat"><label>Total Return</label><div className="value positive">{(latest.totalReturn * 100).toFixed(1)}%</div></div>
                <div className="stat"><label>Sharpe Ratio</label><div className="value">{latest.sharpeRatio?.toFixed(2)}</div></div>
                <div className="stat"><label>Max Drawdown</label><div className="value negative">{(latest.maxDrawdown * 100).toFixed(1)}%</div></div>
                <div className="stat"><label>Win Rate</label><div className="value">{latest.totalTrades > 0 ? ((latest.winningTrades / latest.totalTrades) * 100).toFixed(0) : 0}%</div></div>
                <div className="stat"><label>Total Trades</label><div className="value">{latest.totalTrades}</div></div>
              </div>
              <SvgChart data={equityCurveData} width={600} height={160} label="equity" />
            </div>

            {closePrices.length > 0 && (
              <div className="card">
                <h3>Price Chart — {latest.ticker || 'N/A'} (with trade signals)</h3>
                <SvgChart data={closePrices} markers={tradeMarkers} width={700} height={200} color="#58a6ff" label="price" />
                <div style={{ display: 'flex', gap: '1rem', marginTop: '0.5rem', fontSize: '0.75rem', color: '#8b949e' }}>
                  <span><span style={{ color: '#3fb950' }}>&#9650;</span> BUY</span>
                  <span><span style={{ color: '#f85149' }}>&#9660;</span> SELL</span>
                  <span>{closePrices.length} trading days</span>
                </div>
              </div>
            )}

            {priceData.length > 0 && (
              <div className="card">
                <h3 style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span>OHLCV Data</span>
                  <button className="nav-btn" style={{ fontSize: '0.75rem' }} onClick={() => setShowOhlcv(!showOhlcv)}>
                    {showOhlcv ? 'Hide Table' : `Show Table (${priceData.length} bars)`}
                  </button>
                </h3>
                {showOhlcv && (
                  <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                    <table>
                      <thead><tr><th>Date</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>Volume</th></tr></thead>
                      <tbody>
                        {priceData.map((b, i) => (
                          <tr key={i} style={trades.some(t => t.BarIndex === i) ? { background: '#1f6feb22' } : {}}>
                            <td>{new Date(b.Date).toLocaleDateString()}</td>
                            <td>${b.Open?.toFixed(2)}</td>
                            <td>${b.High?.toFixed(2)}</td>
                            <td>${b.Low?.toFixed(2)}</td>
                            <td style={{ fontWeight: 'bold' }}>${b.Close?.toFixed(2)}</td>
                            <td>{(b.Volume / 1000000).toFixed(1)}M</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {trades.length > 0 && (
              <div className="card">
                <h3>Trade History ({trades.length} trades)</h3>
                <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                  <table>
                    <thead><tr><th>Date</th><th>Ticker</th><th>Action</th><th>Qty</th><th>Price</th><th>P&L</th><th>Signal</th></tr></thead>
                    <tbody>
                      {trades.map((t, i) => (
                        <tr key={i}>
                          <td>{new Date(t.Date).toLocaleDateString()}</td>
                          <td><strong>{t.Ticker}</strong></td>
                          <td style={{ color: t.Action === 'BUY' ? '#3fb950' : '#f85149' }}>{t.Action}</td>
                          <td>{t.Quantity}</td>
                          <td>${t.Price?.toFixed(2)}</td>
                          <td className={t.PnL >= 0 ? 'positive' : 'negative'}>{t.PnL >= 0 ? '+' : ''}${t.PnL?.toFixed(2)}</td>
                          <td style={{ color: '#8b949e', fontSize: '0.75rem' }}>{t.Signal}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {trades.length === 0 && (
              <div className="card">
                <h3>Trade History</h3>
                <p className="empty">No trade log available for this backtest. Re-run the backtest to generate trade history.</p>
              </div>
            )}
          </>
        )}

        {backtests.length === 0 && <p className="empty">No backtests run yet</p>}
      </div>
    )
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2>Strategy Lab</h2>
        <button className="primary" onClick={() => setShowForm(!showForm)}>+ New Strategy</button>
      </div>

      {showForm && (
        <div className="card">
          <h3>Create Strategy</h3>
          <form onSubmit={handleCreate}>
            <div className="form-row"><label>Name</label><input value={form.name} onChange={e => setForm({...form, name: e.target.value})} required /></div>
            <div className="form-row"><label>Description</label><input value={form.description} onChange={e => setForm({...form, description: e.target.value})} /></div>
            <div className="form-row"><label>Rules (JSON)</label><textarea rows={8} value={form.rules} onChange={e => setForm({...form, rules: e.target.value})} /></div>
            <button className="primary" type="submit">Create</button>
          </form>
        </div>
      )}

      {strategies.length === 0 ? <p className="empty">No strategies yet. Create one to get started.</p> : (
        <div className="grid">
          {strategies.map(s => (
            <div key={s.id} className="card" style={{ cursor: 'pointer' }} onClick={() => selectStrategy(s)}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <strong>{s.name}</strong>
                <span className={`badge ${s.status}`}>{s.status}</span>
              </div>
              <p style={{ fontSize: '0.8rem', color: '#8b949e', marginTop: '0.5rem' }}>{s.description || 'No description'}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
