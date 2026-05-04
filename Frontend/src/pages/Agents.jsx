import { useState, useEffect } from 'react'
import { getAgents, createAgentTask } from '../api'
import StatusDot from '../components/StatusDot'

const ROLE_LABELS = {
  ceo: 'CEO',
  research: 'Research',
  backtest: 'Backtest',
  risk: 'Risk Mgmt',
  execution: 'Execution',
  cost_optimizer: 'Cost Optimizer'
}

export default function Agents() {
  const [agents, setAgents] = useState([])
  const [selected, setSelected] = useState(null)
  const [showTask, setShowTask] = useState(false)
  const [taskForm, setTaskForm] = useState({ taskType: 'research', payload: '{}' })
  const [loading, setLoading] = useState(true)
  const [seeding, setSeeding] = useState(false)

  const load = () => {
    getAgents().then(a => { setAgents(a); setLoading(false) }).catch(() => setLoading(false))
  }

  useEffect(load, [])

  const handleSeed = async () => {
    setSeeding(true)
    try {
      const res = await fetch('/api/agents/seed', { method: 'POST' })
      if (!res.ok) { const t = await res.text(); alert(t); }
      load()
    } catch (err) { alert(err.message) }
    setSeeding(false)
  }

  const handleCreateTask = async (e) => {
    e.preventDefault()
    try {
      await createAgentTask(selected.id, taskForm)
      setShowTask(false)
      setTaskForm({ taskType: 'research', payload: '{}' })
      load()
    } catch (err) { alert('Error: ' + err.message) }
  }

  if (loading) return <p className="loading">Loading agents...</p>

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2>Agents</h2>
        {agents.length === 0 && (
          <button className="primary" onClick={handleSeed} disabled={seeding}>
            {seeding ? 'Seeding...' : 'Hire All Agents'}
          </button>
        )}
      </div>

      {agents.length === 0 ? (
        <div className="card">
          <p className="empty">No agents hired yet. Click "Hire All Agents" to deploy your trading firm.</p>
        </div>
      ) : (
        <div className="grid">
          {agents.map(agent => (
            <div key={agent.id} className="card" style={{ cursor: 'pointer', border: selected?.id === agent.id ? '1px solid #00ff88' : '1px solid #30363d' }} onClick={() => { setSelected(agent); setShowTask(false) }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                <strong style={{ color: '#f0f6fc' }}>{agent.name}</strong>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <StatusDot status={agent.status} />
                  <span style={{ fontSize: '0.7rem', color: '#8b949e' }}>{agent.status}</span>
                </div>
              </div>
              <span className="badge draft">{ROLE_LABELS[agent.role] || agent.role}</span>
              <p style={{ fontSize: '0.78rem', color: '#8b949e', marginTop: '0.5rem' }}>{agent.mandate?.slice(0, 120)}{agent.mandate?.length > 120 ? '...' : ''}</p>
              {agent.lastHeartbeat && (
                <p style={{ fontSize: '0.7rem', color: '#484f58', marginTop: '0.5rem' }}>
                  Last seen: {new Date(agent.lastHeartbeat).toLocaleString()}
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      {selected && (
        <div className="card" style={{ marginTop: '1rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3>{selected.name} — Tasks</h3>
            <button className="primary" onClick={() => setShowTask(!showTask)}>+ Create Task</button>
          </div>

          {showTask && (
            <form onSubmit={handleCreateTask} style={{ marginTop: '0.75rem', display: 'flex', gap: '0.5rem', alignItems: 'flex-end', flexWrap: 'wrap' }}>
              <div className="form-row" style={{ minWidth: '150px' }}>
                <label>Task Type</label>
                <select value={taskForm.taskType} onChange={e => setTaskForm({...taskForm, taskType: e.target.value})}>
                  <option value="research">research</option>
                  <option value="backtest">backtest</option>
                  <option value="validate">validate</option>
                  <option value="execute">execute</option>
                  <option value="report">report</option>
                  <option value="optimize">optimize</option>
                </select>
              </div>
              <div className="form-row" style={{ flex: 1 }}>
                <label>Payload (JSON)</label>
                <input value={taskForm.payload} onChange={e => setTaskForm({...taskForm, payload: e.target.value})} />
              </div>
              <button className="primary" type="submit" style={{ marginBottom: '0.75rem' }}>Submit</button>
            </form>
          )}

          {selected.tasks?.length > 0 ? (
            <table style={{ marginTop: '0.75rem' }}>
              <thead><tr><th>Type</th><th>Status</th><th>Created</th><th>Completed</th></tr></thead>
              <tbody>
                {selected.tasks.map((t, i) => (
                  <tr key={i}>
                    <td>{t.taskType}</td>
                    <td><span className={`badge ${t.status === 'completed' ? 'live' : t.status === 'failed' ? 'archived' : 'backtested'}`}>{t.status}</span></td>
                    <td style={{ fontSize: '0.75rem' }}>{new Date(t.createdAt).toLocaleString()}</td>
                    <td style={{ fontSize: '0.75rem' }}>{t.completedAt ? new Date(t.completedAt).toLocaleString() : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="empty" style={{ marginTop: '0.75rem' }}>No tasks yet for this agent</p>
          )}
        </div>
      )}
    </div>
  )
}
