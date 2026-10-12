import { useEffect, useMemo, useState } from 'react'
import { fetchStats, fetchTransfers } from '../api/transferApi'
import { getApiErrorMessage } from '../api/axios'
import { ArrowRight, BarChart3, ChevronRight } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import HeroPreview from '../components/HeroPreview'
import '../styles/Home.css'
import heroImg from '../img/hero.webp'

export default function Home() {
    const [stats, setStats] = useState(null)
    const [items, setItems] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [reloadKey, setReloadKey] = useState(0)
    const navigate = useNavigate()

    useEffect(() => {
        let mounted = true
        async function load() {
            try {
                setLoading(true)
                setError(null)
                const [statsResult, transfersResult] = await Promise.allSettled([
                    fetchStats(),
                    fetchTransfers()
                ])
                if (!mounted) return
                if (statsResult.status === 'rejected' && transfersResult.status === 'rejected') {
                    throw transfersResult.reason
                }
                setStats(statsResult.status === 'fulfilled' ? statsResult.value : null)
                setItems(transfersResult.status === 'fulfilled' ? transfersResult.value || [] : [])
                if (statsResult.status === 'rejected' || transfersResult.status === 'rejected') {
                    setError('Some live market data could not be loaded.')
                }
            } catch (e) {
                if (mounted) setError(getApiErrorMessage(e, 'Live market data is temporarily unavailable.'))
            } finally {
                if (mounted) setLoading(false)
            }
        }
        load()
        return () => { mounted = false }
    }, [reloadKey])

    const dataUnavailable = Boolean(error && !stats && items.length === 0)

    const latest = useMemo(() => items.slice().sort((a, b) => {
        const da = new Date(a.date || a.publishedAt || a.published || 0).getTime()
        const db = new Date(b.date || b.publishedAt || b.published || 0).getTime()
        return db - da
    }).slice(0, 5), [items])

    function formatFee(fee, currency) {
        if (fee === 0) return 'Free'
        if (!fee) return 'Undisclosed'
        const cur = (currency || '').toUpperCase()
        if (cur === 'GBP' || cur === '£') return `£${fee}m`
        if (cur === 'EUR' || cur === '€') return `€${fee}m`
        if (cur === 'USD' || cur === '$') return `$${fee}m`
        return `${fee}m`
    }

    function getTransferBadge(type) {
        const value = (type || '').toLowerCase()
        if (value.includes('completed') || value.includes('official')) return { label: 'Official', css: 'badge-official' }
        if (value.includes('rumour') || value.includes('rumor')) return { label: 'Rumour', css: 'badge-rumour' }
        if (value.includes('contract')) return { label: 'Contract', css: 'badge-contract' }
        if (value.includes('free')) return { label: 'Free', css: 'badge-free' }
        return { label: 'Unknown', css: 'badge-neutral' }
    }

    const statCards = [
        { label: 'Records tracked', value: stats?.totalTransfers ?? (dataUnavailable ? null : items.length) },
        { label: 'Official deals', value: stats?.completedTransfers ?? null },
        { label: 'Active rumours', value: stats?.rumours ?? null },
        { label: 'Contract updates', value: stats?.contracts ?? null },
        { label: 'Free transfers', value: stats?.freeTransfers ?? null }
    ]

    const categories = [
        { title: 'Official deals', desc: 'Completed and confirmed moves', link: '/transfers?cat=official', count: stats?.completedTransfers ?? null },
        { title: 'Rumours', desc: 'Reported moves awaiting confirmation', link: '/transfers?cat=rumours', count: stats?.rumours ?? null },
        { title: 'Contracts', desc: 'Renewals and extensions', link: '/transfers?cat=contracts', count: stats?.contracts ?? null },
        { title: 'Free transfers', desc: 'Moves completed without a fee', link: '/transfers?cat=free', count: stats?.freeTransfers ?? null }
    ]

    return (
        <div className="home-page">
            <section className="home-hero" style={{ backgroundImage: `url(${heroImg})` }}>
                <div className="hero-layer" />
                <div className="hero-shell">
                    <div className="hero-copy">
                        <span className="hero-label"><i /> Transfer market coverage</span>
                        <h1>Football moves,<br />without the noise.</h1>
                        <p>Track confirmed deals, credible reports and contract updates in one structured market feed.</p>
                        <div className="hero-actions">
                            <button onClick={() => navigate('/transfers')} className="hero-primary">
                                Open transfer feed <ArrowRight size={17} />
                            </button>
                            <button onClick={() => navigate('/statistics')} className="hero-secondary">
                                <BarChart3 size={17} /> Market overview
                            </button>
                        </div>
                    </div>
                </div>
                <div className="hero-status">
                    <span><i /> Monitoring BBC Sport</span>
                    <span>Structured records</span>
                    <span>Updated automatically</span>
                </div>
            </section>

            <main className="home-main">
                <section className="market-summary" aria-label="Market summary">
                    {statCards.map((stat) => (
                        <div className="kpi-card" key={stat.label}>
                            <span>{stat.label}</span>
                            <strong>{loading || stat.value === null ? '—' : stat.value}</strong>
                        </div>
                    ))}
                </section>

                {error && <div className="home-error"><span>{error}</span><button type="button" onClick={() => setReloadKey((value) => value + 1)}>Retry live data</button></div>}

                <section className="home-data-grid">
                    <div className="latest-section">
                        <div className="section-head">
                            <div>
                                <span>Latest movement</span>
                                <h2>Recently tracked</h2>
                            </div>
                            <Link to="/transfers" className="section-link">All transfers <ArrowRight size={15} /></Link>
                        </div>
                        <HeroPreview
                            items={latest}
                            formatFee={formatFee}
                            getTransferBadge={getTransferBadge}
                            loading={loading}
                            unavailable={dataUnavailable}
                        />
                    </div>

                    <aside className="category-section">
                        <div className="section-head">
                            <div>
                                <span>Market views</span>
                                <h2>Browse by status</h2>
                            </div>
                        </div>
                        <div className="category-strip">
                            {categories.map((category) => (
                                <Link to={category.link} className="category-pill" key={category.title}>
                                    <span className="category-count">{category.count ?? '—'}</span>
                                    <span className="category-copy">
                                        <strong>{category.title}</strong>
                                        <small>{category.desc}</small>
                                    </span>
                                    <ChevronRight size={18} />
                                </Link>
                            ))}
                        </div>
                    </aside>
                </section>
            </main>

            <footer className="home-footer">
                <strong>Transfer Index</strong>
                <span>Independent football market monitoring.</span>
            </footer>
        </div>
    )
}
