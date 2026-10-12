import { useEffect, useState } from 'react'
import { fetchTransfers } from '../api/transferApi'
import { getApiErrorMessage } from '../api/axios'
import { Link, useSearchParams } from 'react-router-dom'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import '../styles/Transfers.css'

const PAGE_SIZE = 25
const CATEGORY_MAP = {
    official: { title: 'Official Deals', desc: 'Confirmed and completed transfer events' },
    rumours: { title: 'Rumours', desc: 'Unconfirmed transfer market signals' },
    contracts: { title: 'Contract Renewals', desc: 'Player contract extensions and renewals' },
    free: { title: 'Free Transfers', desc: 'Zero-fee player movements' }
}

export default function Transfers() {
    const [items, setItems] = useState([])
    const [totalCount, setTotalCount] = useState(0)
    const [pageCount, setPageCount] = useState(0)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [sortBy, setSortBy] = useState('newest')
    const [page, setPage] = useState(1)
    const [reloadKey, setReloadKey] = useState(0)
    const [params, setParams] = useSearchParams()

    const search = params.get('search') || ''
    const category = params.get('cat') || 'official'
    const cfg = CATEGORY_MAP[category] || CATEGORY_MAP.official
    const isGlobalSearch = Boolean(search && !params.get('cat'))

    useEffect(() => {
        let mounted = true
        const timer = setTimeout(async () => {
            try {
                setLoading(true)
                setError(null)
                const response = await fetchTransfers({
                    page,
                    pageSize: PAGE_SIZE,
                    category: isGlobalSearch ? undefined : category,
                    search: search || undefined,
                    sort: sortBy
                })
                if (!mounted) return
                setItems(response.items || [])
                setTotalCount(response.totalCount || 0)
                setPageCount(response.totalPages || 0)
            } catch (requestError) {
                if (mounted) setError(getApiErrorMessage(requestError, 'Transfer records are temporarily unavailable.'))
            } finally {
                if (mounted) setLoading(false)
            }
        }, search ? 250 : 0)

        return () => {
            mounted = false
            clearTimeout(timer)
        }
    }, [category, isGlobalSearch, page, reloadKey, search, sortBy])

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
        const value = (type || '').toLowerCase()
        if (value.includes('completed') || value.includes('official')) return { label: 'Official', css: 'transfer-badge-official' }
        if (value.includes('rumour') || value.includes('rumor')) return { label: 'Rumour', css: 'transfer-badge-rumour' }
        if (value.includes('contract')) return { label: 'Contract', css: 'transfer-badge-contract' }
        if (value.includes('free')) return { label: 'Free', css: 'transfer-badge-free' }
        return { label: 'Unknown', css: 'transfer-badge-neutral' }
    }

    function handleCategoryChange(nextCategory) {
        setParams({ cat: nextCategory })
        setSortBy('newest')
        setPage(1)
    }

    function handleSearchChange(event) {
        const value = event.target.value
        setPage(1)
        setParams(value.trim() ? { cat: category, search: value } : { cat: category })
    }

    function clearSearch() {
        setParams({ cat: category })
        setPage(1)
    }

    return (
        <div className="transfers-page">
            <section className="transfers-hero">
                <div>
                    <span className="transfers-kicker">{isGlobalSearch ? 'Search results' : 'Transfer market'}</span>
                    <h1>Transfers</h1>
                    <p>Browse confirmed deals, reported moves, contract updates and free transfers.</p>
                </div>
                <div className="transfers-summary">
                    <strong>{loading ? '—' : totalCount}</strong>
                    <span>{isGlobalSearch ? 'Search Results' : cfg.title}</span>
                </div>
            </section>

            <section className="transfer-controls">
                <div className="filter-tabs">
                    {Object.keys(CATEGORY_MAP).map((key) => (
                        <button key={key} className={`filter-tab ${key === category && !isGlobalSearch ? 'active' : ''}`} onClick={() => handleCategoryChange(key)}>
                            <strong>{CATEGORY_MAP[key].title}</strong>
                            <span>{CATEGORY_MAP[key].desc}</span>
                        </button>
                    ))}
                </div>
                <div className="transfer-search">
                    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M21 21L16.5 16.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                        <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="2" />
                    </svg>
                    <input placeholder={isGlobalSearch ? 'Search all transfer events...' : `Search ${cfg.title.toLowerCase()}...`} value={search} onChange={handleSearchChange} />
                    {search && <button type="button" className="clear-search" onClick={clearSearch}>Clear</button>}
                </div>
            </section>

            {error && <div className="transfer-error"><span>{error}</span><button type="button" className="retry-button" onClick={() => setReloadKey((value) => value + 1)}>Retry live data</button></div>}

            <section className="transfer-feed">
                <div className="feed-header">
                    <div>
                        <span>{isGlobalSearch ? 'Search Results' : 'Current Category'}</span>
                        <h2>{isGlobalSearch ? `Search: ${search}` : cfg.title}</h2>
                    </div>
                    <div className="feed-actions">
                        <p>{totalCount} events found</p>
                        <select value={sortBy} onChange={(event) => { setSortBy(event.target.value); setPage(1) }} aria-label="Sort transfer events">
                            <option value="newest">Newest first</option>
                            <option value="oldest">Oldest first</option>
                            <option value="confidence">Highest confidence</option>
                            <option value="fee">Highest fee</option>
                        </select>
                    </div>
                </div>

                <div className="transfer-table">
                    <div className="transfer-table-head"><span>Player</span><span>Route</span><span>Type</span><span>Fee</span><span>Confidence</span><span>Date</span></div>
                    {!error && items.map((item) => {
                        const id = item.id
                        const player = item.playerName || 'Player not reported'
                        const from = item.fromClub || 'Previous club not reported'
                        const to = item.toClub || 'Destination not reported'
                        const badge = getBadge(item.transferType)
                        const confidence = Math.round(Number(item.confidence ?? 0) * 100)
                        return (
                            <Link to={`/transfer/${id}`} key={id} className="transfer-row">
                                <strong>{player}<small className="source-count">{item.sourceCount || 1} source{item.sourceCount === 1 ? '' : 's'}</small></strong>
                                <span className="transfer-route">{from} <b>→</b> {to}</span>
                                <span className={`transfer-badge ${badge.css}`}>{badge.label}</span>
                                <span className="transfer-fee">{formatFee(item.estimatedFee, item.feeCurrency)}</span>
                                <span className="transfer-confidence"><b>{confidence}%</b><i><em style={{ width: `${confidence}%` }} /></i></span>
                                <span className="transfer-date">{item.publishedAt ? new Date(item.publishedAt).toLocaleDateString() : '—'}</span>
                            </Link>
                        )
                    })}
                    {loading && <div className="transfer-state">Loading transfer events…</div>}
                    {!loading && !error && items.length === 0 && <div className="transfer-state">No matching transfer events found.</div>}
                </div>

                {!loading && !error && pageCount > 1 && (
                    <nav className="transfer-pagination" aria-label="Transfer results pages">
                        <span>{(page - 1) * PAGE_SIZE + 1}-{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}</span>
                        <div>
                            <button type="button" onClick={() => setPage((value) => Math.max(1, value - 1))} disabled={page === 1} aria-label="Previous page" title="Previous page"><ChevronLeft size={18} /></button>
                            <strong>Page {page} of {pageCount}</strong>
                            <button type="button" onClick={() => setPage((value) => Math.min(pageCount, value + 1))} disabled={page === pageCount} aria-label="Next page" title="Next page"><ChevronRight size={18} /></button>
                        </div>
                    </nav>
                )}
            </section>
        </div>
    )
}
