import { useState } from 'react'
import Dashboard from './pages/Dashboard'
import Strategies from './pages/Strategies'
import Risk from './pages/Risk'
import Trading from './pages/Trading'
import Research from './pages/Research'
import Agents from './pages/Agents'
import './App.css'

const PAGES = {
  dashboard: { label: 'Dashboard', component: Dashboard },
  strategies: { label: 'Strategy Lab', component: Strategies },
  risk: { label: 'Risk Console', component: Risk },
  trading: { label: 'Trading', component: Trading },
  research: { label: 'Research', component: Research },
  agents: { label: 'Agents', component: Agents },
}

function App() {
  const [page, setPage] = useState('dashboard')
  const [mode, setMode] = useState('paper')
  const PageComponent = PAGES[page].component

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1>ArTrading</h1>
          <span className={`mode-badge ${mode}`}>{mode.toUpperCase()}</span>
        </div>
        <nav>
          {Object.entries(PAGES).map(([key, { label }]) => (
            <button
              key={key}
              className={`nav-btn ${page === key ? 'active' : ''}`}
              onClick={() => setPage(key)}
            >
              {label}
            </button>
          ))}
        </nav>
      </aside>
      <main className="content">
        <PageComponent mode={mode} setMode={setMode} />
      </main>
    </div>
  )
}

export default App
