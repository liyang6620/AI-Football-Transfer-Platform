import { useEffect, useMemo, useState } from 'react'
import { fetchTransfers } from '../api/transferApi'
import { Link, useSearchParams } from 'react-router-dom'
import '../styles/Transfers.css'

const CATEGORY_MAP = {
    official: {
        title: 'Official Deals',
        match: 'Completed Transfer',
        desc: 'Confirmed and completed transfer records'
    },
    rumours: {
        title: 'Rumours',
        match: 'Rumour',
        desc: 'Unconfirmed transfer market signals'
    },
    contracts: {
        title: 'Contract Renewals',
        match: 'Contract',
        desc: 'Player contract extensions and renewals'
    },
    free: {
        title: 'Free Transfers',
        match: 'Free Transfer',
        desc: 'Zero-fee player movements'
    }
}

export default function Transfers() {
    const [items, setItems] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [sortBy, setSortBy] = useState('newest')

    const [params, setParams] = useSearchParams()

    const searchFromUrl = params.get('search') || ''
    const search = searchFromUrl

    const category = params.get('cat') || 'official'
    const cfg = CATEGORY_MAP[category] || CATEGORY_MAP.official

    const isGlobalSearch = Boolean(searchFromUrl && !params.get('cat'))

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

        return () => {
            mounted = false
        }
    }, [])

    const filtered = useMemo(() => {
        const base = isGlobalSearch
            ? items
            : items.filter((it) => {
                const raw = ((it.transferType || it.type || '') + '').toLowerCase()

                if (category === 'official') {
                    return raw.includes('completed') || raw.includes('official')
                }

                if (category === 'rumours') {
                    return raw.includes('rumour') || raw.includes('rumor')
                }

                if (category === 'contracts') {
                    return raw.includes('contract')
                }

                if (category === 'free') {
                    return raw.includes('free')
                }

                return true
            })

        const q = search.trim().toLowerCase()

        const searched = !q
            ? base
            : base.filter((it) => {
                const player = (it.player || it.playerName || it.title || '').toString().toLowerCase()
                const from = (it.fromClub || it.from || it.clubFrom || '').toString().toLowerCase()
                const to = (it.toClub || it.to || it.clubTo || '').toString().toLowerCase()
                const title = (it.title || it.headline || '').toString().toLowerCase()
                const type = (it.transferType || it.type || '').toString().toLowerCase()
                const fee = (it.estimatedFee ?? it.fee ?? '').toString().toLowerCase()

                return (
                    player.includes(q) ||
                    from.includes(q) ||
                    to.includes(q) ||
                    title.includes(q) ||
                    type.includes(q) ||
                    fee.includes(q)
                )
            })

        return searched.slice().sort((a, b) => {
            if (sortBy === 'confidence') {
                return Number(b.confidence ?? 0) - Number(a.confidence ?? 0)
            }

            if (sortBy === 'fee') {
                return Number(b.estimatedFee ?? b.fee ?? 0) - Number(a.estimatedFee ?? a.fee ?? 0)
            }

            if (sortBy === 'oldest') {
                return (
                    new Date(a.date || a.publishedAt || a.published || 0).getTime() -
                    new Date(b.date || b.publishedAt || b.published || 0).getTime()
                )
            }

            return (
                new Date(b.date || b.publishedAt || b.published || 0).getTime() -
                new Date(a.date || a.publishedAt || a.published || 0).getTime()
            )
        })
    }, [items, category, search, isGlobalSearch, sortBy])

    function formatFee(fee, currency) {
        if (fee === 0) return 'Free'
        if (!fee) return 'Undisclosed'

        const cur = (currency || '').toUpperCase()

        if (cur === 'GBP' || cur === '£') return `£${fee}m`
        if (cur === 'EUR' || cur === '€') return `€${fee}m`
        if (cur === 'USD' || cur === '$') return `$${fee}m`

        return `${fee}m`
    }

    function getBadge(type) {
        const t = (type || '').toLowerCase()

        if (t.includes('completed') || t.includes('official')) {
            return { label: 'Official', css: 'transfer-badge-official' }
        }

        if (t.includes('rumour') || t.includes('rumor')) {
            return { label: 'Rumour', css: 'transfer-badge-rumour' }
        }

        if (t.includes('contract')) {
            return { label: 'Contract', css: 'transfer-badge-contract' }
        }

        if (t.includes('free')) {
            return { label: 'Free', css: 'transfer-badge-free' }
        }

        return { label: 'Unknown', css: 'transfer-badge-neutral' }
    }

    function handleCategoryChange(k) {
        setParams({ cat: k })
        setSortBy('newest')
    }

    function handleSearchChange(e) {
        const value = e.target.value

        if (value.trim()) {
            setParams({ cat: category, search: value })
        } else {
            setParams({ cat: category })
        }
    }

    function clearSearch() {
        setParams({ cat: category })
    }

    return (
        <div className="transfers-page">
            <section className="transfers-hero">
                <div>
                    <span className="transfers-kicker">
                        {isGlobalSearch ? 'Global Transfer Search' : 'Transfer Intelligence Feed'}
                    </span>

                    <h1>Transfers</h1>

                    <p>
                        Browse AI-extracted football transfer intelligence across official deals,
                        rumours, renewals and free transfers.
                    </p>
                </div>

                <div className="transfers-summary">
                    <strong>{filtered.length}</strong>
                    <span>{isGlobalSearch ? 'Search Results' : cfg.title}</span>
                </div>
            </section>

            <section className="transfer-controls">
                <div className="filter-tabs">
                    {Object.keys(CATEGORY_MAP).map((k) => (
                        <button
                            key={k}
                            className={`filter-tab ${k === category && !isGlobalSearch ? 'active' : ''}`}
                            onClick={() => handleCategoryChange(k)}
                        >
                            <strong>{CATEGORY_MAP[k].title}</strong>
                            <span>{CATEGORY_MAP[k].desc}</span>
                        </button>
                    ))}
                </div>

                <div className="transfer-search">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path
                            d="M21 21L16.5 16.5"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                        />

                        <circle
                            cx="11"
                            cy="11"
                            r="7"
                            stroke="currentColor"
                            strokeWidth="2"
                        />
                    </svg>

                    <input
                        placeholder={
                            isGlobalSearch
                                ? 'Search all transfer records...'
                                : `Search ${cfg.title.toLowerCase()}...`
                        }
                        value={search}
                        onChange={handleSearchChange}
                    />

                    {search && (
                        <button type="button" className="clear-search" onClick={clearSearch}>
                            Clear
                        </button>
                    )}
                </div>
            </section>

            {error && <div className="transfer-error">{error}</div>}

            <section className="transfer-feed">
                <div className="feed-header">
                    <div>
                        <span>{isGlobalSearch ? 'Search Results' : 'Current Category'}</span>
                        <h2>{isGlobalSearch ? `Search: ${search}` : cfg.title}</h2>
                    </div>

                    <div className="feed-actions">
                        <p>{filtered.length} records found</p>

                        <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                            <option value="newest">Newest first</option>
                            <option value="oldest">Oldest first</option>
                            <option value="confidence">Highest confidence</option>
                            <option value="fee">Highest fee</option>
                        </select>
                    </div>
                </div>

                <div className="transfer-table">
                    <div className="transfer-table-head">
                        <span>Player</span>
                        <span>Route</span>
                        <span>Type</span>
                        <span>Fee</span>
                        <span>Confidence</span>
                        <span>Date</span>
                    </div>

                    {filtered.map((it) => {
                        const id = it.id || it._id || it.newsId
                        const player = it.player || it.playerName || it.title || 'Unknown Player'
                        const from = it.fromClub || it.from || it.clubFrom || 'Unknown'
                        const to = it.toClub || it.to || it.clubTo || 'Unknown'
                        const fee = formatFee(it.estimatedFee ?? it.fee, it.feeCurrency)
                        const badge = getBadge(it.transferType || it.type)
                        const confPct = Math.round(Number(it.confidence ?? 0) * 100)
                        const date = it.date || it.publishedAt || it.published

                        return (
                            <Link to={`/transfer/${id}`} key={id} className="transfer-row">
                                <strong>{player}</strong>

                                <span className="transfer-route">
                                    {from} <b>→</b> {to}
                                </span>

                                <span className={`transfer-badge ${badge.css}`}>
                                    {badge.label}
                                </span>

                                <span className="transfer-fee">
                                    {fee}
                                </span>

                                <span className="transfer-confidence">
                                    <b>{confPct}%</b>
                                    <i>
                                        <em style={{ width: `${confPct}%` }} />
                                    </i>
                                </span>

                                <span className="transfer-date">
                                    {date ? new Date(date).toLocaleDateString() : '—'}
                                </span>
                            </Link>
                        )
                    })}

                    {loading && (
                        <div className="transfer-state">
                            Loading transfers…
                        </div>
                    )}

                    {!loading && filtered.length === 0 && (
                        <div className="transfer-state">
                            No matching transfer records found.
                        </div>
                    )}
                </div>
            </section>
        </div>
    )
}