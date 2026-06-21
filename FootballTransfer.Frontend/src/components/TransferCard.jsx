import Badge from './Badge'

export default function TransferCard({ item, onClick }) {
  const player = item.player || item.playerName || item.title || 'Unknown'
  const from = item.fromClub || item.from || item.clubFrom || item.source || '—'
  const to = item.toClub || item.to || item.clubTo || item.destination || '—'
  const fee = item.fee || item.transferFee || item.amount || 'Undisclosed'
  const type = item.transferType || item.type || 'Unknown'
  const confidence = item.confidence ?? item.confidenceScore ?? item.score ?? ''

  return (
    <div className="transfer-card" onClick={() => onClick && onClick(item)}>
      <div className="tc-row">
        <div className="tc-player">{player}</div>
        <div className="tc-confidence">{confidence !== '' ? `${confidence}%` : 'N/A'}</div>
      </div>
      <div className="tc-clubs">
        <div className="tc-from">{from}</div>
        <div className="tc-arrow">→</div>
        <div className="tc-to">{to}</div>
      </div>
      <div className="tc-footer">
        <div className="tc-fee">{fee}</div>
        <Badge type={type} />
      </div>
    </div>
  )
}
