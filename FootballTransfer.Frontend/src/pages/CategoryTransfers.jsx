import { useEffect, useMemo, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { fetchTransfers } from '../api/transferApi'
import TransferCard from '../components/TransferCard'

const CATEGORY_MAP = {
  official: { title: 'Official Deals', match: 'Completed Transfer', desc: 'Confirmed transfers reported by reliable sources.' },
  rumours: { title: 'Rumours', match: 'Rumour', desc: 'Transfer rumours circulating in news sources.' },
  contracts: { title: 'Contract Renewals', match: 'Contract', desc: 'Contract extensions and renewals.' },
  free: { title: 'Free Transfers', match: 'Free Transfer', desc: 'Players moving on free transfers.' },
}

export default function CategoryTransfers() {
  const { category } = useParams()
  const cfg = CATEGORY_MAP[category] || CATEGORY_MAP.official
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [query, setQuery] = useState('')

  useEffect(() => {
    let mounted = true
    async function load() {
      try {
        setLoading(true)
        const d = await fetchTransfers()
        if (!mounted) return
        setItems(d || [])
      } catch (e) {
        if (mounted) setError(e.message)
      } finally {
        if (mounted) setLoading(false)
      }
    }
    load()
    return () => { mounted = false }
  }, [])

  const filtered = useMemo(() => {
    const desired = (cfg.match || '').toLowerCase()
    const base = items.filter((it) => {
      const t = (it.transferType || it.type || '').toString().toLowerCase()
      return t === desired
    })
    if (!query) return base
    const q = query.toLowerCase()
    return base.filter((it) => {
      const player = (it.player || it.playerName || it.title || '').toString().toLowerCase()
      const from = (it.fromClub || it.from || it.clubFrom || '').toString().toLowerCase()
      const to = (it.toClub || it.to || it.clubTo || '').toString().toLowerCase()
      const title = (it.title || it.headline || '').toString().toLowerCase()
      return player.includes(q) || from.includes(q) || to.includes(q) || title.includes(q)
    })
  }, [items, cfg, query])

  return (
    <div className="page transfers-category-page">
      <div className="page-header">
        <h2>{cfg.title}</h2>
        <p className="muted">{cfg.desc}</p>
        <div style={{ marginTop: 12 }}>
          <input className="site-search" placeholder={`Search ${cfg.title}`} value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
      </div>

      {loading && <div className="loading">Loading…</div>}
      {error && <div className="error">{error}</div>}

      <div className="news-list">
        {filtered.map((it) => (
          <div key={it.id || it._id || it.newsId} className="news-item">
            <TransferCard item={it} />
            <div className="card-actions"><Link to={`/transfer/${it.id || it._id || it.newsId}`} className="view-btn">View Details</Link></div>
          </div>
        ))}
        {filtered.length === 0 && !loading && <div className="empty muted">No items found.</div>}
      </div>
    </div>
  )
}
