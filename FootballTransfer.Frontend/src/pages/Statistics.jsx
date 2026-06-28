import { useEffect, useMemo, useState } from 'react'
import { fetchStats, fetchTransfers } from '../api/transferApi'
import '../styles/Statistics.css'

const TYPE_CONFIG = [
    { key: 'official', label: 'Official Deals', match: ['completed transfer', 'official'] },
    { key: 'rumours', label: 'Rumours', match: ['rumour', 'rumor'] },
    { key: 'contracts', label: 'Contract Renewals', match: ['contract'] },
    { key: 'free', label: 'Free Transfers', match: ['free transfer', 'free'] },
]

export default function Statistics() {
    const [stats, setStats] = useState(null)
    const [items, setItems] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    useEffect(() => {
        let mounted = true

        async function load() {
            try {
                setLoading(true)

                const [s, t] = await Promise.all([
                    fetchStats().catch(() => null),
                    fetchTransfers().catch(() => [])
                ])

                if (!mounted) return

                setStats(s)
                setItems(t || [])
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
    }, [])

    const computed = useMemo(() => {
        const total = stats?.totalTransfers ?? items.length

        const typeCounts = TYPE_CONFIG.map((type) => {
            let count = 0

            if (type.key === 'official') count = stats?.completedTransfers ?? 0
            if (type.key === 'rumours') count = stats?.rumours ?? 0
            if (type.key === 'contracts') count = stats?.contracts ?? 0
            if (type.key === 'free') count = stats?.freeTransfers ?? 0

            if (!count && items.length > 0) {
                count = items.filter((it) => {
                    const raw = (it.transferType || it.type || '').toString().toLowerCase()
                    return type.match.some((m) => raw.includes(m))
                }).length
            }

            return {
                ...type,
                count,
                percent: total ? Math.round((count / total) * 100) : 0
            }
        })

        const confidenceValues = items
            .map((it) => Number(it.confidence ?? 0))
            .filter((n) => !Number.isNaN(n) && n > 0)

        const avgConfidence =
            stats?.averageConfidence != null
                ? Number(stats.averageConfidence)
                : confidenceValues.length
                    ? confidenceValues.reduce((a, b) => a + b, 0) / confidenceValues.length
                    : 0

        const highestConfidence = confidenceValues.length
            ? Math.max(...confidenceValues)
            : 0

        const highConfidenceCount = confidenceValues.filter((n) => n >= 0.85).length
        const lowConfidenceCount = confidenceValues.filter((n) => n > 0 && n < 0.7).length

        const knownFeeItems = items.filter((it) => {
            const fee = Number(it.estimatedFee ?? it.fee)
            return !Number.isNaN(fee) && fee > 0
        })

        const totalKnownFees = knownFeeItems.reduce((sum, it) => {
            return sum + Number(it.estimatedFee ?? it.fee)
        }, 0)

        const averageKnownFee = knownFeeItems.length
            ? totalKnownFees / knownFeeItems.length
            : 0

        const highestFee = knownFeeItems.length
            ? Math.max(...knownFeeItems.map((it) => Number(it.estimatedFee ?? it.fee)))
            : 0

        const undisclosedCount = items.filter((it) => {
            const fee = it.estimatedFee ?? it.fee
            return fee === null || fee === undefined || fee === ''
        }).length

        function topClubs(fieldNames) {
            const map = {}

            items.forEach((it) => {
                const club = fieldNames
                    .map((name) => it[name])
                    .find((v) => v && v !== 'Unknown')

                if (!club) return

                map[club] = (map[club] || 0) + 1
            })

            return Object.entries(map)
                .sort((a, b) => b[1] - a[1])
                .slice(0, 5)
                .map(([name, count]) => ({ name, count }))
        }

        const topDestinationClubs = topClubs(['toClub', 'to', 'clubTo'])
        const topSourceClubs = topClubs(['fromClub', 'from', 'clubFrom'])

        const dateMap = {}

        items.forEach((it) => {
            const raw = it.date || it.publishedAt || it.published
            if (!raw) return

            const label = new Date(raw).toLocaleDateString()

            dateMap[label] = (dateMap[label] || 0) + 1
        })

        const recentActivity = Object.entries(dateMap)
            .map(([date, count]) => ({ date, count }))
            .slice(0, 6)

        return {
            total,
            typeCounts,
            avgConfidence,
            highestConfidence,
            highConfidenceCount,
            lowConfidenceCount,
            totalKnownFees,
            averageKnownFee,
            highestFee,
            undisclosedCount,
            knownFeeCount: knownFeeItems.length,
            topDestinationClubs,
            topSourceClubs,
            recentActivity
        }
    }, [items, stats])

    function pct(value) {
        return `${Math.round(Number(value || 0) * 100)}%`
    }

    function fee(value) {
        if (!value) return '£0m'
        return `£${Number(value).toFixed(value >= 10 ? 1 : 2)}m`
    }

    const overviewCards = [
        { label: 'Total Transfers', value: computed.total },
        { label: 'Official Deals', value: computed.typeCounts.find((x) => x.key === 'official')?.count ?? 0 },
        { label: 'Rumours', value: computed.typeCounts.find((x) => x.key === 'rumours')?.count ?? 0 },
        { label: 'Contracts', value: computed.typeCounts.find((x) => x.key === 'contracts')?.count ?? 0 },
        { label: 'Free Transfers', value: computed.typeCounts.find((x) => x.key === 'free')?.count ?? 0 },
    ]

    return (
        <div className="statistics-page">
            <section className="statistics-hero">
                <div>
                    <span className="statistics-kicker">Market Intelligence Dashboard</span>
                    <h1>Statistics</h1>
                    <p>
                        Understand the transfer market through AI-extracted records,
                        confidence scoring, fee intelligence and club activity.
                    </p>
                </div>

                <div className="statistics-score-card">
                    <span>AI Confidence</span>
                    <strong>{pct(computed.avgConfidence)}</strong>
                    <small>Average confidence score</small>
                </div>
            </section>

            {error && <div className="statistics-error">{error}</div>}

            <section className="statistics-kpi-grid">
                {overviewCards.map((card) => (
                    <div className="statistics-kpi-card" key={card.label}>
                        <strong>{card.value}</strong>
                        <span>{card.label}</span>
                    </div>
                ))}
            </section>

            {loading && <div className="statistics-state">Loading statistics…</div>}

            <section className="statistics-grid">
                <div className="statistics-panel">
                    <div className="panel-head">
                        <div>
                            <span>Breakdown</span>
                            <h2>Transfer Type</h2>
                        </div>
                    </div>

                    <div className="breakdown-list">
                        {computed.typeCounts.map((item) => (
                            <div className="breakdown-row" key={item.key}>
                                <div className="breakdown-top">
                                    <strong>{item.label}</strong>
                                    <span>{item.count}</span>
                                </div>

                                <div className="breakdown-track">
                                    <div style={{ width: `${item.percent}%` }} />
                                </div>

                                <small>{item.percent}% of total transfers</small>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="statistics-panel">
                    <div className="panel-head">
                        <div>
                            <span>AI Quality</span>
                            <h2>Confidence Overview</h2>
                        </div>
                    </div>

                    <div className="quality-grid">
                        <div>
                            <strong>{pct(computed.avgConfidence)}</strong>
                            <span>Average Confidence</span>
                        </div>

                        <div>
                            <strong>{pct(computed.highestConfidence)}</strong>
                            <span>Highest Confidence</span>
                        </div>

                        <div>
                            <strong>{computed.highConfidenceCount}</strong>
                            <span>High Confidence Records</span>
                        </div>

                        <div>
                            <strong>{computed.lowConfidenceCount}</strong>
                            <span>Low Confidence Records</span>
                        </div>
                    </div>
                </div>

                <div className="statistics-panel">
                    <div className="panel-head">
                        <div>
                            <span>Financial View</span>
                            <h2>Fee Intelligence</h2>
                        </div>
                    </div>

                    <div className="fee-grid">
                        <div className="fee-main">
                            <span>Total Known Fees</span>
                            <strong>{fee(computed.totalKnownFees)}</strong>
                        </div>

                        <div className="fee-list">
                            <div>
                                <span>Known Fee Records</span>
                                <strong>{computed.knownFeeCount}</strong>
                            </div>

                            <div>
                                <span>Average Known Fee</span>
                                <strong>{fee(computed.averageKnownFee)}</strong>
                            </div>

                            <div>
                                <span>Highest Fee</span>
                                <strong>{fee(computed.highestFee)}</strong>
                            </div>

                            <div>
                                <span>Undisclosed Fees</span>
                                <strong>{computed.undisclosedCount}</strong>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="statistics-panel">
                    <div className="panel-head">
                        <div>
                            <span>Club Activity</span>
                            <h2>Most Active Clubs</h2>
                        </div>
                    </div>

                    <div className="club-columns">
                        <ClubList title="Destination Clubs" items={computed.topDestinationClubs} />
                        <ClubList title="Source Clubs" items={computed.topSourceClubs} />
                    </div>
                </div>

                <div className="statistics-panel statistics-wide">
                    <div className="panel-head">
                        <div>
                            <span>Timeline</span>
                            <h2>Recent Activity</h2>
                        </div>
                    </div>

                    <div className="activity-list">
                        {computed.recentActivity.map((item) => (
                            <div className="activity-row" key={item.date}>
                                <strong>{item.date}</strong>
                                <span>{item.count} records</span>
                                <div>
                                    <i style={{ width: `${Math.min(item.count * 18, 100)}%` }} />
                                </div>
                            </div>
                        ))}

                        {computed.recentActivity.length === 0 && (
                            <div className="statistics-empty">No recent activity data.</div>
                        )}
                    </div>
                </div>
            </section>
        </div>
    )
}

function ClubList({ title, items }) {
    return (
        <div className="club-list">
            <h3>{title}</h3>

            {items.length === 0 && (
                <div className="statistics-empty">No club data.</div>
            )}

            {items.map((club, index) => (
                <div className="club-row" key={club.name}>
                    <span>{index + 1}</span>
                    <strong>{club.name}</strong>
                    <b>{club.count}</b>
                </div>
            ))}
        </div>
    )
}