import { useState } from 'react'
import { runSensitivity } from '../api'

const PARAMS = [
  { value: 'rsiperiod',       label: 'RSI Period',           min: 5,    max: 50,   step: 1    },
  { value: 'smafastperiod',   label: 'SMA Fast Period',      min: 5,    max: 100,  step: 5    },
  { value: 'smamediumperiod', label: 'SMA Medium Period',    min: 10,   max: 200,  step: 10   },
  { value: 'smaslowperiod',   label: 'SMA Slow Period',      min: 50,   max: 400,  step: 25   },
  { value: 'bollingerperiod', label: 'Bollinger Period',     min: 5,    max: 50,   step: 5    },
  { value: 'bollingerstddev', label: 'Bollinger StdDev',     min: 0.5,  max: 4,    step: 0.5  },
  { value: 'entryvalue',      label: 'Entry Condition Value',min: 10,   max: 50,   step: 5    },
  { value: 'exitvalue',       label: 'Exit Condition Value', min: 50,   max: 90,   step: 5    },
  { value: 'positionsize',    label: 'Position Size (%)',    min: 0.01, max: 0.5,  step: 0.05 },
]

function MultiLineChart({ points, width = 640, height = 200 }) {
  if (!points || points.length < 2) return null

  const pad = { top: 12, right: 68, bottom: 28, left: 52 }
  const cw = width - pad.left - pad.right
  const ch = height - pad.top - pad.bottom

  const xVals = points.map(p => p.parameterValue)
  const returns = points.map(p => p.totalReturn)
  const sharpes = points.map(p => p.sharpeRatio)
  const drawdowns = points.map(p => p.maxDrawdown)

  const xMin = Math.min(...xVals), xMax = Math.max(...xVals)
  const xRange = xMax - xMin || 1

  function norm(vals) {
    const mn = Math.min(...vals), mx = Math.max(...vals)
    return { mn, mx, range: mx - mn || 1 }
  }

  function toPath(vals, n) {
    return vals.map((v, i) => {
      const x = pad.left + ((xVals[i] - xMin) / xRange) * cw
      const y = pad.top + ch - ((v - n.mn) / n.range) * ch
      return `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`
    }).join(' ')
  }

  const retN = norm(returns), shaN = norm(sharpes), draN = norm(drawdowns)

  const yTicks = [0, 0.25, 0.5, 0.75, 1].map(t => ({
    y: pad.top + t * ch,
    v: retN.mx - t * retN.range,
  }))

  const step = Math.ceil(points.length / 6)
  const xTicks = points.filter((_, i) => i % step === 0 || i === points.length - 1)

  return (
    <svg width={width} height={height} style={{ display: 'block' }}>
      <rect width={width} height={height} fill="#0a0f0a" rx="4" />

      {yTicks.map((t, i) => (
        <g key={i}>
          <line x1={pad.left} y1={t.y} x2={pad.left + cw} y2={t.y} stroke="#1a2a1a" strokeWidth="1" />
          <text x={pad.left - 4} y={t.y + 4} textAnchor="end" fill="#446644" fontSize="8" fontFamily="monospace">
            {(t.v * 100).toFixed(0)}%
          </text>
        </g>
      ))}

      {xTicks.map((p, i) => {
        const x = pad.left + ((p.parameterValue - xMin) / xRange) * cw
        return (
          <text key={i} x={x} y={height - 6} textAnchor="middle" fill="#446644" fontSize="8" fontFamily="monospace">
            {p.parameterValue}
          </text>
        )
      })}

      <path d={toPath(returns, retN)} fill="none" stroke="#00ff88" strokeWidth="2" />
      <path d={toPath(sharpes, shaN)} fill="none" stroke="#58a6ff" strokeWidth="1.5" strokeDasharray="5,3" />
      <path d={toPath(drawdowns, draN)} fill="none" stroke="#f85149" strokeWidth="1.5" strokeDasharray="3,3" />

      <g>
        <line x1={pad.left + cw + 6}  y1={pad.top + 10} x2={pad.left + cw + 18} y2={pad.top + 10} stroke="#00ff88" strokeWidth="2" />
        <text x={pad.left + cw + 20} y={pad.top + 14} fill="#00ff88" fontSize="8" fontFamily="monospace">Return</text>
        <line x1={pad.left + cw + 6}  y1={pad.top + 24} x2={pad.left + cw + 18} y2={pad.top + 24} stroke="#58a6ff" strokeWidth="1.5" strokeDasharray="5,3" />
        <text x={pad.left + cw + 20} y={pad.top + 28} fill="#58a6ff" fontSize="8" fontFamily="monospace">Sharpe</text>
        <line x1={pad.left + cw + 6}  y1={pad.top + 38} x2={pad.left + cw + 18} y2={pad.top + 38} stroke="#f85149" strokeWidth="1.5" strokeDasharray="3,3" />
        <text x={pad.left + cw + 20} y={pad.top + 42} fill="#f85149" fontSize="8" fontFamily="monospace">Drwdn</text>
      </g>
    </svg>
  )
}

export default function SensitivityPanel({ strategyId, btForm }) {
  const [paramDef, setParamDef] = useState(PARAMS[0])
  const [range, setRange] = useState({ min: PARAMS[0].min, max: PARAMS[0].max, step: PARAMS[0].step })
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState(null)
  const [error, setError] = useState(null)

  function selectParam(val) {
    const p = PARAMS.find(p => p.value === val)
    setParamDef(p)
    setRange({ min: p.min, max: p.max, step: p.step })
    setResult(null)
  }

  async function handleRun(e) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const r = await runSensitivity(strategyId, {
        ticker: btForm.ticker,
        startDate: btForm.startDate,
        endDate: btForm.endDate,
        startingCapital: Number(btForm.startingCapital),
        parameter: paramDef.value,
        min: Number(range.min),
        max: Number(range.max),
        step: Number(range.step),
      })
      setResult(r)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const points = result?.points || []

  return (
    <div className="card">
      <h3>Sensitivity Analysis</h3>

      <form onSubmit={handleRun} style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: '1rem' }}>
        <div>
          <label style={{ fontSize: '0.72rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Parameter</label>
          <select value={paramDef.value} onChange={e => selectParam(e.target.value)} style={{ minWidth: '170px' }}>
            {PARAMS.map(p => <option key={p.value} value={p.value}>{p.label}</option>)}
          </select>
        </div>
        <div>
          <label style={{ fontSize: '0.72rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Min</label>
          <input type="number" value={range.min} step={paramDef.step} onChange={e => setRange({ ...range, min: e.target.value })} style={{ width: '80px' }} />
        </div>
        <div>
          <label style={{ fontSize: '0.72rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Max</label>
          <input type="number" value={range.max} step={paramDef.step} onChange={e => setRange({ ...range, max: e.target.value })} style={{ width: '80px' }} />
        </div>
        <div>
          <label style={{ fontSize: '0.72rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Step</label>
          <input type="number" value={range.step} step={paramDef.step} min={paramDef.step} onChange={e => setRange({ ...range, step: e.target.value })} style={{ width: '80px' }} />
        </div>
        <button className="primary" type="submit" disabled={loading} style={{ flexShrink: 0, alignSelf: 'flex-end' }}>
          {loading ? 'Running...' : 'Run Analysis'}
        </button>
      </form>

      {error && <p className="error" style={{ marginBottom: '0.75rem' }}>{error}</p>}

      {points.length > 0 && (
        <>
          <MultiLineChart points={points} width={640} height={200} />
          <div style={{ maxHeight: '280px', overflowY: 'auto', marginTop: '1rem' }}>
            <table>
              <thead>
                <tr>
                  <th>{paramDef.label}</th>
                  <th style={{ textAlign: 'right' }}>Return</th>
                  <th style={{ textAlign: 'right' }}>Sharpe</th>
                  <th style={{ textAlign: 'right' }}>Drawdown</th>
                  <th style={{ textAlign: 'right' }}>Win Rate</th>
                  <th style={{ textAlign: 'right' }}>Trades</th>
                </tr>
              </thead>
              <tbody>
                {points.map((p, i) => (
                  <tr key={i}>
                    <td style={{ color: '#58a6ff', fontFamily: 'monospace' }}>{p.parameterValue}</td>
                    <td style={{ textAlign: 'right' }} className={p.totalReturn >= 0 ? 'positive' : 'negative'}>
                      {(p.totalReturn * 100).toFixed(1)}%
                    </td>
                    <td style={{ textAlign: 'right' }}>{p.sharpeRatio?.toFixed(2)}</td>
                    <td style={{ textAlign: 'right' }} className="negative">{(p.maxDrawdown * 100).toFixed(1)}%</td>
                    <td style={{ textAlign: 'right' }}>{(p.winRate * 100).toFixed(0)}%</td>
                    <td style={{ textAlign: 'right' }}>{p.totalTrades}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
