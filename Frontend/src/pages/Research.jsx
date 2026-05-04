import { useState } from 'react'
import { generateBrief, scanMarket } from '../api'

export default function Research() {
  const [brief, setBrief] = useState(null)
  const [insights, setInsights] = useState([])
  const [loadingBrief, setLoadingBrief] = useState(false)
  const [loadingScan, setLoadingScan] = useState(false)
  const [scanTicker, setScanTicker] = useState('')

  const handleBrief = async () => {
    setLoadingBrief(true)
    try {
      const result = await generateBrief()
      setBrief(result)
      if (result.topInsights) setInsights(result.topInsights)
    } catch (err) { alert('Error: ' + err.message) }
    setLoadingBrief(false)
  }

  const handleScan = async () => {
    setLoadingScan(true)
    try {
      const result = await scanMarket(scanTicker ? { symbol: scanTicker } : {})
      setInsights(result)
    } catch (err) { alert('Error: ' + err.message) }
    setLoadingScan(false)
  }

  const strengthBar = (val) => (
    <div style={{ background: '#21262d', borderRadius: '4px', height: '6px', width: '100px', overflow: 'hidden', display: 'inline-block', verticalAlign: 'middle', marginLeft: '8px' }}>
      <div style={{ background: '#00ff88', height: '100%', width: `${(val * 100).toFixed(0)}%` }} />
    </div>
  )

  return (
    <div>
      <h2 style={{ marginBottom: '1rem' }}>Research Feed</h2>

      <div className="card">
        <h3>Weekly Brief</h3>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <button className="primary" onClick={handleBrief} disabled={loadingBrief}>
            {loadingBrief ? 'Generating...' : 'Generate Weekly Brief'}
          </button>
        </div>
        {brief && (
          <div style={{ marginTop: '1rem' }}>
            <div style={{ padding: '0.75rem', background: '#0d1117', borderRadius: '6px', borderLeft: '3px solid #00ff88' }}>
              <strong style={{ color: '#00ff88' }}>Theme: </strong>{brief.weeklyTheme}
            </div>
            <p style={{ marginTop: '0.75rem', fontSize: '0.85rem', color: '#8b949e' }}>{brief.summary}</p>
          </div>
        )}
      </div>

      <div className="card">
        <h3>Market Scan</h3>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <input
            value={scanTicker}
            onChange={e => setScanTicker(e.target.value.toUpperCase())}
            placeholder="Ticker (blank = all)"
            style={{ width: '180px' }}
          />
          <button className="primary" onClick={handleScan} disabled={loadingScan}>
            {loadingScan ? 'Scanning...' : 'Scan Market'}
          </button>
        </div>
      </div>

      {insights.length > 0 && (
        <div className="card">
          <h3>Market Insights</h3>
          <table>
            <thead><tr><th>Symbol</th><th>Type</th><th>Strength</th><th>Description</th><th>Time</th></tr></thead>
            <tbody>
              {insights.map((ins, i) => (
                <tr key={i}>
                  <td><strong style={{ color: '#58a6ff' }}>{ins.symbol}</strong></td>
                  <td><span className="badge backtested">{ins.insightType}</span></td>
                  <td>{(ins.strength * 100).toFixed(0)}% {strengthBar(ins.strength)}</td>
                  <td style={{ fontSize: '0.78rem' }}>{ins.description}</td>
                  <td style={{ fontSize: '0.75rem', color: '#8b949e' }}>{new Date(ins.observedAt).toLocaleTimeString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {brief?.strategyIdeas?.length > 0 && (
        <div>
          <h3 style={{ marginBottom: '0.75rem' }}>Strategy Ideas</h3>
          <div className="grid">
            {brief.strategyIdeas.map((idea, i) => (
              <div key={i} className="card">
                <strong style={{ color: '#f0f6fc' }}>{idea.name}</strong>
                <p style={{ fontSize: '0.8rem', color: '#8b949e', margin: '0.5rem 0' }}>{idea.description}</p>
                <div style={{ display: 'flex', gap: '1rem', fontSize: '0.8rem', marginBottom: '0.5rem' }}>
                  <span className="positive">+{(idea.expectedReturn * 100).toFixed(0)}% expected</span>
                  <span className="negative">{(idea.estimatedRisk * 100).toFixed(0)}% risk</span>
                </div>
                <div style={{ fontSize: '0.75rem', color: '#8b949e' }}>
                  {idea.relatedSymbols?.join(' · ')}
                </div>
                <p style={{ fontSize: '0.75rem', color: '#8b949e', marginTop: '0.5rem', borderTop: '1px solid #21262d', paddingTop: '0.5rem' }}>{idea.rationale}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
