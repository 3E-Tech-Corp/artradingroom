import { useState, useEffect } from 'react'
import { getRiskThresholds, updateRiskThreshold, getAuditLog } from '../api'

export default function Risk() {
  const [thresholds, setThresholds] = useState(null)
  const [audit, setAudit] = useState([])
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({})
  const [loading, setLoading] = useState(true)

  const load = () => {
    Promise.all([
      getRiskThresholds().catch(() => ({ id: 1, maxDrawdown: 0.20, maxPositionSize: 0.10, dailyLossLimit: 0.05, maxLeverage: 1.0, requireApproval: true })),
      getAuditLog({ entity: 'Trade', limit: 20 }).catch(() => [])
    ]).then(([t, a]) => {
      setThresholds(t)
      setForm(t)
      setAudit(a)
      setLoading(false)
    })
  }

  useEffect(load, [])

  const handleSave = async () => {
    try {
      await updateRiskThreshold(thresholds.id, {
        maxDrawdown: Number(form.maxDrawdown),
        maxPositionSize: Number(form.maxPositionSize),
        dailyLossLimit: Number(form.dailyLossLimit),
        maxLeverage: Number(form.maxLeverage),
        requireApproval: form.requireApproval
      })
      setEditing(false)
      load()
    } catch (err) { alert('Error: ' + err.message) }
  }

  if (loading) return <p className="loading">Loading risk console...</p>

  return (
    <div>
      <h2 style={{ marginBottom: '1rem' }}>Risk Console</h2>

      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3>Risk Thresholds</h3>
          {!editing ? (
            <button className="primary" onClick={() => setEditing(true)}>Edit</button>
          ) : (
            <div>
              <button className="primary" onClick={handleSave}>Save</button>
              <button className="nav-btn" onClick={() => { setEditing(false); setForm(thresholds) }}>Cancel</button>
            </div>
          )}
        </div>

        <div className="stats-row" style={{ marginTop: '0.75rem' }}>
          <div className="stat">
            <label>Max Drawdown</label>
            {editing ? <input type="number" step="0.01" value={form.maxDrawdown} onChange={e => setForm({...form, maxDrawdown: e.target.value})} />
              : <div className="value">{(thresholds.maxDrawdown * 100).toFixed(0)}%</div>}
          </div>
          <div className="stat">
            <label>Max Position Size</label>
            {editing ? <input type="number" step="0.01" value={form.maxPositionSize} onChange={e => setForm({...form, maxPositionSize: e.target.value})} />
              : <div className="value">{(thresholds.maxPositionSize * 100).toFixed(0)}%</div>}
          </div>
          <div className="stat">
            <label>Daily Loss Limit</label>
            {editing ? <input type="number" step="0.01" value={form.dailyLossLimit} onChange={e => setForm({...form, dailyLossLimit: e.target.value})} />
              : <div className="value">{(thresholds.dailyLossLimit * 100).toFixed(0)}%</div>}
          </div>
          <div className="stat">
            <label>Max Leverage</label>
            {editing ? <input type="number" step="0.1" value={form.maxLeverage} onChange={e => setForm({...form, maxLeverage: e.target.value})} />
              : <div className="value">{thresholds.maxLeverage}x</div>}
          </div>
          <div className="stat">
            <label>Require Approval</label>
            {editing ? <select value={form.requireApproval} onChange={e => setForm({...form, requireApproval: e.target.value === 'true'})}>
                <option value="true">Yes</option><option value="false">No</option>
              </select>
              : <div className="value">{thresholds.requireApproval ? 'Yes' : 'No'}</div>}
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Risk Audit Log</h3>
        {audit.length === 0 ? <p className="empty">No risk events yet</p> : (
          <table>
            <thead><tr><th>Action</th><th>Entity</th><th>Reasoning</th><th>Time</th></tr></thead>
            <tbody>
              {audit.map((a, i) => (
                <tr key={i}>
                  <td><span className={`badge ${a.action.includes('reject') ? 'live' : 'backtested'}`}>{a.action}</span></td>
                  <td>{a.entity}</td>
                  <td style={{ maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis' }}>{a.reasoning || '-'}</td>
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
