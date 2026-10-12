import { useEffect, useMemo, useState } from 'react'
import { fetchStats } from '../api/transferApi'
import { getApiErrorMessage } from '../api/axios'
import '../styles/Statistics.css'

const TYPE_CONFIG = [
    { key: 'official', label: 'Official Deals', match: ['completed transfer', 'official'] },
    { key: 'rumours', label: 'Rumours', match: ['rumour', 'rumor'] },
    { key: 'contracts', label: 'Contract Renewals', match: ['contract'] },
    { key: 'free', label: 'Free Transfers', match: ['free transfer', 'free'] },
]

export default function Statistics() {
    const [stats, setStats] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [reloadKey, setReloadKey] = useState(0)

    useEffect(() => {
        let mounted = true

        async function load() {
            try {
                setLoading(true)
                setError(null)

                const result = await fetchStats()
                if (!mounted) return
                setStats(result)
            } catch (e) {
                if (mounted) setError(getApiErrorMessage(e, 'Live statistics are temporarily unavailable.'))
            } finally {
                if (mounted) setLoading(false)
            }
        }

        load()

        return () => {
            mounted = false
        }
    }, [reloadKey])

    const computed = useMemo(() => {
        const total = stats?.totalTransfers ?? 0

        const typeCounts = TYPE_CONFIG.map((type) => {
            let count = 0

            if (type.key === 'official') count = stats?.completedTransfers ?? 0
            if (type.key === 'rumours') count = stats?.rumours ?? 0
            if (type.key === 'contracts') count = stats?.contracts ?? 0
            if (type.key === 'free') count = stats?.freeTransfers ?? 0

            return {
                ...type,
                count,
                percent: total ? Math.round((count / total) * 100) : 0
            }
        })

        return {
            total,
            typeCounts,
            avgConfidence: Number(stats?.averageConfidence ?? 0),
            highestConfidence: Number(stats?.highestConfidence ?? 0),
            highConfidenceCount: stats?.highConfidenceCount ?? 0,
            lowConfidenceCount: stats?.lowConfidenceCount ?? 0,
            feesByCurrency: stats?.feesByCurrency ?? [],
            undisclosedCount: stats?.undisclosedCount ?? 0,
            knownFeeCount: stats?.knownFeeCount ?? 0,
            topDestinationClubs: stats?.topDestinationClubs ?? [],
            topSourceClubs: stats?.topSourceClubs ?? [],
            recentActivity: stats?.recentActivity ?? [],
            sourceCoverage: stats?.sourceCoverage ?? []
        }
    }, [stats])

    function pct(value) {
        return `${Math.round(Number(value || 0) * 100)}%`
    }

    function fee(value, currency) {
        const symbols = { GBP: '£', EUR: '€', USD: '$' }
        const amount = Number(value).toFixed(value >= 10 ? 1 : 2)
        return `${symbols[currency] || `${currency} `}${amount}m`
    }

    const dataUnavailable = Boolean(error && !stats)

    const overviewCards = [
        { label: 'Total Transfers', value: dataUnavailable ? null : computed.total },
        { label: 'Official Deals', value: dataUnavailable ? null : computed.typeCounts.find((x) => x.key === 'official')?.count ?? 0 },
        { label: 'Rumours', value: dataUnavailable ? null : computed.typeCounts.find((x) => x.key === 'rumours')?.count ?? 0 },
        { label: 'Contracts', value: dataUnavailable ? null : computed.typeCounts.find((x) => x.key === 'contracts')?.count ?? 0 },
        { label: 'Free Transfers', value: dataUnavailable ? null : computed.typeCounts.find((x) => x.key === 'free')?.count ?? 0 },
    ]

    return (
        <div className="statistics-page">
            <section className="statistics-hero">
                <div>
                    <span className="statistics-kicker">Market overview</span>
                    <h1>Statistics</h1>
                    <p>
                        Review deal status, reported fees, source coverage and club activity.
                    </p>
                </div>

                <div className="statistics-score-card">
                    <span>Extraction confidence</span>
                    <strong>{pct(computed.avgConfidence)}</strong>
                    <small>Average confidence score</small>
                </div>
            </section>

            {error && <div className="statistics-error"><span>{error}</span><button type="button" className="retry-button" onClick={() => setReloadKey((value) => value + 1)}>Retry live data</button></div>}

            <section className="statistics-kpi-grid">
                {overviewCards.map((card) => (
                    <div className="statistics-kpi-card" key={card.label}>
                        <strong>{card.value ?? '—'}</strong>
                        <span>{card.label}</span>
                    </div>
                ))}
            </section>

            {loading && <div className="statistics-state">Loading statistics…</div>}

            {!dataUnavailable && <section className="statistics-grid">
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
                            <span>Record quality</span>
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
                            <h2>Reported Fees</h2>
                        </div>
                    </div>

                    <div className="fee-grid">
                        <div className="fee-list">
                            <div>
                                <span>Known Fee Records</span>
                                <strong>{computed.knownFeeCount}</strong>
                            </div>
                            <div>
                                <span>Undisclosed Fees</span>
                                <strong>{computed.undisclosedCount}</strong>
                            </div>
                        </div>
                        <div className="currency-fee-list">
                            {computed.feesByCurrency.map((group) => (
                                <div key={group.currency}>
                                    <span><b>{group.currency}</b>{group.count} reported fee{group.count === 1 ? '' : 's'}</span>
                                    <strong>{fee(group.total, group.currency)} total</strong>
                                    <small>{fee(group.average, group.currency)} average · {fee(group.highest, group.currency)} highest</small>
                                </div>
                            ))}
                            {computed.feesByCurrency.length === 0 && <div className="statistics-empty">No disclosed fee data.</div>}
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
                            <span>Source Coverage</span>
                            <h2>Reporting Mix</h2>
                        </div>
                    </div>
                    <div className="source-coverage-list">
                        {computed.sourceCoverage.map((source) => (
                            <div key={source.source}>
                                <strong>{source.source}</strong>
                                <span>{source.events} events</span>
                                <small>{source.reports} source reports retained</small>
                            </div>
                        ))}
                        {computed.sourceCoverage.length === 0 && <div className="statistics-empty">No source coverage data.</div>}
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
            </section>}
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
