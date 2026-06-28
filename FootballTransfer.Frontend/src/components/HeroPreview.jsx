import { Link } from 'react-router-dom'

export default function HeroPreview({ items = [], formatFee, getTransferBadge }) {
    const previewItems = items.slice(0, 3)

    return (
        <div className="hero-preview">
            <div className="preview-header">
                <div>
                    <span>Live Feed</span>
                    <h3>Transfer Intelligence</h3>
                </div>
                <div className="live-dot">Live</div>
            </div>

            <div className="preview-list">
                {previewItems.map((it, index) => {
                    const id = it.id || it._id || it.newsId || index
                    const player = it.player || it.playerName || it.title || 'Unknown Player'
                    const from = it.fromClub || it.from || it.clubFrom || 'Unknown'
                    const to = it.toClub || it.to || it.clubTo || 'Unknown'
                    const fee = formatFee(it.estimatedFee ?? it.fee, it.feeCurrency)
                    const badge = getTransferBadge(it.transferType || it.type)
                    const confPct = Math.round(Number(it.confidence ?? 0) * 100)

                    return (
                        <Link key={id} to={`/transfer/${id}`} className="preview-card">
                            <div className="preview-main">
                                <strong>{player}</strong>
                                <span>{from} → {to}</span>
                            </div>

                            <div className="preview-meta">
                                <span className={`mini-badge ${badge.css}`}>{badge.label}</span>
                                <b>{fee}</b>
                            </div>

                            <div className="preview-confidence">
                                <span>{confPct}% confidence</span>
                                <div className="mini-track">
                                    <div style={{ width: `${confPct}%` }} />
                                </div>
                            </div>
                        </Link>
                    )
                })}

                {previewItems.length === 0 && (
                    <div className="preview-empty">
                        No transfer data yet
                    </div>
                )}
            </div>
        </div>
    )
}