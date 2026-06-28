import { useMemo, useState } from 'react'
import Badge from './Badge'

function parseNumber(val) {
  if (val == null) return NaN
  if (typeof val === 'number') return val
  const s = String(val).replace(/[^0-9.-]+/g, '')
  return parseFloat(s)
}

function formatFee(estimatedFee, feeCurrency) {
  if (estimatedFee === null || estimatedFee === undefined) return 'Undisclosed'
  const v = estimatedFee
  const rounded = typeof v === 'number' ? v : parseFloat(String(v))
  if (isNaN(rounded)) return 'Undisclosed'
  const m = `${rounded}m`
  const cur = (feeCurrency || '').toString().toUpperCase()
  if (cur === 'GBP' || cur === '£') return `£${m}`
  if (cur === 'EUR' || cur === '€') return `€${m}`
  if (cur === 'USD' || cur === '$') return `$${m}`
  return `${rounded}${cur ? ' ' + cur : ''}m`
}

export default function TransfersTable({ items = [], onRowClick }) {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [sortBy, setSortBy] = useState('date')
  const [dir, setDir] = useState('desc')
  const [filter, setFilter] = useState('')

  const filtered = useMemo(() => {
    const q = filter.trim().toLowerCase()
    if (!q) return items
    return items.filter((it) => {
      const player = (it.player || it.playerName || it.title || '').toString().toLowerCase()
      const from = (it.fromClub || it.from || it.clubFrom || '').toString().toLowerCase()
      const to = (it.toClub || it.to || it.clubTo || '').toString().toLowerCase()
      return player.includes(q) || from.includes(q) || to.includes(q) || (it.fee || '').toString().toLowerCase().includes(q)
    })
  }, [items, filter])

  const sorted = useMemo(() => {
    const arr = [...filtered]
    arr.sort((a, b) => {
      let av, bv
      if (sortBy === 'date') {
        av = new Date(a.date || a.publishedAt || 0).getTime() || 0
        bv = new Date(b.date || b.publishedAt || 0).getTime() || 0
      } else if (sortBy === 'fee') {
        av = a.estimatedFee ?? a.fee ?? a.transferFee ?? a.amount
        bv = b.estimatedFee ?? b.fee ?? b.transferFee ?? b.amount
        av = typeof av === 'number' ? av : parseNumber(av)
        bv = typeof bv === 'number' ? bv : parseNumber(bv)
      } else if (sortBy === 'confidence') {
        av = Number(a.confidence ?? a.confidenceScore ?? a.score)
        bv = Number(b.confidence ?? b.confidenceScore ?? b.score)
      }
      if (isNaN(av)) av = 0
      if (isNaN(bv)) bv = 0
      return dir === 'asc' ? av - bv : bv - av
    })
    return arr
  }, [filtered, sortBy, dir])

  const total = sorted.length
  const pages = Math.max(1, Math.ceil(total / pageSize))
  const pageItems = sorted.slice((page - 1) * pageSize, page * pageSize)

  function changeSort(field) {
    if (sortBy === field) {
      setDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortBy(field)
      setDir('desc')
    }
  }

  return (
    <div className="transfers-table">
      <div className="table-actions">
        <input placeholder="Filter by player, club, fee" value={filter} onChange={(e) => { setFilter(e.target.value); setPage(1) }} />
        <div className="page-size">
          <label>Rows:</label>
          <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1) }}>
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={25}>25</option>
          </select>
        </div>
      </div>

      <table>
        <thead>
          <tr>
            <th>Player</th>
            <th>From</th>
            <th>To</th>
            <th onClick={() => changeSort('fee')} className="sortable">Fee {sortBy === 'fee' ? (dir === 'asc' ? '▲' : '▼') : ''}</th>
            <th onClick={() => changeSort('date')} className="sortable">Date {sortBy === 'date' ? (dir === 'asc' ? '▲' : '▼') : ''}</th>
            <th onClick={() => changeSort('confidence')} className="sortable">Confidence {sortBy === 'confidence' ? (dir === 'asc' ? '▲' : '▼') : ''}</th>
            <th>Type</th>
          </tr>
        </thead>
        <tbody>
          {pageItems.map((it) => {
            const player = it.player || it.playerName || it.title || 'Unknown'
            const from = it.fromClub || it.from || it.clubFrom || '—'
            const to = it.toClub || it.to || it.clubTo || '—'
            const estimatedFee = it.estimatedFee ?? it.fee ?? it.transferFee ?? it.amount ?? null
            const fee = formatFee(estimatedFee, it.feeCurrency || it.currency)
            const type = it.transferType || it.type || 'Unknown'
            const confidenceRaw = it.confidence ?? it.confidenceScore ?? it.score
            const confidenceDisplay = (confidenceRaw !== undefined && confidenceRaw !== null && confidenceRaw !== '') ? `${Math.round(Number(confidenceRaw) * 100)}%` : 'N/A'
            return (
              <tr key={it.id || it.newsId || it._id} onClick={() => onRowClick && onRowClick(it)}>
                <td>{player}</td>
                <td>{from}</td>
                <td>{to}</td>
                <td>{fee}</td>
                <td>{new Date(it.date || it.publishedAt || '').toLocaleDateString()}</td>
                <td>{confidenceDisplay}</td>
                <td><Badge type={type} /></td>
              </tr>
            )
          })}
        </tbody>
      </table>

      <div className="table-footer">
        <div className="pagination">
          <button onClick={() => setPage(1)} disabled={page === 1}>First</button>
          <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}>Prev</button>
          <span>Page {page} of {pages}</span>
          <button onClick={() => setPage((p) => Math.min(pages, p + 1))} disabled={page === pages}>Next</button>
          <button onClick={() => setPage(pages)} disabled={page === pages}>Last</button>
        </div>
        <div className="table-summary">Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}</div>
      </div>
    </div>
  )
}
