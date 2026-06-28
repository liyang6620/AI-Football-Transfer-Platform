import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchTransfer, fetchTransfers } from '../api/transferApi'
import '../styles/TransferDetail.css'

export default function TransferDetail() {
    const { id } = useParams()
    const [item, setItem] = useState(null)
    const [timeline, setTimeline] = useState([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)

    useEffect(() => {
        let mounted = true
        if (!id) return

        async function load() {
            try {
                setLoading(true)

                const [d, all] = await Promise.all([
                    fetchTransfer(id),
                    fetchTransfers().catch(() => [])
                ])

                if (!mounted) return

                setItem(d)

                const playerName = getPlayerName(d)

                const matched = (all || [])
                    .filter((x) => getPlayerName(x).toLowerCase() === playerName.toLowerCase())
                    .sort((a, b) => {
                        const da = new Date(a.date || a.publishedAt || a.published || a.createdAt || 0).getTime()
                        const db = new Date(b.date || b.publishedAt || b.published || b.createdAt || 0).getTime()
                        return db - da
                    })

                setTimeline(matched)
            } catch (e) {
                if (mounted) setError(e.message)
            } finally {
                if (mounted) setLoading(false)
            }
        }

        load()

        return () => {
            mounted = false
        }
    }, [id])

    const detail = useMemo(() => {
        if (!item) return null

        const player = getPlayerName(item)
        const from = item.fromClub || item.from || item.clubFrom || '—'
        const to = item.toClub || item.to || item.clubTo || '—'

        const estimatedFee =
            item.estimatedFee ??
            item.fee ??
            item.transferFee ??
            item.amount ??
            null

        const currency = item.feeCurrency || item.currency || ''
        const type = item.transferType || item.type || 'Unknown'

        const confidenceRaw =
            item.confidence ??
            item.confidenceScore ??
            item.score ??
            null

        const confidencePercent =
            confidenceRaw !== null && confidenceRaw !== undefined && confidenceRaw !== ''
                ? Math.round(Number(confidenceRaw) * 100)
                : null

        const title = item.title || item.headline || ''
        const date = item.date || item.publishedAt || item.published || item.createdAt || ''

        const summary =
            item.aiSummary ||
            item.summary ||
            item.content ||
            'No summary available.'

        const content =
            item.content ||
            item.body ||
            item.summary ||
            ''

        const source = item.source || item.sourceName || 'Unknown source'
        const url = item.url || item.sourceUrl || item.link || ''

        return {
            player,
            from,
            to,
            estimatedFee,
            currency,
            type,
            confidencePercent,
            title,
            date,
            summary,
            content,
            source,
            url
        }
    }, [item])

    function getPlayerName(record) {
        if (!record) return ''
        return record.player || record.playerName || record.title || 'Unknown'
    }

    function formatFee(estimatedFee, feeCurrency) {
        if (estimatedFee === null || estimatedFee === undefined) return 'Undisclosed'

        const rounded =
            typeof estimatedFee === 'number'
                ? estimatedFee
                : parseFloat(String(estimatedFee))

        if (Number.isNaN(rounded)) return 'Undisclosed'
        if (rounded === 0) return 'Free'

        const m = `${rounded}m`
        const cur = (feeCurrency || '').toString().toUpperCase()

        if (cur === 'GBP' || cur === '£') return `£${m}`
        if (cur === 'EUR' || cur === '€') return `€${m}`
        if (cur === 'USD' || cur === '$') return `$${m}`

        return `${rounded}${cur ? ` ${cur}` : ''}m`
    }

    function badgeClass(type) {
        const t = (type || '').toLowerCase()

        if (t.includes('completed') || t.includes('official')) return 'detail-badge official'
        if (t.includes('rumour') || t.includes('rumor')) return 'detail-badge rumour'
        if (t.includes('contract')) return 'detail-badge contract'
        if (t.includes('free')) return 'detail-badge free'

        return 'detail-badge neutral'
    }

    function getTimelineSummary(event) {
        return (
            event.aiSummary ||
            event.summary ||
            event.content ||
            event.description ||
            event.title ||
            `${getPlayerName(event)} transfer update recorded by the system.`
        )
    }

    if (loading) {
        return (
            <div className="detail-page">
                <div className="detail-state">Loading transfer detail…</div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="detail-page">
                <div className="detail-error">{error}</div>
            </div>
        )
    }

    if (!detail) {
        return (
            <div className="detail-page">
                <div className="detail-state">Transfer not found.</div>
            </div>
        )
    }

    return (
        <div className="detail-page">
            <Link to="/transfers" className="back-link">
                ← Back to transfers
            </Link>

            <section className="detail-hero">
                <div className="detail-main">
                    <span className="detail-kicker">Transfer Record</span>
                    <h1>{detail.player}</h1>

                    <div className="detail-route">
                        <span>{detail.from}</span>
                        <b>→</b>
                        <span>{detail.to}</span>
                    </div>

                    <div className="detail-meta-row">
                        <span className={badgeClass(detail.type)}>{detail.type}</span>
                        <span>{formatFee(detail.estimatedFee, detail.currency)}</span>
                        <span>{detail.date ? new Date(detail.date).toLocaleString() : 'No date'}</span>
                    </div>
                </div>

                <div className="confidence-card">
                    <span>AI Confidence</span>

                    <strong>
                        {detail.confidencePercent !== null ? `${detail.confidencePercent}%` : 'N/A'}
                    </strong>

                    <div className="confidence-track">
                        <div style={{ width: `${detail.confidencePercent ?? 0}%` }} />
                    </div>

                    <p>Reliability score generated from AI extraction.</p>
                </div>
            </section>

            <section className="detail-content-grid">
                <main className="story-column">
                    <section className="detail-panel story-card">
                        <span className="panel-kicker">AI Summary</span>
                        <h2>Transfer Intelligence Summary</h2>
                        <p>{detail.summary}</p>
                    </section>

                    {detail.title && (
                        <section className="detail-panel article-card">
                            <span className="panel-kicker">Original Article</span>
                            <h2>{detail.title}</h2>

                            {detail.content && detail.content !== detail.summary && (
                                <p>{detail.content}</p>
                            )}

                            {detail.url && (
                                <a className="original-link" href={detail.url} target="_blank" rel="noreferrer">
                                    Read Original Article
                                </a>
                            )}
                        </section>
                    )}

                    <section className="detail-panel timeline-panel">
                        <span className="panel-kicker">Player Timeline</span>
                        <h2>{detail.player} Transfer Timeline</h2>

                        <div className="timeline-list">
                            {timeline.map((event, index) => {
                                const eventId = event.id || event._id || event.newsId || index
                                const eventType = event.transferType || event.type || 'Unknown'
                                const eventFrom = event.fromClub || event.from || event.clubFrom || '—'
                                const eventTo = event.toClub || event.to || event.clubTo || '—'
                                const eventDate = event.date || event.publishedAt || event.published || event.createdAt
                                const eventConfidenceRaw = event.confidence ?? event.confidenceScore ?? event.score ?? null
                                const eventConfidence =
                                    eventConfidenceRaw !== null &&
                                        eventConfidenceRaw !== undefined &&
                                        eventConfidenceRaw !== ''
                                        ? Math.round(Number(eventConfidenceRaw) * 100)
                                        : null

                                return (
                                    <div className="timeline-item" key={eventId}>
                                        <div className="timeline-date">
                                            {eventDate ? new Date(eventDate).getFullYear() : '—'}
                                        </div>

                                        <div className="timeline-content">
                                            <div className="timeline-top">
                                                <strong>{eventType}</strong>
                                                <span>{eventDate ? new Date(eventDate).toLocaleDateString() : 'No date'}</span>
                                            </div>

                                            <p className="timeline-route">
                                                {eventFrom} <b>→</b> {eventTo}
                                            </p>

                                            <p className="timeline-summary">{getTimelineSummary(event)}</p>

                                            <div className="timeline-meta">
                                                <span>{formatFee(event.estimatedFee ?? event.fee, event.feeCurrency)}</span>
                                                <span>{eventConfidence !== null ? `${eventConfidence}% confidence` : 'N/A confidence'}</span>
                                                <Link to={`/transfer/${eventId}`}>View record</Link>
                                            </div>
                                        </div>
                                    </div>
                                )
                            })}

                            {timeline.length === 0 && (
                                <div className="detail-state">
                                    No timeline records available for this player.
                                </div>
                            )}
                        </div>
                    </section>
                </main>

                <aside className="snapshot-column">
                    <section className="detail-panel snapshot-card">
                        <span className="panel-kicker">Transfer Snapshot</span>
                        <h2>Key Facts</h2>

                        <div className="snapshot-grid">
                            <div>
                                <span>Player</span>
                                <strong>{detail.player}</strong>
                            </div>

                            <div>
                                <span>From</span>
                                <strong>{detail.from}</strong>
                            </div>

                            <div>
                                <span>To</span>
                                <strong>{detail.to}</strong>
                            </div>

                            <div>
                                <span>Type</span>
                                <strong>{detail.type}</strong>
                            </div>

                            <div>
                                <span>Fee</span>
                                <strong>{formatFee(detail.estimatedFee, detail.currency)}</strong>
                            </div>

                            <div>
                                <span>Confidence</span>
                                <strong>
                                    {detail.confidencePercent !== null ? `${detail.confidencePercent}%` : 'N/A'}
                                </strong>
                            </div>

                            <div>
                                <span>Source</span>
                                <strong>{detail.source}</strong>
                            </div>

                            <div>
                                <span>Published</span>
                                <strong>{detail.date ? new Date(detail.date).toLocaleDateString() : 'No date'}</strong>
                            </div>
                        </div>
                    </section>
                </aside>
            </section>
        </div>
    )
}