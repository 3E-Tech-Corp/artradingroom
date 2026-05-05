import { useState, useEffect } from 'react'
import Dashboard from './pages/Dashboard'
import Strategies from './pages/Strategies'
import Risk from './pages/Risk'
import Trading from './pages/Trading'
import Research from './pages/Research'
import Agents from './pages/Agents'
import MarketData from './pages/MarketData'
import './App.css'

const PAGES = {
  dashboard:  { label: 'Dashboard',   component: Dashboard },
  strategies: { label: 'Strategy Lab', component: Strategies },
  marketdata: { label: 'Market Data',  component: MarketData },
  risk:       { label: 'Risk Console', component: Risk },
  trading:    { label: 'Trading',      component: Trading },
  research:   { label: 'Research',     component: Research },
  agents:     { label: 'Agents',       component: Agents },
}

function getInitialPage() {
  const hash = window.location.hash.replace('#', '')
  return PAGES[hash] ? hash : 'dashboard'
}

function App() {
  const [page, setPage] = useState(getInitialPage)
  const [mode, setMode] = useState('paper')

  useEffect(() => {
    window.location.hash = page
  }, [page])

  useEffect(() => {
    const onHashChange = () => {
      const hash = window.location.hash.replace('#', '')
      if (PAGES[hash]) setPage(hash)
    }
    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [])
  const PageComponent = PAGES[page].component

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1>AITrade</h1>
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
