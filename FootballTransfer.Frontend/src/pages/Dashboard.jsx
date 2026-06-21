import { useEffect, useMemo, useState } from 'react'
import SearchBar from '../components/SearchBar'
import StatCard from '../components/StatCard'
import TransferCard from '../components/TransferCard'
import TransfersTable from '../components/TransfersTable'
import * as api from '../services/api'

export default function Dashboard() {
  const [latest, setLatest] = useState([])
  const [transfers, setTransfers] = useState([])
  const [results, setResults] = useState(null)
  const [selected, setSelected] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    setLoading(true)
    Promise.all([
      api.getLatestTransfers().catch((e) => {
        setError(e.message);
        return []
      }),
      api.getTransfers().catch((e) => {
        setError(e.message);
        return []
      }),
    ])
      .then(([a, b]) => {
        setLatest(a || [])
        setTransfers(b || [])
      })
      .finally(() => setLoading(false))
  }, [])

  async function handleSearch(q) {
    // preserve existing API search behavior
    setLoading(true)
    setError(null)
    try {
      const res = await api.searchNews(q)
      setResults(res || [])
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  async function openItem(item) {
    setError(null)
    try {
      const detail = await api.getNewsById(item.id || item.newsId || item._id)
      setSelected(detail)
    } catch (e) {
      setError(e.message)
    }
  }

  const sourceItems = results !== null ? results : transfers

  const totalTransfers = sourceItems.length
  const latestCount = latest.length
  const avgConfidence = useMemo(() => {
    const arr = sourceItems
      .map((it) => {
        const v = it.confidence ?? it.confidenceScore ?? it.score
        const n = parseFloat(v)
        return isNaN(n) ? null : n
      })
      .filter((v) => v !== null)
    if (arr.length === 0) return 'N/A'
    const avg = arr.reduce((s, x) => s + x, 0) / arr.length
    return `${avg.toFixed(1)}%`
  }, [sourceItems])

  return (
    <div className="dashboard app-dash">
      <header className="dash-header">
        <div>
          <h1>Transfer Intelligence</h1>
          <p className="muted">Live feed and analytics for football transfers</p>
        </div>
        <div className="header-actions">
          <SearchBar onSearch={handleSearch} />
        </div>
      </header>

      {error && <div className="error">{error}</div>}

      <section className="stats-row">
        <StatCard title="Total Transfers" value={totalTransfers} subtitle="Records in view" />
        <StatCard title="Latest Transfers" value={latestCount} subtitle="Most recent updates" />
        <StatCard title="Average Confidence" value={avgConfidence} subtitle="Across visible records" />
      </section>

      <section className="content-row">
        <div className="left-col">
          <div className="panel">
            <div className="panel-header">
              <h2>Latest Transfers</h2>
              <div className="panel-sub">Recent activity</div>
            </div>
            <div className="latest-list">
              {latest.length === 0 && <div className="empty muted">No recent transfers</div>}
              {latest.map((it) => (
                <TransferCard key={it.id || it.newsId || it._id} item={it} onClick={openItem} />
              ))}
            </div>
          </div>

          <div className="panel">
            <div className="panel-header">
              <h2>Transfers</h2>
              <div className="panel-sub">Search, sort and paginate</div>
            </div>
            <TransfersTable items={sourceItems} onRowClick={openItem} />
          </div>
        </div>

        <aside className="right-col panel details-panel">
          <div className="panel-header">
            <h2>Details</h2>
            <div className="panel-sub">Selected transfer</div>
          </div>

          {selected ? (
            <div className="detail-card">
              <h3>{selected.player || selected.playerName || selected.title || selected.headline}</h3>
              <div className="detail-meta">{selected.fromClub || selected.from || selected.clubFrom} → {selected.toClub || selected.to || selected.clubTo}</div>
              <div className="detail-row">
                <div><strong>Fee:</strong> {selected.fee || selected.transferFee || selected.amount || 'Undisclosed'}</div>
                <div><strong>Type:</strong> {selected.transferType || selected.type || '—'}</div>
              </div>
              <div className="detail-body">{selected.content || selected.body || selected.summary || 'No additional details available.'}</div>
              <div className="detail-meta small">{selected.date || selected.publishedAt}</div>
            </div>
          ) : (
            <div className="empty muted">Select a transfer to view details</div>
          )}
        </aside>
      </section>

      {loading && <div className="loading">Loading…</div>}
    </div>
  )
}
