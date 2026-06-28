import Badge from './Badge'
import ProgressBar from './ProgressBar'

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
  // fallback show value and currency code
  return `${rounded}${cur ? ' ' + cur : ''}m`
}

export default function TransferCard({ item, onClick }) {
  const player = item.player || item.playerName || item.title || 'Unknown'
  const from = item.fromClub || item.from || item.clubFrom || item.source || '—'
  const to = item.toClub || item.to || item.clubTo || item.destination || '—'
  const estimatedFee = item.estimatedFee ?? item.fee ?? item.transferFee ?? item.amount ?? null
  const type = item.transferType || item.type || 'Unknown'
  const feeDisplay = (type && type.toString().toLowerCase().includes('free')) ? 'Free Transfer' : formatFee(estimatedFee, item.feeCurrency || item.currency)
  const confidenceRaw = item.confidence ?? item.confidenceScore ?? item.score ?? 0
  const confidencePercent = Number(confidenceRaw) ? Math.round(Number(confidenceRaw) * 100) : 0
  const date = item.date || item.publishedAt || item.published || ''
  const title = item.title || item.headline || ''

  return (
    <div className="latest-row-card" onClick={() => onClick && onClick(item)}>
      <div className="latest-left">
        <h3 className="tc-player">{player}</h3>
        <div className="tc-clubs">
          <span className="tc-from">{from}</span>
          <span className="tc-arrow">→</span>
          <span className="tc-to">{to}</span>
        </div>
        <div className="tc-title">{title}</div>
      </div>
      <div className="latest-right">
        <Badge type={type} />
        <div className="tc-fee">{feeDisplay}</div>
        <div className="confidence-inline">
          <ProgressBar value={confidencePercent} />
          <span className="confidence-text">{confidencePercent}%</span>
        </div>
        <div className="tc-date">{date ? new Date(date).toLocaleDateString() : ''}</div>
      </div>
    </div>
  )
}
