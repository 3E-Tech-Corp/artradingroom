import { useState } from 'react'
import { runMonteCarlo } from '../api'

function Histogram({ data, color = '#00ff88', width = 300, height = 80, label }) {
  if (!data || data.length === 0) return <p style={{ fontSize: '0.75rem', color: '#484f58', fontStyle: 'italic' }}>No data</p>
  const max = Math.max(...data)
  const pad = { top: 6, right: 4, bottom: 4, left: 4 }
  const cw = width - pad.left - pad.right
  const ch = height - pad.top - pad.bottom
  const barW = cw / data.length
  return (
    <svg width={width} height={height} style={{ display: 'block' }}>
      <rect width={width} height={height} fill="#0a0f0a" rx="4" />
      {data.map((v, i) => {
        const bh = max > 0 ? (v / max) * ch : 0
        const x = pad.left + i * barW
        const y = pad.top + ch - bh
        return <rect key={i} x={x + 0.5} y={y} width={Math.max(barW - 1, 0.5)} height={bh} fill={color} opacity="0.75" />
      })}
      <text x={width / 2} y={height - 1} textAnchor="middle" fill="#484f58" fontSize="7" fontFamily="monospace">{label}</text>
    </svg>
  )
}

function fmt(v, isPercent) {
  if (v == null) return '—'
  return isPercent ? `${(v * 100).toFixed(1)}%` : v.toFixed(3)
}

const METRICS = [
  { label: 'Total Return', key: 'totalReturn', isPercent: true },
  { label: 'Max Drawdown', key: 'maxDrawdown', isPercent: true },
  { label: 'Sharpe Ratio', key: 'sharpeRatio', isPercent: false },
  { label: 'Win Rate', key: 'winRate', isPercent: true },
]
const COLS = ['mean', 'median', 'stdDev', 'percentile5', 'percentile25', 'percentile75', 'percentile95']
const COL_LABELS = { mean: 'Mean', median: 'Median', stdDev: 'σ', percentile5: 'P5', percentile25: 'P25', percentile75: 'P75', percentile95: 'P95' }

export default function MonteCarloPanel({ strategyId, btForm }) {
  const [simulations, setSimulations] = useState(1000)
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState(null)
  const [error, setError] = useState(null)

  async function handleRun(e) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const r = await runMonteCarlo(strategyId, {
        ticker: btForm.ticker,
        startDate: btForm.startDate,
        endDate: btForm.endDate,
        startingCapital: Number(btForm.startingCapital),
        simulations: Number(simulations),
      })
      setResult(r)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="card">
      <h3>Monte Carlo Simulation</h3>

      <form onSubmit={handleRun} style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: '1rem' }}>
        <div style={{ flex: '0 0 160px' }}>
          <label style={{ fontSize: '0.72rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Simulations</label>
          <input type="number" value={simulations} min="100" max="10000" step="100" onChange={e => setSimulations(e.target.value)} />
        </div>
        <div style={{ fontSize: '0.72rem', color: '#484f58', alignSelf: 'center' }}>
          {btForm.ticker} · {btForm.startDate} → {btForm.endDate}
        </div>
        <button className="primary" type="submit" disabled={loading} style={{ flexShrink: 0, alignSelf: 'flex-end' }}>
          {loading ? 'Running...' : 'Run Monte Carlo'}
        </button>
      </form>

      {error && <p className="error" style={{ marginBottom: '0.75rem' }}>{error}</p>}

      {result && (
        <>
          <div style={{ overflowX: 'auto', marginBottom: '1rem' }}>
            <table>
              <thead>
                <tr>
                  <th style={{ minWidth: '110px' }}>Metric</th>
                  {COLS.map(c => <th key={c} style={{ textAlign: 'right' }}>{COL_LABELS[c]}</th>)}
                </tr>
              </thead>
              <tbody>
                {METRICS.map(({ label, key, isPercent }) => {
                  const row = result[key] || {}
                  return (
                    <tr key={key}>
                      <td style={{ color: '#8b949e', fontSize: '0.75rem' }}>{label}</td>
                      {COLS.map(c => (
                        <td key={c} style={{ textAlign: 'right', fontFamily: 'monospace', fontSize: '0.78rem' }}>
                          {fmt(row[c], isPercent)}
                        </td>
                      ))}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
            <div>
              <p style={{ fontSize: '0.7rem', color: '#8b949e', marginBottom: '4px', textTransform: 'uppercase' }}>
                Return Distribution ({result.simulations?.toLocaleString()} runs)
              </p>
              <Histogram data={result.returnDistribution} color="#00ff88" width={300} height={80} label="Total Return" />
            </div>
            <div>
              <p style={{ fontSize: '0.7rem', color: '#8b949e', marginBottom: '4px', textTransform: 'uppercase' }}>
                Drawdown Distribution
              </p>
              <Histogram data={result.drawdownDistribution} color="#f85149" width={300} height={80} label="Max Drawdown" />
            </div>
          </div>
        </>
      )}
    </div>
  )
}
