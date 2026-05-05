import { useState, useEffect } from 'react'
import { getMarketSymbols, downloadMarketData, getMarketBars, deleteMarketSymbol } from '../api'
import SvgChart from '../components/SvgChart'

const DEFAULT_START = new Date(new Date().setFullYear(new Date().getFullYear() - 2)).toISOString().slice(0, 10)
const DEFAULT_END   = new Date().toISOString().slice(0, 10)

export default function MarketData() {
  const [symbols, setSymbols]       = useState([])
  const [loading, setLoading]       = useState(true)
  const [selected, setSelected]     = useState(null)  // ticker string
  const [bars, setBars]             = useState([])
  const [barsLoading, setBarsLoading] = useState(false)
  const [showTable, setShowTable]   = useState(false)
  const [downloading, setDownloading] = useState(false)

  const [dlForm, setDlForm] = useState({
    ticker: '',
    startDate: DEFAULT_START,
    endDate: DEFAULT_END,
  })
  const [showDlForm, setShowDlForm] = useState(false)
  const [dlError, setDlError]       = useState('')

  const loadSymbols = () => {
    setLoading(true)
    getMarketSymbols()
      .then(s => { setSymbols(s); setLoading(false) })
      .catch(() => setLoading(false))
  }

  useEffect(loadSymbols, [])

  const handleDownload = async (e) => {
    e.preventDefault()
    setDlError('')
    setDownloading(true)
    try {
      await downloadMarketData({
        ticker:    dlForm.ticker.trim().toUpperCase(),
        startDate: dlForm.startDate,
        endDate:   dlForm.endDate,
      })
      setShowDlForm(false)
      setDlForm({ ...dlForm, ticker: '' })
      loadSymbols()
    } catch (err) {
      setDlError(err.message)
    } finally {
      setDownloading(false)
    }
  }

  const handleSelect = async (ticker) => {
    setSelected(ticker)
    setBarsLoading(true)
    setShowTable(false)
    try {
      const data = await getMarketBars(ticker)
      setBars(data)
    } catch {
      setBars([])
    } finally {
      setBarsLoading(false)
    }
  }

  const handleDelete = async (ticker) => {
    if (!confirm(`Delete all ${ticker} data? This cannot be undone.`)) return
    try {
      await deleteMarketSymbol(ticker)
      if (selected === ticker) { setSelected(null); setBars([]) }
      loadSymbols()
    } catch (err) {
      alert('Delete failed: ' + err.message)
    }
  }

  const handleRefresh = async (sym) => {
    setDlError('')
    setDownloading(true)
    try {
      await downloadMarketData({
        ticker:    sym.ticker,
        startDate: sym.firstDate.slice(0, 10),
        endDate:   DEFAULT_END,
      })
      loadSymbols()
      if (selected === sym.ticker) handleSelect(sym.ticker)
    } catch (err) {
      alert('Refresh failed: ' + err.message)
    } finally {
      setDownloading(false)
    }
  }

  const closePrices = bars.map(b => b.close)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2>Market Data</h2>
        <button className="primary" onClick={() => { setShowDlForm(!showDlForm); setDlError('') }}>
          + Download Symbol
        </button>
      </div>

      {showDlForm && (
        <div className="card">
          <h3>Download OHLCV Data</h3>
          <form onSubmit={handleDownload}>
            <div className="form-row">
              <label>Ticker Symbol</label>
              <input
                value={dlForm.ticker}
                onChange={e => setDlForm({ ...dlForm, ticker: e.target.value.toUpperCase() })}
                placeholder="e.g. SPY, AAPL, NVDA, BTC-USD"
                required
              />
            </div>
            <div className="form-row">
              <label>Start Date</label>
              <input type="date" value={dlForm.startDate} onChange={e => setDlForm({ ...dlForm, startDate: e.target.value })} />
            </div>
            <div className="form-row">
              <label>End Date</label>
              <input type="date" value={dlForm.endDate} onChange={e => setDlForm({ ...dlForm, endDate: e.target.value })} />
            </div>
            {dlError && <p style={{ color: '#f85149', fontSize: '0.85rem', margin: '0.25rem 0' }}>{dlError}</p>}
            <button className="primary" type="submit" disabled={downloading}>
              {downloading ? 'Downloading…' : 'Download'}
            </button>
          </form>
        </div>
      )}

      {loading ? (
        <p className="loading">Loading…</p>
      ) : symbols.length === 0 ? (
        <p className="empty">No symbols downloaded yet. Click "Download Symbol" to get started.</p>
      ) : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table>
            <thead>
              <tr>
                <th>Ticker</th>
                <th>Bars</th>
                <th>First Date</th>
                <th>Last Date</th>
                <th>Last Synced</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {symbols.map(sym => (
                <tr
                  key={sym.ticker}
                  style={{ cursor: 'pointer', background: selected === sym.ticker ? '#1f6feb22' : undefined }}
                  onClick={() => handleSelect(sym.ticker)}
                >
                  <td><strong style={{ color: '#58a6ff' }}>{sym.ticker}</strong></td>
                  <td>{sym.barCount.toLocaleString()}</td>
                  <td>{new Date(sym.firstDate).toLocaleDateString()}</td>
                  <td>{new Date(sym.lastDate).toLocaleDateString()}</td>
                  <td style={{ fontSize: '0.8rem', color: '#8b949e' }}>{new Date(sym.fetchedAt).toLocaleString()}</td>
                  <td style={{ textAlign: 'right' }}>
                    <button
                      className="nav-btn"
                      style={{ fontSize: '0.75rem', marginRight: '0.25rem' }}
                      disabled={downloading}
                      onClick={ev => { ev.stopPropagation(); handleRefresh(sym) }}
                    >
                      Refresh
                    </button>
                    <button
                      className="danger"
                      style={{ fontSize: '0.75rem' }}
                      onClick={ev => { ev.stopPropagation(); handleDelete(sym.ticker) }}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selected && (
        <div style={{ marginTop: '1.5rem' }}>
          <h3 style={{ margin: '0 0 0.75rem' }}>{selected} — {bars.length} bars</h3>

          {barsLoading ? (
            <p className="loading">Loading chart…</p>
          ) : (
            <>
              {closePrices.length > 1 && (
                <div className="card">
                  <h4 style={{ margin: '0 0 0.5rem', color: '#8b949e', fontWeight: 'normal' }}>Close Price</h4>
                  <SvgChart data={closePrices} width={740} height={200} color="#58a6ff" label={selected} />
                  <div style={{ fontSize: '0.75rem', color: '#8b949e', marginTop: '0.25rem' }}>
                    {bars.length > 0 && `${new Date(bars[0].date).toLocaleDateString()} — ${new Date(bars[bars.length - 1].date).toLocaleDateString()}`}
                  </div>
                </div>
              )}

              <div className="card">
                <h4
                  style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '0 0 0.5rem' }}
                >
                  <span>OHLCV Table</span>
                  <button className="nav-btn" style={{ fontSize: '0.75rem' }} onClick={() => setShowTable(!showTable)}>
                    {showTable ? 'Hide' : `Show (${bars.length} rows)`}
                  </button>
                </h4>
                {showTable && (
                  <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                    <table>
                      <thead>
                        <tr><th>Date</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>Volume</th></tr>
                      </thead>
                      <tbody>
                        {bars.map((b, i) => (
                          <tr key={i}>
                            <td>{new Date(b.date).toLocaleDateString()}</td>
                            <td>${b.open?.toFixed(2)}</td>
                            <td>${b.high?.toFixed(2)}</td>
                            <td>${b.low?.toFixed(2)}</td>
                            <td style={{ fontWeight: 'bold' }}>${b.close?.toFixed(2)}</td>
                            <td>{(b.volume / 1_000_000).toFixed(2)}M</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  )
}
